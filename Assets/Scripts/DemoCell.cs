using UnityEngine;

namespace MatchDemo
{
    [CreateAssetMenu()]
    public class DemoCell : ScriptableObject
    {
        public enum Type
        {
            Match,
            Reveal
        }

        public string cellName;
        public Sprite sprite;
        public Type cellType;
    }
}
