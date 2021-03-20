using System;
using UnityEngine;
using System.Collections.Generic;
using CodeMonkey.Utils;

namespace MatchDemo
{
    public class DemoVisual : MonoBehaviour
    {
        public event EventHandler OnStateChanged;

        public enum State
        {
            Busy,
            WaitingForUser,
            TryFindMatches,
            TrySpawnMissing,
            GameOver,
        }

        [SerializeField] private Transform pfCellGridVisual;
        [SerializeField] private Transform pfBackgroundGridVisual;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Demo demo;

        private Grid<Demo.CellGridPosition> grid;
        private Dictionary<Demo.CellGrid, CellGridVisual> cellGridDictionary;

        private bool isSetup;
        private State state;
        private float busyTimer;
        private Action onBusyTimerElapsedAction;

        private int startDragX;
        private int startDragY;
        private Vector3 startDragMouseWorldPosition;

        private void Awake()
        {
            isSetup = false;

            demo.OnLevelSet += Demo_OnLevelSet;
            demo.TryAndFindMatches += Demo_TryAndFindMatches;
        }

        private void Demo_OnLevelSet(object sender, Demo.OnLevelSetEventArgs e)
        {
            FunctionTimer.Create(() => {
                Setup(sender as Demo, e.grid);
            }, .1f);
        }

        private void Demo_TryAndFindMatches(object sender, EventArgs e)
        {
            SetBusyState(.5f, () => SetState(State.TryFindMatches));
        }

        public void Setup(Demo demo, Grid<Demo.CellGridPosition> grid)
        {
            this.demo = demo;
            this.grid = grid;

            float cameraYOffset = 1f;
            cameraTransform.position = new Vector3(grid.GetWidth() * .5f, grid.GetHeight() * .5f + cameraYOffset, cameraTransform.position.z);

            demo.OnCellGridPositionDestroyed += Demo_OnCellGridPositionDestroyed;
            demo.OnNewCellGridSpawned += Demo_OnNewCellGridSpawned;

            // Initialize Visual
            cellGridDictionary = new Dictionary<Demo.CellGrid, CellGridVisual>();

            for (int x = 0; x < grid.GetWidth(); x++)
            {
                for (int y = 0; y < grid.GetHeight(); y++)
                {
                    Demo.CellGridPosition cellGridPosition = grid.GetGridObject(x, y);
                    Demo.CellGrid cellGrid = cellGridPosition.GetCellGrid();

                    Vector3 position = grid.GetWorldPosition(x, y);
                    position = new Vector3(position.x, 12); // ???

                    // Visual Transform
                    //Transform cellGridVisualTransform = Instantiate(pfCellGridVisual, position, Quaternion.identity);
                    Transform cellGridVisualTransform = Instantiate(pfCellGridVisual, grid.GetWorldPosition(x, y), Quaternion.identity);
                    cellGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>().sprite = cellGrid.GetCell().sprite;

                    CellGridVisual cellGridVisual = new CellGridVisual(cellGridVisualTransform, cellGrid, demo.GetGridPositionOffset());

                    cellGridDictionary[cellGrid] = cellGridVisual;

                    // Background Grid Visual
                    Instantiate(pfBackgroundGridVisual, grid.GetWorldPosition(x, y), Quaternion.identity);
                }
            }

            SetBusyState(.5f, () => SetState(State.TryFindMatches));

            isSetup = true;
        }

        private void Demo_OnNewCellGridSpawned(object sender, Demo.OnNewCellGridSpawnedEventArgs e)
        {
            Vector3 position = e.cellGridPosition.GetWorldPosition();
            //position = new Vector3(position.x, 12); // Falling blocks

            Transform cellGridVisualTransform = Instantiate(pfCellGridVisual, position, Quaternion.identity);
            cellGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>().sprite = e.cellGrid.GetCell().sprite;

            CellGridVisual cellGridVisual = new CellGridVisual(cellGridVisualTransform, e.cellGrid, demo.GetGridPositionOffset());

            cellGridDictionary[e.cellGrid] = cellGridVisual;
        }

        private void Demo_OnCellGridPositionDestroyed(object sender, System.EventArgs e)
        {
            Demo.CellGridPosition cellGridPosition = sender as Demo.CellGridPosition;
            if (cellGridPosition != null && cellGridPosition.GetCellGrid() != null)
            {
                cellGridDictionary.Remove(cellGridPosition.GetCellGrid());
            }
        }

