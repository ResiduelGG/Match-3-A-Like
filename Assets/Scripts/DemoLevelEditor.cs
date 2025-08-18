using System.Collections.Generic;
using CodeMonkey.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MatchDemo
{
    /*
     * Controls:
     *   Number Keys 1-5 = Set Gem Type
     *   Right Click = Toggle Glass
     * */
    public class DemoLevelEditor : MonoBehaviour
    {
        [SerializeField] private DemoLevel demoLevel;
        [SerializeField] private Transform pfGemGridVisual;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private TextMeshProUGUI levelText;

        private Grid<GridPosition> grid;

        private void Awake()
        {
            grid = new Grid<GridPosition>(demoLevel.width, demoLevel.height, 1f, Vector3.zero,
                (g, x, y) => new GridPosition(demoLevel, g, x, y));

            levelText.text = demoLevel.name;

            if (demoLevel.levelGridPositionList == null ||
                demoLevel.levelGridPositionList.Count != demoLevel.width * demoLevel.height)
            {
                // Create new Level
                Debug.Log("Creating new level...");
                demoLevel.levelGridPositionList = new List<DemoLevel.LevelGridPosition>();

                for (var x = 0; x < grid.GetWidth(); x++)
                for (var y = 0; y < grid.GetHeight(); y++)
                {
                    var gem = demoLevel.cellList[Random.Range(0, demoLevel.cellList.Count)];

                    var levelGridPosition = new DemoLevel.LevelGridPosition { x = x, y = y, cell = gem };
                    demoLevel.levelGridPositionList.Add(levelGridPosition);

                    CreateVisual(grid.GetGridObject(x, y), levelGridPosition);
                }
            }
            else
            {
                // Load Level
                Debug.Log("Loading level...");
                for (var x = 0; x < grid.GetWidth(); x++)
                for (var y = 0; y < grid.GetHeight(); y++)
                {
                    DemoLevel.LevelGridPosition levelGridPosition = null;

                    foreach (var tmpLevelGridPosition in demoLevel.levelGridPositionList)
                        if (tmpLevelGridPosition.x == x && tmpLevelGridPosition.y == y)
                        {
                            levelGridPosition = tmpLevelGridPosition;
                            break;
                        }

                    if (levelGridPosition == null) Debug.LogError("Error! Null!");

                    CreateVisual(grid.GetGridObject(x, y), levelGridPosition);
                }
            }

            cameraTransform.position =
                new Vector3(grid.GetWidth() * .5f, grid.GetHeight() * .5f, cameraTransform.position.z);
        }

        private void Update()
        {
            var mouseWorldPosition = UtilsClass.GetMouseWorldPosition();

            grid.GetXY(mouseWorldPosition, out var x, out var y);

            if (IsValidPosition(x, y))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[0]);
                if (Input.GetKeyDown(KeyCode.Alpha2)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[1]);
                if (Input.GetKeyDown(KeyCode.Alpha3)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[2]);
                if (Input.GetKeyDown(KeyCode.Alpha4)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[3]);
                if (Input.GetKeyDown(KeyCode.Alpha5)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[4]);
                if (Input.GetKeyDown(KeyCode.Alpha6)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[5]);
                if (Input.GetKeyDown(KeyCode.Alpha7)) grid.GetGridObject(x, y).SetDemoCell(demoLevel.cellList[6]);
            }
        }

        private void CreateVisual(GridPosition gridPosition, DemoLevel.LevelGridPosition levelGridPosition)
        {
            var gemGridVisualTransform =
                Instantiate(pfGemGridVisual, gridPosition.GetWorldPosition(), Quaternion.identity);

            gridPosition.spriteRenderer = gemGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>();
            gridPosition.levelGridPosition = levelGridPosition;

            gridPosition.SetDemoCell(levelGridPosition.cell);
        }

        private bool IsValidPosition(int x, int y)
        {
            if (x < 0 || y < 0 ||
                x >= grid.GetWidth() || y >= grid.GetHeight())
                // Invalid position
                return false;
            return true;
        }

        private class GridPosition
        {
            private readonly DemoLevel demoLevel;
            private readonly Grid<GridPosition> grid;
            private readonly int y;
            private readonly int x;
            public DemoLevel.LevelGridPosition levelGridPosition;

            public SpriteRenderer spriteRenderer;

            public GridPosition(DemoLevel demoLevel, Grid<GridPosition> grid, int x, int y)
            {
                this.demoLevel = demoLevel;
                this.grid = grid;
                this.x = x;
                this.y = y;
            }

            public Vector3 GetWorldPosition()
            {
                return grid.GetWorldPosition(x, y);
            }

            public void SetDemoCell(DemoCell cell)
            {
                spriteRenderer.sprite = cell.sprite;
                levelGridPosition.cell = cell;
#if UNITY_EDITOR
                EditorUtility.SetDirty(demoLevel);
#endif
            }
        }
    }
}