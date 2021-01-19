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
    }
}