        private void Update()
        {
            if (!isSetup) return;

            UpdateVisual();

            switch (state)
            {
                case State.Busy:
                    busyTimer -= Time.deltaTime;
                    if (busyTimer <= 0f)
                    {
                        onBusyTimerElapsedAction();
                    }
                    break;
                case State.WaitingForUser:
                    if (Input.GetMouseButtonDown(0))
                    {
                        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                        grid.GetXY(mouseWorldPosition, out startDragX, out startDragY);

                        startDragMouseWorldPosition = mouseWorldPosition;
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                        grid.GetXY(mouseWorldPosition, out int x, out int y);

                        // ToDo - Clean Up
                        if (demo.CanRevealGridPosition(startDragX, startDragY, x, y))
                        {
                            RevealGridPosition(startDragX, startDragY);
                        }
                        else
                        {
                            if (x != startDragX)
                            {
                                // Different X
                                y = startDragY;

                                if (x < startDragX)
                                {
                                    x = startDragX - 1;
                                }
                                else
                                {
                                    x = startDragX + 1;
                                }
                            }
                            else
                            {
                                // Different Y
                                x = startDragX;

                                if (y < startDragY)
                                {
                                    y = startDragY - 1;
                                }
                                else
                                {
                                    y = startDragY + 1;
                                }
                            }


                            if (demo.CanSwapGridPositions(startDragX, startDragY, x, y))
                            {
                                SwapGridPositions(startDragX, startDragY, x, y);
                            }

                            if (demo.CanStepOverGridPositions(startDragX, startDragY, x, y))
                            {
                                StepOverGridPositions(startDragX, startDragY, x, y);
                            }
                        }
                    }
                    break;
                case State.TryFindMatches:
                    if (demo.TryFindMatchesAndDestroyThem())
                    {
                        SetBusyState(.3f, () => {
                            //demo.FallCellIntoEmptyPositions();

                            SetBusyState(.3f, () => {
                                demo.SpawnNewMissingGridPositions();

                                SetBusyState(.5f, () => SetState(State.TryFindMatches));
                            });
                        });
                    }
                    else
                    {
                        TrySetStateWaitingForUser();
                    }
                    break;
                case State.TrySpawnMissing:
                    SetBusyState(.3f, () => {
                        demo.SpawnNewMissingGridPositions();

                        SetBusyState(.5f, () => SetState(State.TryFindMatches));
                    });
                    break;
                case State.GameOver:
                    break;
            }
        }

        public class CellGridVisual
        {
            private Transform transform;
            private Demo.CellGrid cellGrid;
            private Vector3 cellGridVisdualOffset;

            public CellGridVisual(Transform transform, Demo.CellGrid cellGrid, Vector3 cellGridVisdualOffset)
            {
                this.transform = transform;
                this.cellGrid = cellGrid;
                this.cellGridVisdualOffset = cellGridVisdualOffset;

                cellGrid.OnDestroyed += CellGrid_OnDestroyed;
            }

            private void CellGrid_OnDestroyed(object sender, System.EventArgs e)
            {
                //transform.GetComponent<Animation>().Play();
                //Destroy(transform.gameObject, 1f);
                Destroy(transform.gameObject);
            }

            public void Update()
            {
                Vector3 targetPosition = cellGrid.GetWorldPosition() + cellGridVisdualOffset;
                Vector3 moveDir = (targetPosition - transform.position);
                float moveSpeed = 10f;
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
        }

        private void UpdateVisual()
        {
            foreach (Demo.CellGrid cellGrid in cellGridDictionary.Keys)
            {
                cellGridDictionary[cellGrid].Update();
            }
        }

        public void SwapGridPositions(int startX, int startY, int endX, int endY)
        {
            demo.SwapGridPositions(startX, startY, endX, endY);
            demo.UseMove();

            SetBusyState(.5f, () => SetState(State.TryFindMatches));
        }

        public void StepOverGridPositions(int startX, int startY, int endX, int endY)
        {
            demo.StepOverGridPositions(startX, startY, endX, endY);
            demo.UseMove();

            SetState(State.TrySpawnMissing);
            //SetBusyState(.5f, () => SetState(State.TryFindMatches));
        }

        public void RevealGridPosition(int X, int Y)
        {
            demo.RevealGridPosition(X, Y);
            demo.UseMove();

            SetBusyState(.5f, () => SetState(State.TryFindMatches));
        }

        private void SetBusyState(float busyTimer, Action onBusyTimerElapsedAction)
        {
            SetState(State.Busy);
            this.busyTimer = busyTimer;
            this.onBusyTimerElapsedAction = onBusyTimerElapsedAction;
        }

        private void TrySetStateWaitingForUser()
        {
            //if (demo.TryIsGameOver())
            //{
            //    // Game Over!
            //    Debug.Log("Game Over!");
            //    SetState(State.GameOver);
            //}
            //else
            //{
                // Keep Playing
                SetState(State.WaitingForUser);
            //}
        }

        private void SetState(State state)
        {
            this.state = state;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
