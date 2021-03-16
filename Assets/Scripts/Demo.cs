using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchDemo
{
    public class Demo : MonoBehaviour
    {
        public event EventHandler OnCellGridPositionDestroyed;
        public event EventHandler<OnNewCellGridSpawnedEventArgs> OnNewCellGridSpawned;
        public event EventHandler<OnLevelSetEventArgs> OnLevelSet;
        //public event EventHandler OnGlassDestroyed;
        public event EventHandler OnMoveUsed;
        //public event EventHandler OnOutOfMoves;
        public event EventHandler OnScoreChanged;
        //public event EventHandler OnWin;

        public class OnNewCellGridSpawnedEventArgs : EventArgs
        {
            public CellGrid cellGrid;
            public CellGridPosition cellGridPosition;
        }

        public class OnLevelSetEventArgs : EventArgs
        {
            public DemoLevel level;
            public Grid<CellGridPosition> grid;
        }

        [SerializeField] private DemoLevel level;
        [SerializeField] private Vector2 gridPositionOffset;
        
        private int gridWidth;
        private int gridHeight;
        private bool generateRandomCells;
        private Grid<CellGridPosition> grid;

        private SavedCellData savedStartCell;
        private SavedCellData savedEndCell;

        private void Start()
        {
            InitLevel(level);
        }

        public void InitLevel(DemoLevel level)
        {
            this.level = level;

            gridWidth = level.width;
            gridHeight = level.height;
            generateRandomCells = level.generateRandomCells;

            grid = new Grid<CellGridPosition>(gridWidth, gridHeight, 1f, Vector2.zero + gridPositionOffset, (Grid<CellGridPosition> g, int x, int y) => new CellGridPosition(g, x, y));

            bool findUniqueLevel = true;
            int count = 0;

            // Ensure starting level without matches
            while (findUniqueLevel)
            {
                count++;
                // Initialize Grid
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        DemoLevel.LevelGridPosition levelGridPosition = null;

                        foreach (DemoLevel.LevelGridPosition tmpLevelGridPosition in level.levelGridPositionList)
                        {
                            if (tmpLevelGridPosition.x == x && tmpLevelGridPosition.y == y)
                            {
                                levelGridPosition = tmpLevelGridPosition;
                                break;
                            }
                        }

                        DemoCell cell = null;

                        // TODO - Clean Up
                        if (levelGridPosition == null)
                        {
                            if (generateRandomCells)
                            {
                                cell = level.GetRandomCell();
                            } else
                            {
                                cell = level.GetRevealCell();
                            }
                            
                        } else
                        {

                            cell = levelGridPosition.cell;
                        }


                        CellGrid cellGrid = new CellGrid(cell, x, y);
                        grid.GetGridObject(x, y).SetCellGrid(cellGrid);
                    }
                }

                List<List<CellGridPosition>> allLinkedCellGridPositionList = GetAllCellLinks();

                if (allLinkedCellGridPositionList.Count == 0)
                {
                    findUniqueLevel = false;
                }
            }

            Debug.Log(count);

            OnLevelSet?.Invoke(this, new OnLevelSetEventArgs { level = level, grid = grid });
        }

        public Vector3 GetGridPositionOffset()
        {
            return new Vector3(gridPositionOffset.x, gridPositionOffset.y, 0);
        }

        public bool CanRevealGridPosition(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

            if (startX != endX || startY != endY) return false; // Not the same position

            CellGridPosition revealGridPosition = grid.GetGridObject(startX, startY);

            if (!revealGridPosition.HasCellGrid()) return false;

            CellGrid revealCellGrid = revealGridPosition.GetCellGrid();
            DemoCell revealCell = revealCellGrid.GetCell();

            if (revealCell.cellType != DemoCell.Type.Reveal) return false;

            return true;
        }

        public bool CanSwapGridPositions(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

            if (startX == endX && startY == endY) return false; // Same Position

            CellGridPosition startSwapGridPosition = grid.GetGridObject(startX, startY);
            CellGridPosition endSwapGridPosition = grid.GetGridObject(endX, endY);

            CellGrid startSwapCellGrid = startSwapGridPosition.GetCellGrid();
            CellGrid endSwapCellGrid = endSwapGridPosition.GetCellGrid();

            if (startSwapCellGrid == null || endSwapCellGrid == null) return false; // Cannot move to empty space

            DemoCell startSwapCell = startSwapCellGrid.GetCell();
            DemoCell endSwapCell = endSwapCellGrid.GetCell();

            if (startSwapCell.cellType != DemoCell.Type.Match || endSwapCell.cellType != DemoCell.Type.Match) return false; // One of the swap cells are invalid

            SwapGridPositions(startX, startY, endX, endY, true); // Swap

            bool hasLinkAfterSwap = HasCellLink(startX, startY) || HasCellLink(endX, endY);

            SwapGridPositions(startX, startY, endX, endY, true); // Swap Back

            return hasLinkAfterSwap;
        }

        public bool CanStepOverGridPositions(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

            if (startX == endX && startY == endY) return false; // Same Position

            CellGridPosition startSwapGridPosition = grid.GetGridObject(startX, startY);
            CellGridPosition endSwapGridPosition = grid.GetGridObject(endX, endY);

            CellGrid startSwapCellGrid = startSwapGridPosition.GetCellGrid();
            CellGrid endSwapCellGrid = endSwapGridPosition.GetCellGrid();

            if (startSwapCellGrid == null || endSwapCellGrid == null) return false; // Cannot move to empty space

            DemoCell startSwapCell = startSwapCellGrid.GetCell();
            DemoCell endSwapCell = endSwapCellGrid.GetCell();

            if (startSwapCell.cellType == DemoCell.Type.Reveal || endSwapCell.cellType == DemoCell.Type.Reveal) return false; // Can only step over match cells
            if (startSwapCell.cellType != endSwapCell.cellType) return false; // One of the swap cells are not of the same type
            if (startSwapCell.cellName != endSwapCell.cellName) return false; // One of the swap cells are not of the same name

            int horizontalDirection = endX - startX;
            int verticalDirection = endY - startY;

            int stepOverPositionX = endX + horizontalDirection;
            int stepOverPositionY = endY + verticalDirection;

            if (!IsValidPosition(stepOverPositionX, stepOverPositionY)) return false; // Invalid Position
            if (endX == stepOverPositionX && endY == stepOverPositionY) return false; // Same Position

            CellGridPosition stepOverGridPosition = grid.GetGridObject(stepOverPositionX, stepOverPositionY);
            CellGrid stepOverCellGrid = stepOverGridPosition.GetCellGrid();

            if (stepOverCellGrid == null) return false; // Cannot move to empty space

            DemoCell stepOverCell = stepOverCellGrid.GetCell();

            if (stepOverCell.cellType != DemoCell.Type.Reveal) return false; // Cannot move to empty space

            return true;
        }

        public void SwapGridPositions(int startX, int startY, int endX, int endY, bool swapCheck = false)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return; // Invalid Position

            if (startX == endX && startY == endY) return; // Same Position

            CellGridPosition startCellGridPosition = grid.GetGridObject(startX, startY);
            CellGridPosition endCellGridPosition = grid.GetGridObject(endX, endY);

            CellGrid startCellGrid = startCellGridPosition.GetCellGrid();
            CellGrid endCellGrid = endCellGridPosition.GetCellGrid();

            if (startCellGrid == null || endCellGrid == null) return; // Cannot move to empty space

            startCellGrid.SetCellXY(endX, endY);
            endCellGrid.SetCellXY(startX, startY);

            DemoCell startSwapCell = startCellGrid.GetCell();
            DemoCell endSwapCell = endCellGrid.GetCell();

            // Save swapped cell data for later comparison for reveal and combine
            if (!swapCheck)
            {
                savedStartCell = new SavedCellData(startCellGrid.GetCell().cellName, endX, endY, startCellGrid.GetCell().cellIndex, startSwapCell.cellType);
                savedEndCell = new SavedCellData(endCellGrid.GetCell().cellName, startX, startY, endCellGrid.GetCell().cellIndex, endSwapCell.cellType);
            }

            startCellGridPosition.SetCellGrid(endCellGrid);
            endCellGridPosition.SetCellGrid(startCellGrid);
        }

        public void StepOverGridPositions(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return; // Invalid Position

            if (startX == endX && startY == endY) return; // Same Position

            CellGridPosition startSwapGridPosition = grid.GetGridObject(startX, startY);
            CellGridPosition endSwapGridPosition = grid.GetGridObject(endX, endY);

            CellGrid startSwapCellGrid = startSwapGridPosition.GetCellGrid();
            CellGrid endSwapCellGrid = endSwapGridPosition.GetCellGrid();

            if (startSwapCellGrid == null || endSwapCellGrid == null) return; // Cannot move to empty space

            DemoCell startSwapCell = startSwapCellGrid.GetCell();
            DemoCell endSwapCell = endSwapCellGrid.GetCell();

            if (startSwapCell.cellType != endSwapCell.cellType) return; // One of the swap cells are not of the same type
            if (startSwapCell.cellName != endSwapCell.cellName) return; // One of the swap cells are not of the same name

            int horizontalDirection = endX - startX;
            int verticalDirection = endY - startY;

            int stepOverPositionX = endX + horizontalDirection;
            int stepOverPositionY = endY + verticalDirection;

            if (!IsValidPosition(stepOverPositionX, stepOverPositionY)) return; // Invalid Position
            if (endX == stepOverPositionX && endY == stepOverPositionY) return; // Same Position

            CellGridPosition stepOverGridPosition = grid.GetGridObject(stepOverPositionX, stepOverPositionY);
            CellGrid stepOverCellGrid = stepOverGridPosition.GetCellGrid();

            if (stepOverCellGrid == null) return; // Cannot move to empty space

            DemoCell stepOverCell = stepOverCellGrid.GetCell();

            if (stepOverCell.cellType != DemoCell.Type.Reveal) return; // Cannot move to empty space

            // Remove old cells
            startSwapGridPosition.ClearCellGrid();
            TryDestroyCellGridPosition(endSwapGridPosition);
            TryDestroyCellGridPosition(stepOverGridPosition);

            // Handle cell movement to reveal position
            startSwapCellGrid.SetCellXY(stepOverPositionX, stepOverPositionY);

            // Save swapped cell data for later comparison for reveal and combine
            savedStartCell = new SavedCellData(startSwapCellGrid.GetCell().cellName, stepOverPositionX, stepOverPositionY, startSwapCellGrid.GetCell().cellIndex, stepOverCell.cellType);
            savedEndCell = null;

            stepOverGridPosition.SetCellGrid(startSwapCellGrid);

            // Spawn reveal cells
            //DemoCell startCell = level.GetRevealCell();
            //DemoCell endCell = level.GetRevealCell();

            //CellGrid startCellGrid = new CellGrid(startCell, startX, startY);
            //CellGrid endCellGrid = new CellGrid(endCell, endX, endY);

            //startSwapGridPosition.SetCellGrid(startCellGrid);
            //endSwapGridPosition.SetCellGrid(endCellGrid);

            //OnNewCellGridSpawned?.Invoke(startCellGrid, new OnNewCellGridSpawnedEventArgs
            //{
            //    cellGrid = startCellGrid,
            //    cellGridPosition = startSwapGridPosition,
            //});

            //OnNewCellGridSpawned?.Invoke(endCellGrid, new OnNewCellGridSpawnedEventArgs
            //{
            //    cellGrid = endCellGrid,
            //    cellGridPosition = endSwapGridPosition,
            //});
        }

        public void RevealGridPosition(int X, int Y)
        {
            if (!IsValidPosition(X, Y)) return; // Invalid Position

            CellGridPosition revealCellGridPosition = grid.GetGridObject(X, Y);

            CellGrid revealCellGrid = revealCellGridPosition.GetCellGrid();

            if (!revealCellGridPosition.HasCellGrid()) return; // Cannot reveal empty space

            // Remove old cell
            TryDestroyCellGridPosition(revealCellGridPosition);

            // Spawn new cell
            DemoCell newCell = level.GetRandomMatchCell();
            CellGrid newCellGrid = new CellGrid(newCell, X, Y);
            grid.GetGridObject(X, Y).SetCellGrid(newCellGrid);


            // Update saved cell data for reveal and combine
            savedStartCell = new SavedCellData(newCell.cellName, X, Y, newCell.cellIndex, newCell.cellType);
            savedEndCell = null;

            // Trigger new cell spawn to update visual
            OnNewCellGridSpawned?.Invoke(newCellGrid, new OnNewCellGridSpawnedEventArgs
            {
                cellGrid = newCellGrid,
                cellGridPosition = revealCellGridPosition,
            });
        }

        private bool IsValidPosition(int x, int y)
        {
            if (x < 0 || y < 0 ||
                x >= gridWidth || y >= gridHeight)
            {
                // Invalid position
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool HasCellLink(int x, int y)
        {
            List<CellGridPosition> linkedCellGridPositionList = GetCellLinks(x, y);
            return linkedCellGridPositionList != null && linkedCellGridPositionList.Count >= 3;
        }

        public void UseMove()
        {
            //moveCount--;
            OnMoveUsed?.Invoke(this, EventArgs.Empty);
        }

        public List<CellGridPosition> GetCellLinks(int x, int y)
        {
            DemoCell cell = GetCell(x, y);

            if (cell == null || cell.cellType != DemoCell.Type.Match) return null;

            int rightLinkAmount = 0;
            for (int i = 1; i < gridWidth; i++)
            {
                if (IsValidPosition(x + i, y))
                {
                    DemoCell nextCell = GetCell(x + i, y);
                    if (nextCell == cell)
                    {
                        // Same Cell
                        rightLinkAmount++;
                    }
                    else
                    {
                        // Not same Cell
                        break;
                    }
                }
                else
                {
                    // Invalid position
                    break;
                }
            }

            int leftLinkAmount = 0;
            for (int i = 1; i < gridWidth; i++)
            {
                if (IsValidPosition(x - i, y))
                {
                    DemoCell nextCell = GetCell(x - i, y);
                    if (nextCell == cell)
                    {
                        // Same Cell
                        leftLinkAmount++;
                    }
                    else
                    {
                        // Not same Cell
                        break;
                    }
                }
                else
                {
                    // Invalid position
                    break;
                }
            }

            int horizontalLinkAmount = 1 + leftLinkAmount + rightLinkAmount; // This Cell + left + right

            if (horizontalLinkAmount >= 3)
            {
                // Has 3 horizontal linked cells
                List<CellGridPosition> linkedCellGridPositionList = new List<CellGridPosition>();
                int leftMostX = x - leftLinkAmount;
                for (int i = 0; i < horizontalLinkAmount; i++)
                {
                    linkedCellGridPositionList.Add(grid.GetGridObject(leftMostX + i, y));
                }
                return linkedCellGridPositionList;
            }

            int upLinkAmount = 0;
            for (int i = 1; i < gridHeight; i++)
            {
                if (IsValidPosition(x, y + i))
                {
                    DemoCell nextCell = GetCell(x, y + i);
                    if (nextCell == cell)
                    {
                        // Same Cell
                        upLinkAmount++;
                    }
                    else
                    {
                        // Not same Cell
                        break;
                    }
                }
                else
                {
                    // Invalid position
                    break;
                }
            }

            int downLinkAmount = 0;
            for (int i = 1; i < gridHeight; i++)
            {
                if (IsValidPosition(x, y - i))
                {
                    DemoCell nextCell = GetCell(x, y - i);
                    if (nextCell == cell)
                    {
                        // Same Cell
                        downLinkAmount++;
                    }
                    else
                    {
                        // Not same Cell
                        break;
                    }
                }
                else
                {
                    // Invalid position
                    break;
                }
            }

            int verticalLinkAmount = 1 + downLinkAmount + upLinkAmount; // This Cell + down + up

            if (verticalLinkAmount >= 3)
            {
                // Has 3 vertical linked cells
                List<CellGridPosition> linkedCellGridPositionList = new List<CellGridPosition>();
                int downMostY = y - downLinkAmount;
                for (int i = 0; i < verticalLinkAmount; i++)
                {
                    linkedCellGridPositionList.Add(grid.GetGridObject(x, downMostY + i));
                }
                return linkedCellGridPositionList;
            }

            // No links
            return null;
        }

        public bool TryFindMatchesAndDestroyThem()
        {
            List<List<CellGridPosition>> allLinkedCellGridPositionList = GetAllCellLinks();

            bool foundMatch = false;

            //List<Vector2Int> explosionGridPositionList = new List<Vector2Int>();

            foreach (List<CellGridPosition> linkedCellGridPositionList in allLinkedCellGridPositionList)
            {
                foreach (CellGridPosition cellGridPosition in linkedCellGridPositionList)
                {
                    TryDestroyCellGridPosition(cellGridPosition);
                }

                //if (linkedCellGridPositionList.Count >= 4)
                //{
                    // More than 4 linked
                    //score += 200;

                    // Special Explosion Cell
                    //CellGridPosition explosionOriginCellGridPosition = linkedCellGridPositionList[0];

                    //int explosionX = explosionOriginCellGridPosition.GetX();
                    //int explosionY = explosionOriginCellGridPosition.GetY();

                    // Explode all 8 neighbours
                    //explosionGridPositionList.Add(new Vector2Int(explosionX - 1, explosionY - 1));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX + 0, explosionY - 1));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX + 1, explosionY - 1));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX - 1, explosionY + 0));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX + 1, explosionY + 0));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX - 1, explosionY + 1));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX + 0, explosionY + 1));
                    //explosionGridPositionList.Add(new Vector2Int(explosionX + 1, explosionY + 1));
                //}

                foundMatch = true;
            }

            //bool spawnExplosion = match4Explosions;

            //if (spawnExplosion)
            //{
            //    foreach (Vector2Int explosionGridPosition in explosionGridPositionList)
            //    {
            //        if (IsValidPosition(explosionGridPosition.x, explosionGridPosition.y))
            //        {
            //            CellGridPosition cellGridPosition = grid.GetGridObject(explosionGridPosition.x, explosionGridPosition.y);
            //            TryDestroyCellGridPosition(cellGridPosition);
            //        }
            //    }
            //}

            OnScoreChanged?.Invoke(this, EventArgs.Empty);

            if (!foundMatch)
            {
                savedStartCell = null;
                savedEndCell = null;
            }

            return foundMatch;
        }

        private void TryDestroyCellGridPosition(CellGridPosition cellGridPosition)
        {
            if (cellGridPosition.HasCellGrid())
            {
                //score += 100;

                cellGridPosition.DestroyCell();
                OnCellGridPositionDestroyed?.Invoke(cellGridPosition, EventArgs.Empty);
                cellGridPosition.ClearCellGrid();
            }

            //if (cellGridPosition.HasGlass())
            //{
            //    score += 100;

            //    cellGridPosition.DestroyGlass();
            //    OnGlassDestroyed?.Invoke(this, EventArgs.Empty);
            //}
        }

        public List<List<CellGridPosition>> GetAllCellLinks()
        {
            // Finds all the links with the current grid
            List<List<CellGridPosition>> allLinkedCellGridPositionList = new List<List<CellGridPosition>>();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (HasCellLink(x, y))
                    {
                        List<CellGridPosition> linkedCellGridPositionList = GetCellLinks(x, y);

                        if (allLinkedCellGridPositionList.Count == 0)
                        {
                            // First one
                            allLinkedCellGridPositionList.Add(linkedCellGridPositionList);
                        }
                        else
                        {
                            bool uniqueNewLink = true;

                            foreach (List<CellGridPosition> tmpLinkedCellGridPositionList in allLinkedCellGridPositionList)
                            {
                                if (linkedCellGridPositionList.Count == tmpLinkedCellGridPositionList.Count)
                                {
                                    // Same number of links
                                    // Are they all the same?
                                    bool allTheSame = true;
                                    for (int i = 0; i < linkedCellGridPositionList.Count; i++)
                                    {
                                        if (linkedCellGridPositionList[i] == tmpLinkedCellGridPositionList[i])
                                        {
                                            // This one is the same, link is not unique
                                        }
                                        else
                                        {
                                            // These don't match
                                            allTheSame = false;
                                            break;
                                        }
                                    }

                                    if (allTheSame)
                                    {
                                        // Nodes are all the same, not a new unique link
                                        uniqueNewLink = false;
                                    }
                                }
                            }

                            // Add to the total list if it's a unique link
                            if (uniqueNewLink)
                            {
                                allLinkedCellGridPositionList.Add(linkedCellGridPositionList);
                            }
                        }
                    }
                }
            }

            return allLinkedCellGridPositionList;
        }

        public void FallCellIntoEmptyPositions()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CellGridPosition cellGridPosition = grid.GetGridObject(x, y);

                    if (!cellGridPosition.IsEmpty())
                    {
                        // Grid Position has Cell
                        for (int i = y - 1; i >= 0; i--)
                        {
                            CellGridPosition nextCellGridPosition = grid.GetGridObject(x, i);
                            if (nextCellGridPosition.IsEmpty())
                            {
                                cellGridPosition.GetCellGrid().SetCellXY(x, i);
                                nextCellGridPosition.SetCellGrid(cellGridPosition.GetCellGrid());
                                cellGridPosition.ClearCellGrid();

                                cellGridPosition = nextCellGridPosition;
                            }
                            else
                            {
                                // Next Grid Position is not empty, stop looking
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void SpawnNewMissingGridPositions()
        {
            bool savedStartCellCombined = false;
            bool savedEndCellCombined = false;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CellGridPosition cellGridPosition = grid.GetGridObject(x, y);

                    if (cellGridPosition.IsEmpty())
                    {
                        DemoCell cell = cell = level.GetRevealCell(); ;

                        // Optimize
                        if (savedStartCell != null && cellGridPosition.GetX() == savedStartCell.GetX() && cellGridPosition.GetY() == savedStartCell.GetY())
                        {
                            int cellIndex = savedStartCell.GetCellIndex() + 1;
                            if (cellIndex <= 5)
                            {
                                cell = level.GetCellByIndex(cellIndex);
                                savedStartCell = new SavedCellData(cell.cellName, x, y, cell.cellIndex, cell.cellType);
                                savedStartCellCombined = true;
                            }

                        } else if (savedEndCell != null && cellGridPosition.GetX() == savedEndCell.GetX() && cellGridPosition.GetY() == savedEndCell.GetY())
                        {
                            int cellIndex = savedEndCell.GetCellIndex() + 1;
                            if (cellIndex <= 5)
                            {
                                cell = level.GetCellByIndex(cellIndex);
                                savedEndCell = new SavedCellData(cell.cellName, x, y, cell.cellIndex, cell.cellType);
                                savedEndCellCombined = true;
                            }
                        }

                        CellGrid cellGrid = new CellGrid(cell, x, y);

                        cellGridPosition.SetCellGrid(cellGrid);

                        OnNewCellGridSpawned?.Invoke(cellGrid, new OnNewCellGridSpawnedEventArgs
                        {
                            cellGrid = cellGrid,
                            cellGridPosition = cellGridPosition,
                        });
                    }
                }
            }

            // Remove stored cell if it was not combined initially, not spawn new cell if it is combined later
            if (!savedStartCellCombined && savedEndCellCombined) savedStartCell = null; // Double check to exclude step over case when only start cell is present
            if (!savedEndCellCombined) savedEndCell = null;
        }

        private DemoCell GetCell(int x, int y)
        {
            if (!IsValidPosition(x, y)) return null;

            CellGridPosition cellGridPosition = grid.GetGridObject(x, y);

            if (cellGridPosition.GetCellGrid() == null) return null;

            return cellGridPosition.GetCellGrid().GetCell();
        }

        public class CellGridPosition
        {
            private CellGrid cellGrid;

            private Grid<CellGridPosition> grid;
            private int x;
            private int y;

            public CellGridPosition(Grid<CellGridPosition> grid, int x, int y)
            {
                this.grid = grid;
                this.x = x;
                this.y = y;
            }

            public void SetCellGrid(CellGrid cellGrid)
            {
                this.cellGrid = cellGrid;
                grid.TriggerGridObjectChanged(x, y);
            }

            public int GetX()
            {
                return x;
            }

            public int GetY()
            {
                return y;
            }

            public Vector3 GetWorldPosition()
            {
                return grid.GetWorldPosition(x, y);
            }

            public CellGrid GetCellGrid()
            {
                return cellGrid;
            }

            public void ClearCellGrid()
            {
                cellGrid = null;
            }

            public void DestroyCell()
            {
                cellGrid?.Destroy();
                grid.TriggerGridObjectChanged(x, y);
            }

            public bool HasCellGrid()
            {
                return cellGrid != null;
            }

            public bool IsEmpty()
            {
                return cellGrid == null;
            }

            public override string ToString()
            {
                return cellGrid?.ToString();
            }
        }

        public class CellGrid
        {
            public event EventHandler OnDestroyed;

            private DemoCell cell;
            private int x;
            private int y;
            private bool isDestroyed;

            public CellGrid(DemoCell cell, int x, int y)
            {
                this.cell = cell;
                this.x = x;
                this.y = y;

                isDestroyed = false;
            }

            public DemoCell GetCell()
            {
                return cell;
            }

            public Vector3 GetWorldPosition()
            {
                return new Vector3(x, y);
            }

            public void SetCellXY(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public void Destroy()
            {
                isDestroyed = true;
                OnDestroyed?.Invoke(this, EventArgs.Empty);
            }

            public override string ToString()
            {
                //return isDestroyed.ToString();
                return x + ", " + y;
            }

        }
    }

    public class SavedCellData
    {
        private string cellName;
        private int cellIndex;
        private int x;
        private int y;
        private DemoCell.Type cellType;

        public SavedCellData(string cellName, int x, int y, int cellIndex, DemoCell.Type cellType)
        {
            this.cellName = cellName;
            this.x = x;
            this.y = y;
            this.cellIndex = cellIndex;
            this.cellType = cellType;
        }

        public string GetCellName()
        {
            return cellName;
        }

        public int GetCellIndex()
        {
            return cellIndex;
        }

        public DemoCell.Type GetCellType()
        {
            return cellType;
        }

        public int GetX()
        {
            return x;
        }

        public int GetY()
        {
            return y;
        }
    }
}
