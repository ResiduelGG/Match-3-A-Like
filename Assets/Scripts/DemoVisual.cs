using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace MatchDemo
{
    public class DemoVisual : MonoBehaviour
    {
        public enum State
        {
            Busy,
            Drag,
            WaitingForUser,
            TryFindMatches,
            TrySpawnMissing,
            GameOver
        }

        [SerializeField] private Transform pfCellGridVisual;
        [SerializeField] private Transform pfBackgroundGridVisual;
        [SerializeField] [CanBeNull] private Transform cameraTransform;
        [SerializeField] private Demo demo;
        private float busyTimer;
        private Dictionary<Demo.CellGrid, CellGridVisual> cellGridDictionary;

        private Grid<Demo.CellGridPosition> grid;

        private bool isSetup;
        private Action onBusyTimerElapsedAction;
        private int startDragY;
        private Vector3 startDragMouseWorldPosition;

        private int startDragX;
        private State state;

        private void Awake()
        {
            isSetup = false;

            demo.OnLevelSet += Demo_OnLevelSet;
            demo.TryAndFindMatches += Demo_TryAndFindMatches;
        }

        private void Update()
        {
            if (!isSetup) return;

            UpdateVisual();

            switch (state)
            {
                case State.Busy:
                    busyTimer -= Time.deltaTime;
                    if (busyTimer <= 0f) onBusyTimerElapsedAction();
                    break;
                case State.Drag:
                    if (demo.CanDragAndDrop())
                    {
                        var mouseWorldPosition = UtilsClass.GetMouseWorldPosition();

                        if (Input.GetMouseButtonUp(0))
                        {
                            Drop(mouseWorldPosition);
                            break;
                        }

                        grid.GetXY(mouseWorldPosition, out startDragX, out startDragY);
                        var dragPosition = mouseWorldPosition - demo.GetGridPositionOffset();

                        var x = dragPosition.x;
                        var y = dragPosition.y;

                        // Account for negative rounding
                        if (x < 0) x--;
                        if (y < 0) y--;

                        Drag((int)x, (int)y);
                    }

                    break;
                case State.WaitingForUser:
                    if (demo.CanDragAndDrop())
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            var mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                            grid.GetXY(mouseWorldPosition, out startDragX, out startDragY);
                            if (demo.CanDrag(startDragX, startDragY)) StartDrag(startDragX, startDragY);
                        }
                    }
                    else
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            var mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                            grid.GetXY(mouseWorldPosition, out startDragX, out startDragY);

                            startDragMouseWorldPosition = mouseWorldPosition;
                        }

                        if (Input.GetMouseButtonUp(0))
                        {
                            var mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                            grid.GetXY(mouseWorldPosition, out var x, out var y);

                            // ToDo - Clean Up
                            if (demo.CanRevealGridPosition(startDragX, startDragY))
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
                                        x = startDragX - 1;
                                    else
                                        x = startDragX + 1;
                                }
                                else
                                {
                                    // Different Y
                                    x = startDragX;

                                    if (y < startDragY)
                                        y = startDragY - 1;
                                    else
                                        y = startDragY + 1;
                                }


                                if (demo.CanSwapGridPositions(startDragX, startDragY, x, y))
                                    SwapGridPositions(startDragX, startDragY, x, y);

                                if (demo.CanStepOverGridPositions(startDragX, startDragY, x, y))
                                    StepOverGridPositions(startDragX, startDragY, x, y);
                            }
                        }
                    }

                    break;
                case State.TryFindMatches:
                    if (demo.TryFindMatchesAndDestroyThem())
                        SetBusyState(.3f, () =>
                        {
                            //demo.FallCellIntoEmptyPositions();
                            SetBusyState(.3f, () =>
                            {
                                demo.SpawnNewMissingGridPositions();

                                SetBusyState(.5f, () => SetState(State.TryFindMatches));
                            });
                        });
                    else
                        TrySetStateWaitingForUser();
                    break;
                case State.TrySpawnMissing:
                    SetBusyState(.3f, () =>
                    {
                        demo.SpawnNewMissingGridPositions();

                        SetBusyState(.5f, () => SetState(State.TryFindMatches));
                    });
                    break;
                case State.GameOver:
                    break;
            }
        }

        public event EventHandler OnStateChanged;

        private void Demo_OnLevelSet(object sender, Demo.OnLevelSetEventArgs e)
        {
            FunctionTimer.Create(() => { Setup(sender as Demo, e.grid); }, .1f);
        }

        private void Demo_TryAndFindMatches(object sender, EventArgs e)
        {
            SetBusyState(.5f, () => SetState(State.TryFindMatches));
        }

        public void Setup(Demo demo, Grid<Demo.CellGridPosition> grid)
        {
            this.demo = demo;
            this.grid = grid;

            var cameraYOffset = 1f;

            if (cameraTransform != null)
                cameraTransform.position = new Vector3(grid.GetWidth() * .5f, grid.GetHeight() * .5f + cameraYOffset,
                    cameraTransform.position.z);

            demo.OnCellGridPositionDestroyed += Demo_OnCellGridPositionDestroyed;
            demo.OnNewCellGridSpawned += Demo_OnNewCellGridSpawned;

            // Initialize Visual
            cellGridDictionary = new Dictionary<Demo.CellGrid, CellGridVisual>();

            for (var x = 0; x < grid.GetWidth(); x++)
            for (var y = 0; y < grid.GetHeight(); y++)
            {
                var cellGridPosition = grid.GetGridObject(x, y);
                var cellGrid = cellGridPosition.GetCellGrid();

                var position = grid.GetWorldPosition(x, y);

                //position = new Vector3(position.x, 0); // ???
                // Visual Transform
                //Transform cellGridVisualTransform = Instantiate(pfCellGridVisual, position, Quaternion.identity);
                var cellGridVisualTransform =
                    Instantiate(pfCellGridVisual, grid.GetWorldPosition(x, y), Quaternion.identity);
                var spriteRenderer =
                    cellGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = cellGrid.GetCell().sprite;
                spriteRenderer.color = cellGrid.GetCell().tint;


                var cellGridVisual =
                    new CellGridVisual(cellGridVisualTransform, cellGrid, demo.GetGridPositionOffset());

                cellGridDictionary[cellGrid] = cellGridVisual;

                // Background Grid Visual
                Instantiate(pfBackgroundGridVisual, grid.GetWorldPosition(x, y), Quaternion.identity);
            }

            SetBusyState(.5f, () => SetState(State.TryFindMatches));

            isSetup = true;
        }

        private void Demo_OnNewCellGridSpawned(object sender, Demo.OnNewCellGridSpawnedEventArgs e)
        {
            var position = e.cellGridPosition.GetWorldPosition();
            //position = new Vector3(position.x, 12); // Falling blocks

            var cellGridVisualTransform = Instantiate(pfCellGridVisual, position, Quaternion.identity);

            var spriteRenderer =
                cellGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = e.cellGrid.GetCell().sprite;
            spriteRenderer.color = e.cellGrid.GetCell().tint;
            
            var cellGridVisual = new CellGridVisual(cellGridVisualTransform, e.cellGrid, demo.GetGridPositionOffset());

            cellGridDictionary[e.cellGrid] = cellGridVisual;
        }

        private void Demo_OnCellGridPositionDestroyed(object sender, EventArgs e)
        {
            var cellGridPosition = sender as Demo.CellGridPosition;
            if (cellGridPosition != null && cellGridPosition.GetCellGrid() != null)
                cellGridDictionary.Remove(cellGridPosition.GetCellGrid());
        }

        private void UpdateVisual()
        {
            foreach (var cellGrid in cellGridDictionary.Keys) cellGridDictionary[cellGrid].Update();
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

        public void StartDrag(int X, int Y)
        {
            var cellGridPosition = grid.GetGridObject(X, Y);
            var cellGrid = cellGridPosition.GetCellGrid();

            var cellGridVisual = cellGridDictionary[cellGrid];
            cellGridVisual.Up();

            demo.StartDrag(X, Y);
            SetState(State.Drag);
        }

        public void Drag(int X, int Y)
        {
            demo.Drag(X, Y);
        }

        public void Drop(Vector3 dropWorldPositon)
        {
            demo.Drop(dropWorldPositon);
            //SetBusyState(.5f, () => SetState(State.TryFindMatches));
            SetBusyState(.5f, () => SetState(State.TrySpawnMissing));
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

        public class CellGridVisual
        {
            private readonly Demo.CellGrid cellGrid;
            private readonly Vector3 cellGridVisualOffset;
            private readonly Transform transform;

            public CellGridVisual(Transform transform, Demo.CellGrid cellGrid, Vector3 cellGridVisualOffset)
            {
                this.transform = transform;
                this.cellGrid = cellGrid;
                this.cellGridVisualOffset = cellGridVisualOffset;

                cellGrid.OnDestroyed += CellGrid_OnDestroyed;
            }

            private void CellGrid_OnDestroyed(object sender, EventArgs e)
            {
                //transform.GetComponent<Animation>().Play();
                //Destroy(transform.gameObject, 1f);
                Destroy(transform.gameObject);
            }

            public void Update()
            {
                var targetPosition = cellGrid.GetWorldPosition() + cellGridVisualOffset;
                var moveDir = targetPosition - transform.position;
                var moveSpeed = 10f;
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }

            public void Up()
            {
                transform.Find("sprite").GetComponent<SpriteRenderer>().sortingOrder += 10;
            }

            public void Down()
            {
                transform.Find("sprite").GetComponent<SpriteRenderer>().sortingOrder -= 10;
            }
        }
    }
}