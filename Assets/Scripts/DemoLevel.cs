using System.Collections.Generic;
using UnityEngine;

namespace MatchDemo
{
    [CreateAssetMenu()]
    public class DemoLevel : ScriptableObject
    {
        public List<DemoCell> cellList;
        public int width;
        public int height;
        public List<LevelGridPosition> levelGridPositionList;

        [System.Serializable]
        public class LevelGridPosition
        {
            public DemoCell cell;
            public int x;
            public int y;
        }

        public DemoCell GetRandomCell()
        {
            return cellList[UnityEngine.Random.Range(0, cellList.Count)];
        }

        public DemoCell GetRandomMatchCell()
        {
            List<DemoCell> tmpList = cellList.FindAll(cell => cell.cellType == DemoCell.Type.Match);

            return tmpList[UnityEngine.Random.Range(0, tmpList.Count)];
        }

        public DemoCell GetRevealCell()
        {
            DemoCell revelCell = cellList.Find(cell => cell.cellType == DemoCell.Type.Reveal);

            return revelCell;
        }

        public DemoCell GetCellByType(DemoCell.Type cellType)
        {
            DemoCell revelCell = cellList.Find(cell => cell.cellType == cellType);

            return revelCell;
        }
    }
}
