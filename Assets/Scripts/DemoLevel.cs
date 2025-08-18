using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MatchDemo
{
    [CreateAssetMenu]
    public class DemoLevel : ScriptableObject
    {
        public List<DemoCell> cellList;
        public int width;
        public int height;
        public bool generateRandomCells;
        public bool canDragAndDrop;
        public List<LevelGridPosition> levelGridPositionList;

        public int GetCellCount()
        {
            return cellList.Count;
        }

        public DemoCell GetRandomCell()
        {
            return cellList[Random.Range(0, cellList.Count)];
        }

        public DemoCell GetRandomMatchCell()
        {
            var tmpList = cellList.FindAll(cell => cell.cellType == DemoCell.Type.Match);

            return tmpList[Random.Range(0, tmpList.Count)];
        }

        public DemoCell GetRevealCell()
        {
            var revealCell = cellList.Find(cell => cell.cellType == DemoCell.Type.Reveal);

            return revealCell;
        }

        public DemoCell GetCombineCell()
        {
            var combineCell = cellList.Find(cell => cell.cellType == DemoCell.Type.Combine);

            return combineCell;
        }

        public DemoCell GetCellByName(string cellName)
        {
            var cellByName = cellList.Find(cell => cell.cellName == cellName);

            return cellByName;
        }

        public DemoCell GetCellByIndex(int cellIndex)
        {
            var cellByIndex = cellList.Find(cell => cell.cellIndex == cellIndex);

            return cellByIndex;
        }

        [Serializable]
        public class LevelGridPosition
        {
            public DemoCell cell;
            public int x;
            public int y;
        }
    }
}