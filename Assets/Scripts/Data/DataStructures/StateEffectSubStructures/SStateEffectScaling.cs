using Enums;
using Game.Spells;
using System;
using UnityEngine;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public struct SStateEffectScaling
    {
        public EStateEffectProperty StateEffectProperty;
        public float ScalingFactor;

        public SStateEffectScaling(EStateEffectProperty stateEffectProperty, float scalingFactor = 1.1f)
        {
            StateEffectProperty = stateEffectProperty;
            ScalingFactor = scalingFactor;
        }
    }
}