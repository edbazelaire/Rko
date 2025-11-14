using System;
using UnityEngine;

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

        public readonly Vector2 AsVector2 => new Vector2(X, Y);
    }
}