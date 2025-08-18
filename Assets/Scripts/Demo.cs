using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchDemo
{
    public class Demo : MonoBehaviour
    {
        [SerializeField] private DemoLevel level;
        [SerializeField] private Vector2 gridPositionOffset;
        private bool canDragAndDrop;
        private bool generateRandomCells;
        private Grid<CellGridPosition> grid;
        private int gridHeight;

        private int gridWidth;
        private int maxCellInde;

        private int maxIndex;
        private SavedCellData savedEndCell;

        private SavedCellData savedStartCell;

        private void Start()
        {
            InitLevel(level);
        }

        public event EventHandler OnCellGridPositionDestroyed;
        public event EventHandler<OnNewCellGridSpawnedEventArgs> OnNewCellGridSpawned;
        public event EventHandler<OnLevelSetEventArgs> OnLevelSet;
        public event EventHandler<OnStepOverEventArgs> OnStepOver;
        public event EventHandler<OnDropEventArgs> OnDrop;

        public event EventHandler TryAndFindMatches;

        //public event EventHandler OnGlassDestroyed;
        public event EventHandler OnMoveUsed;

        //public event EventHandler OnOutOfMoves;
        public event EventHandler OnScoreChanged;

        public void InitLevel(DemoLevel level)
        {
            this.level = level;

            gridWidth = level.width;
            gridHeight = level.height;
            generateRandomCells = level.generateRandomCells;
            canDragAndDrop = level.canDragAndDrop;
            maxIndex = level.GetCellCount() - 1;

            grid = new Grid<CellGridPosition>(gridWidth, gridHeight, 1f, Vector2.zero + gridPositionOffset,
                (g, x, y) => new CellGridPosition(g, x, y));

            var findUniqueLevel = true;
            var count = 0;

            // Ensure starting level without matches
            while (findUniqueLevel)
            {
                count++;
                // Initialize Grid
                for (var x = 0; x < gridWidth; x++)
                for (var y = 0; y < gridHeight; y++)
                {
                    DemoLevel.LevelGridPosition levelGridPosition = null;

                    foreach (var tmpLevelGridPosition in level.levelGridPositionList)
                        if (tmpLevelGridPosition.x == x && tmpLevelGridPosition.y == y)
                        {
                            levelGridPosition = tmpLevelGridPosition;
                            break;
                        }

                    DemoCell cell = null;

                    // TODO - Clean Up
                    if (levelGridPosition == null)
                    {
                        if (generateRandomCells)
                            cell = level.GetRandomCell();
                        else
                            cell = level.GetCombineCell();
                    }
                    else
                    {
                        cell = levelGridPosition.cell;
                    }


                    var cellGrid = new CellGrid(cell, x, y);
                    grid.GetGridObject(x, y).SetCellGrid(cellGrid);
                }

                var allLinkedCellGridPositionList = GetAllCellLinks();

                if (allLinkedCellGridPositionList.Count == 0) findUniqueLevel = false;
            }

            Debug.Log(count);

            OnLevelSet?.Invoke(this, new OnLevelSetEventArgs { level = level, grid = grid });
        }

        public Vector3 GetGridPositionOffset()
        {
            return new Vector3(gridPositionOffset.x, gridPositionOffset.y, 0);
        }

        public bool CanDragAndDrop()
        {
            return canDragAndDrop;
        }

        public bool CanRevealGridPosition(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

            if (startX != endX || startY != endY) return false; // Not the same position

            var revealGridPosition = grid.GetGridObject(startX, startY);

            if (!revealGridPosition.HasCellGrid()) return false;

            var revealCellGrid = revealGridPosition.GetCellGrid();
            var revealCell = revealCellGrid.GetCell();

            if (revealCell.cellType != DemoCell.Type.Reveal) return false;

            return true;
        }

        public bool CanRevealGridPosition(int startX, int startY)
        {
            if (!IsValidPosition(startX, startY)) return false; // Invalid Position

            var revealGridPosition = grid.GetGridObject(startX, startY);

            if (!revealGridPosition.HasCellGrid()) return false;

            var revealCellGrid = revealGridPosition.GetCellGrid();
            var revealCell = revealCellGrid.GetCell();

            if (revealCell.cellType != DemoCell.Type.Reveal) return false;

            return true;
        }

        public bool CanSwapGridPositions(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

            if (startX == endX && startY == endY) return false; // Same Position

            var startSwapGridPosition = grid.GetGridObject(startX, startY);
            var endSwapGridPosition = grid.GetGridObject(endX, endY);

            var startSwapCellGrid = startSwapGridPosition.GetCellGrid();
            var endSwapCellGrid = endSwapGridPosition.GetCellGrid();

            if (startSwapCellGrid == null || endSwapCellGrid == null) return false; // Cannot move to empty space

            var startSwapCell = startSwapCellGrid.GetCell();
            var endSwapCell = endSwapCellGrid.GetCell();

            if (startSwapCell.cellType != DemoCell.Type.Match || endSwapCell.cellType != DemoCell.Type.Match)
                return false; // One of the swap cells are invalid

            SwapGridPositions(startX, startY, endX, endY, true); // Swap

            var hasLinkAfterSwap = HasCellLink(startX, startY) || HasCellLink(endX, endY);

            SwapGridPositions(startX, startY, endX, endY, true); // Swap Back

            return hasLinkAfterSwap;
        }

        public bool CanStepOverGridPositions(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

            if (startX == endX && startY == endY) return false; // Same Position

            var startSwapGridPosition = grid.GetGridObject(startX, startY);
            var endSwapGridPosition = grid.GetGridObject(endX, endY);

            var startSwapCellGrid = startSwapGridPosition.GetCellGrid();
            var endSwapCellGrid = endSwapGridPosition.GetCellGrid();

            if (startSwapCellGrid == null || endSwapCellGrid == null) return false; // Cannot move to empty space

            var startSwapCell = startSwapCellGrid.GetCell();
            var endSwapCell = endSwapCellGrid.GetCell();

            if (startSwapCell.cellType == DemoCell.Type.Reveal || endSwapCell.cellType == DemoCell.Type.Reveal)
                return false; // Can only step over match cells
            if (startSwapCell.cellType != endSwapCell.cellType)
                return false; // One of the swap cells are not of the same type
            if (startSwapCell.cellName != endSwapCell.cellName)
                return false; // One of the swap cells are not of the same name

            var horizontalDirection = endX - startX;
            var verticalDirection = endY - startY;

            var stepOverPositionX = endX + horizontalDirection;
            var stepOverPositionY = endY + verticalDirection;

            if (!IsValidPosition(stepOverPositionX, stepOverPositionY)) return false; // Invalid Position
            if (endX == stepOverPositionX && endY == stepOverPositionY) return false; // Same Position

            var stepOverGridPosition = grid.GetGridObject(stepOverPositionX, stepOverPositionY);
            var stepOverCellGrid = stepOverGridPosition.GetCellGrid();

            if (stepOverCellGrid == null) return false; // Cannot move to empty space

            var stepOverCell = stepOverCellGrid.GetCell();

            if (stepOverCell.cellType != DemoCell.Type.Reveal) return false; // Cannot move to empty space

            return true;
        }

        public void SwapGridPositions(int startX, int startY, int endX, int endY, bool swapCheck = false)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return; // Invalid Position

            if (startX == endX && startY == endY) return; // Same Position

            var startCellGridPosition = grid.GetGridObject(startX, startY);
            var endCellGridPosition = grid.GetGridObject(endX, endY);

            var startCellGrid = startCellGridPosition.GetCellGrid();
            var endCellGrid = endCellGridPosition.GetCellGrid();

            if (startCellGrid == null || endCellGrid == null) return; // Cannot move to empty space

            startCellGrid.SetCellXY(endX, endY);
            endCellGrid.SetCellXY(startX, startY);

            var startSwapCell = startCellGrid.GetCell();
            var endSwapCell = endCellGrid.GetCell();

            // Save swapped cell data for later comparison for reveal and combine
            if (!swapCheck)
            {
                savedStartCell = new SavedCellData(startCellGrid.GetCell().cellName, endX, endY,
                    startCellGrid.GetCell().cellIndex, startSwapCell.cellType);
                savedEndCell = new SavedCellData(endCellGrid.GetCell().cellName, startX, startY,
                    endCellGrid.GetCell().cellIndex, endSwapCell.cellType);
            }

            startCellGridPosition.SetCellGrid(endCellGrid);
            endCellGridPosition.SetCellGrid(startCellGrid);
        }

        public void StepOverGridPositions(int startX, int startY, int endX, int endY)
        {
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return; // Invalid Position

            if (startX == endX && startY == endY) return; // Same Position

            var startSwapGridPosition = grid.GetGridObject(startX, startY);
            var endSwapGridPosition = grid.GetGridObject(endX, endY);

            var startSwapCellGrid = startSwapGridPosition.GetCellGrid();
            var endSwapCellGrid = endSwapGridPosition.GetCellGrid();

            if (startSwapCellGrid == null || endSwapCellGrid == null) return; // Cannot move to empty space

            var startSwapCell = startSwapCellGrid.GetCell();
            var endSwapCell = endSwapCellGrid.GetCell();

            if (startSwapCell.cellType != endSwapCell.cellType)
                return; // One of the swap cells are not of the same type
            if (startSwapCell.cellName != endSwapCell.cellName)
                return; // One of the swap cells are not of the same name

            var horizontalDirection = endX - startX;
            var verticalDirection = endY - startY;

            var stepOverPositionX = endX + horizontalDirection;
            var stepOverPositionY = endY + verticalDirection;

            if (!IsValidPosition(stepOverPositionX, stepOverPositionY)) return; // Invalid Position
            if (endX == stepOverPositionX && endY == stepOverPositionY) return; // Same Position

            var stepOverGridPosition = grid.GetGridObject(stepOverPositionX, stepOverPositionY);
            var stepOverCellGrid = stepOverGridPosition.GetCellGrid();

            if (stepOverCellGrid == null) return; // Cannot move to empty space

            var stepOverCell = stepOverCellGrid.GetCell();

            if (stepOverCell.cellType != DemoCell.Type.Reveal) return; // Cannot move to empty space

            // Remove old cells
            startSwapGridPosition.ClearCellGrid();
            TryDestroyCellGridPosition(endSwapGridPosition);
            TryDestroyCellGridPosition(stepOverGridPosition);

            // Handle cell movement to reveal position
            startSwapCellGrid.SetCellXY(stepOverPositionX, stepOverPositionY);

            // Save swapped cell data for later comparison for reveal and combine
            savedStartCell = new SavedCellData(startSwapCellGrid.GetCell().cellName, stepOverPositionX,
                stepOverPositionY, startSwapCellGrid.GetCell().cellIndex, stepOverCell.cellType);
            savedEndCell = null;

            stepOverGridPosition.SetCellGrid(startSwapCellGrid);

            // Move stepped over cell to combine bar
            OnStepOver?.Invoke(this, new OnStepOverEventArgs { cell = endSwapCell });
        }

        public void RevealGridPosition(int X, int Y, SavedCellData savedCellData = null)
        {
            if (!IsValidPosition(X, Y)) return; // Invalid Position

            var revealCellGridPosition = grid.GetGridObject(X, Y);

            var revealCellGrid = revealCellGridPosition.GetCellGrid();

            if (!revealCellGridPosition.HasCellGrid()) return; // Cannot reveal empty space

            // Remove old cell
            TryDestroyCellGridPosition(revealCellGridPosition);

            var newCell = level.GetRandomMatchCell();

            // Spawn new cell
            if (savedCellData is SavedCellData) newCell = level.GetCellByName(savedCellData.GetCellName());

            var newCellGrid = new CellGrid(newCell, X, Y);
            grid.GetGridObject(X, Y).SetCellGrid(newCellGrid);

            // Update saved cell data for reveal and combine
            savedStartCell = new SavedCellData(newCell.cellName, X, Y, newCell.cellIndex, newCell.cellType);
            savedEndCell = null;

            // Trigger new cell spawn to update visual
            OnNewCellGridSpawned?.Invoke(newCellGrid, new OnNewCellGridSpawnedEventArgs
            {
                cellGrid = newCellGrid,
                cellGridPosition = revealCellGridPosition
            });

            if (savedCellData is SavedCellData) TryAndFindMatches?.Invoke(this, EventArgs.Empty);
        }

        private bool IsValidPosition(int x, int y)
        {
            if (x < 0 || y < 0 ||
                x >= gridWidth || y >= gridHeight)
                // Invalid position
                return false;
            return true;
        }

        public bool HasCellLink(int x, int y)
        {
            var linkedCellGridPositionList = GetCellLinks(x, y);
            return linkedCellGridPositionList != null && linkedCellGridPositionList.Count >= 3;
        }

        public void UseMove()
        {
            //moveCount--;
            OnMoveUsed?.Invoke(this, EventArgs.Empty);
        }

        public Grid<CellGridPosition> GetGrid()
        {
            return grid;
        }

        public List<CellGridPosition> GetCellLinks(int x, int y)
        {
            var cell = GetCell(x, y);

            if (cell == null || cell.cellType != DemoCell.Type.Match) return null;

            var rightLinkAmount = 0;
            for (var i = 1; i < gridWidth; i++)
                if (IsValidPosition(x + i, y))
                {
                    var nextCell = GetCell(x + i, y);
                    if (nextCell == cell)
                        // Same Cell
                        rightLinkAmount++;
                    else
                        // Not same Cell
                        break;
                }
                else
                {
                    // Invalid position
                    break;
                }

            var leftLinkAmount = 0;
            for (var i = 1; i < gridWidth; i++)
                if (IsValidPosition(x - i, y))
                {
                    var nextCell = GetCell(x - i, y);
                    if (nextCell == cell)
                        // Same Cell
                        leftLinkAmount++;
                    else
                        // Not same Cell
                        break;
                }
                else
                {
                    // Invalid position
                    break;
                }

            var horizontalLinkAmount = 1 + leftLinkAmount + rightLinkAmount; // This Cell + left + right

            if (horizontalLinkAmount >= 3)
            {
                // Has 3 horizontal linked cells
                var linkedCellGridPositionList = new List<CellGridPosition>();
                var leftMostX = x - leftLinkAmount;
                for (var i = 0; i < horizontalLinkAmount; i++)
                    linkedCellGridPositionList.Add(grid.GetGridObject(leftMostX + i, y));
                return linkedCellGridPositionList;
            }

            var upLinkAmount = 0;
            for (var i = 1; i < gridHeight; i++)
                if (IsValidPosition(x, y + i))
                {
                    var nextCell = GetCell(x, y + i);
                    if (nextCell == cell)
                        // Same Cell
                        upLinkAmount++;
                    else
                        // Not same Cell
                        break;
                }
                else
                {
                    // Invalid position
                    break;
                }

            var downLinkAmount = 0;
            for (var i = 1; i < gridHeight; i++)
                if (IsValidPosition(x, y - i))
                {
                    var nextCell = GetCell(x, y - i);
                    if (nextCell == cell)
                        // Same Cell
                        downLinkAmount++;
                    else
                        // Not same Cell
                        break;
                }
                else
                {
                    // Invalid position
                    break;
                }

            var verticalLinkAmount = 1 + downLinkAmount + upLinkAmount; // This Cell + down + up

            if (verticalLinkAmount >= 3)
            {
                // Has 3 vertical linked cells
                var linkedCellGridPositionList = new List<CellGridPosition>();
                var downMostY = y - downLinkAmount;
                for (var i = 0; i < verticalLinkAmount; i++)
                    linkedCellGridPositionList.Add(grid.GetGridObject(x, downMostY + i));
                return linkedCellGridPositionList;
            }

            // No links
            return null;
        }

        public bool TryFindMatchesAndDestroyThem()
        {
            var allLinkedCellGridPositionList = GetAllCellLinks();

            var foundMatch = false;

            //List<Vector2Int> explosionGridPositionList = new List<Vector2Int>();

            foreach (var linkedCellGridPositionList in allLinkedCellGridPositionList)
            {
                foreach (var cellGridPosition in linkedCellGridPositionList)
                    TryDestroyCellGridPosition(cellGridPosition);

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
            var allLinkedCellGridPositionList = new List<List<CellGridPosition>>();

            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
                if (HasCellLink(x, y))
                {
                    var linkedCellGridPositionList = GetCellLinks(x, y);

                    if (allLinkedCellGridPositionList.Count == 0)
                    {
                        // First one
                        allLinkedCellGridPositionList.Add(linkedCellGridPositionList);
                    }
                    else
                    {
                        var uniqueNewLink = true;

                        foreach (var tmpLinkedCellGridPositionList in allLinkedCellGridPositionList)
                            if (linkedCellGridPositionList.Count == tmpLinkedCellGridPositionList.Count)
                            {
                                // Same number of links
                                // Are they all the same?
                                var allTheSame = true;
                                for (var i = 0; i < linkedCellGridPositionList.Count; i++)
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

                                if (allTheSame)
                                    // Nodes are all the same, not a new unique link
                                    uniqueNewLink = false;
                            }

                        // Add to the total list if it's a unique link
                        if (uniqueNewLink) allLinkedCellGridPositionList.Add(linkedCellGridPositionList);
                    }
                }

            return allLinkedCellGridPositionList;
        }

        public void FallCellIntoEmptyPositions()
        {
            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
            {
                var cellGridPosition = grid.GetGridObject(x, y);

                if (!cellGridPosition.IsEmpty())
                    // Grid Position has Cell
                    for (var i = y - 1; i >= 0; i--)
                    {
                        var nextCellGridPosition = grid.GetGridObject(x, i);
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

        public void SpawnNewMissingGridPositions()
        {
            var savedStartCellCombined = false;
            var savedEndCellCombined = false;

            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
            {
                var cellGridPosition = grid.GetGridObject(x, y);

                if (cellGridPosition.IsEmpty())
                {
                    DemoCell cell = cell = level.GetRevealCell();

                    if (!level.generateRandomCells) cell = level.GetCombineCell();

                    // Optimize
                    if (savedStartCell != null && cellGridPosition.GetX() == savedStartCell.GetX() &&
                        cellGridPosition.GetY() == savedStartCell.GetY())
                    {
                        var cellIndex = savedStartCell.GetCellIndex() + 1;
                        if (cellIndex <= maxIndex)
                        {
                            cell = level.GetCellByIndex(cellIndex);
                            savedStartCell = new SavedCellData(cell.cellName, x, y, cell.cellIndex, cell.cellType);
                            savedStartCellCombined = true;
                        }
                    }
                    else if (savedEndCell != null && cellGridPosition.GetX() == savedEndCell.GetX() &&
                             cellGridPosition.GetY() == savedEndCell.GetY())
                    {
                        var cellIndex = savedEndCell.GetCellIndex() + 1;
                        if (cellIndex <= maxIndex)
                        {
                            cell = level.GetCellByIndex(cellIndex);
                            savedEndCell = new SavedCellData(cell.cellName, x, y, cell.cellIndex, cell.cellType);
                            savedEndCellCombined = true;
                        }
                    }

                    var cellGrid = new CellGrid(cell, x, y);

                    cellGridPosition.SetCellGrid(cellGrid);

                    OnNewCellGridSpawned?.Invoke(cellGrid, new OnNewCellGridSpawnedEventArgs
                    {
                        cellGrid = cellGrid,
                        cellGridPosition = cellGridPosition
                    });
                }
            }

            // Remove stored cell if it was not combined initially, not spawn new cell if it is combined later
            if (!savedStartCellCombined && savedEndCellCombined)
                savedStartCell = null; // Double check to exclude step over case when only start cell is present
            if (!savedEndCellCombined) savedEndCell = null;
        }

        // TODO - Rework
        public void SpawnNewCellOnClosestGridPosition(DemoCell newCell)
        {
            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
            {
                var cellGridPosition = grid.GetGridObject(x, y);
                var cellGrid = cellGridPosition.GetCellGrid();
                var cell = cellGrid.GetCell();

                if (cell != null && cell.cellType == DemoCell.Type.Combine)
                {
                    TryDestroyCellGridPosition(cellGridPosition);

                    var newCellGrid = new CellGrid(newCell, x, y);
                    grid.GetGridObject(x, y).SetCellGrid(newCellGrid);

                    savedStartCell = new SavedCellData(newCellGrid.GetCell().cellName, x, y,
                        newCellGrid.GetCell().cellIndex, newCell.cellType);
                    ;

                    // Trigger new cell spawn to update visual
                    OnNewCellGridSpawned?.Invoke(newCellGrid, new OnNewCellGridSpawnedEventArgs
                    {
                        cellGrid = newCellGrid,
                        cellGridPosition = cellGridPosition
                    });

                    TryAndFindMatches?.Invoke(this, EventArgs.Empty);

                    return;
                }
            }
        }

        // TODO - Refactor
        public bool CanDrag(int x, int y)
        {
            if (!IsValidPosition(x, y)) return false; // Invalid Position

            var dragCellGridPosition = grid.GetGridObject(x, y);
            var dragCellGrid = dragCellGridPosition.GetCellGrid();

            if (dragCellGrid == null) return false; // Cannot drag to empty space

            var dragCell = dragCellGrid.GetCell();

            if (dragCell.cellType != DemoCell.Type.Match) return false; // Drag cell is invalid

            return true;
        }

        // TODO - Refactor
        public void StartDrag(int x, int y)
        {
            var dragCellGridPosition = grid.GetGridObject(x, y);
            var dragCellGrid = dragCellGridPosition.GetCellGrid();

            if (dragCellGrid == null) return; // Cannot drag to empty space

            var dragCell = dragCellGrid.GetCell();

            if (dragCell.cellType != DemoCell.Type.Match) return; // Drag cell is invalid

            savedStartCell = new SavedCellData(dragCell.cellName, x, y, dragCell.cellIndex, dragCell.cellType);
        }

        // TODO - Refactor
        public void Drag(int dragX, int dragY)
        {
            if (savedStartCell == null) return;

            var x = savedStartCell.GetX();
            var y = savedStartCell.GetY();

            var dragCellGridPosition = grid.GetGridObject(x, y);
            var dragCellGrid = dragCellGridPosition.GetCellGrid();

            if (dragCellGrid == null) return; // Cannot drag to empty space

            var dragCell = dragCellGrid.GetCell();

            if (dragCell.cellType != DemoCell.Type.Match) return; // Drag cell is invalid

            dragCellGrid.SetCellXY(dragX, dragY);
        }

        // TODO - Refactor
        public void Drop(Vector3 dropWorldPosition)
        {
            if (savedStartCell == null) return;

            OnDrop?.Invoke(this,
                new OnDropEventArgs { savedCellData = savedStartCell, mouseWorldPosition = dropWorldPosition });
        }

        public void DropSuccess()
        {
            if (savedStartCell == null) return;

            var dragCellGridPosition = grid.GetGridObject(savedStartCell.GetX(), savedStartCell.GetY());

            TryDestroyCellGridPosition(dragCellGridPosition);

            savedStartCell = null;
        }

        public void DropFailed()
        {
            if (savedStartCell == null) return;

            var dragCellGridPosition = grid.GetGridObject(savedStartCell.GetX(), savedStartCell.GetY());
            var dragCellGrid = dragCellGridPosition.GetCellGrid();

            dragCellGrid.SetCellXY(savedStartCell.GetX(), savedStartCell.GetY());
            savedStartCell = null;
        }

        private DemoCell GetCell(int x, int y)
        {
            if (!IsValidPosition(x, y)) return null;

            var cellGridPosition = grid.GetGridObject(x, y);

            if (cellGridPosition.GetCellGrid() == null) return null;

            return cellGridPosition.GetCellGrid().GetCell();
        }

        public class OnNewCellGridSpawnedEventArgs : EventArgs
        {
            public CellGrid cellGrid;
            public CellGridPosition cellGridPosition;
        }

        public class OnLevelSetEventArgs : EventArgs
        {
            public Grid<CellGridPosition> grid;
            public DemoLevel level;
        }

        public class OnStepOverEventArgs : EventArgs
        {
            public DemoCell cell;
        }

        public class OnDropEventArgs : EventArgs
        {
            public Vector3 mouseWorldPosition;
            public SavedCellData savedCellData;
        }

        public class CellGridPosition
        {
            private readonly Grid<CellGridPosition> grid;
            private readonly int y;
            private readonly int x;
            private CellGrid cellGrid;

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
            private readonly DemoCell cell;
            private int y;
            private bool isDestroyed;
            private int x;

            public CellGrid(DemoCell cell, int x, int y)
            {
                this.cell = cell;
                this.x = x;
                this.y = y;

                isDestroyed = false;
            }

            public event EventHandler OnDestroyed;

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
        private readonly int cellIndex;
        private readonly string cellName;
        private readonly DemoCell.Type cellType;
        private readonly int y;
        private readonly int x;

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