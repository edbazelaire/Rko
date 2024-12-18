using System;

namespace Assets.Scripts.Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SOffset
    {
        public float X;
        public float Y;

        public SOffset(float x = 0, float y = 0)
        {
            X = x;
            Y = y;
        }
    }
}