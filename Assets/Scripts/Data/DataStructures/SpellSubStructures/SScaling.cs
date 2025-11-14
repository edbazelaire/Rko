using Enums;
using System.Collections.Generic;
using System;
using Tools;
using UnityEngine;
using MyBox;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SScaling
    {
        public float                    BaseValue;
        public float                    ScalingValue;
        public EScalingType             ScalingType;
        public ERoundingType            RoundingType;
        public List<SScalingTreshold>   ScalingTresholds;

        public SScaling(
            float baseValue                         = 0f, 
            float scalingValue                      = 0f, 
            EScalingType scalingType                = EScalingType.Exponential,
            ERoundingType roundingType              = ERoundingType.Round,
            List<SScalingTreshold> scalingTresholds = null
        )
        {
            BaseValue           = baseValue;
            ScalingValue        = scalingValue;
            ScalingType         = scalingType;
            RoundingType        = roundingType;
            ScalingTresholds    = scalingTresholds ?? new List<SScalingTreshold>();
        }

        public float Get(int level)
        {
            return RoundValue(GetScaledValue(level));
        }

        float GetScaledValue(int level)
        {
            if (! ScalingTresholds.IsNullOrEmpty())
                return GetTreshold(level);

            switch (ScalingType)
            {
                case EScalingType.Exponential:
                    return BaseValue * (float)Math.Pow(1 + ScalingValue, level - 1);

                case EScalingType.Additive:
                    return BaseValue + ScalingValue * (level - 1);

                case EScalingType.Multiply:
                    return BaseValue * (1 + ScalingValue * (level - 1));

                default:
                    ErrorHandler.Warning("Unhandled case : " + ScalingType);
                    return 1f;
            }
        }

        float RoundValue(float value)
        {
            switch (RoundingType)
            {
                case ERoundingType.None:
                    return value;

                case ERoundingType.Floor:
                    return Mathf.Floor(value);

                case ERoundingType.Round:
                    return Mathf.Round(value);

                case ERoundingType.Ceil:
                    return Mathf.Ceil(value);

                default:
                    ErrorHandler.Warning("Unhandled case : " + RoundingType);
                    return value;
            }
        }

        float GetTreshold(int level)
        {
            float value = BaseValue;
            foreach (SScalingTreshold treshold in ScalingTresholds)
            {
                // level is sup to current level : return current value
                if (treshold.Level > level)
                    return value;

                // set value as current value
                value = treshold.Value;
            }

            return value;
        }

        public EScalingDirection GetScalingDirection()
        {
            if (ScalingValue > 0)
            {
                return EScalingDirection.Up;
            }

            if (ScalingValue < 0)
            {
                return EScalingDirection.Down;
            }

            // -- getting the scaling direction of a threshold value can be challenging : lets assume that all thresholds are "UP"
            if (ScalingTresholds.Count > 0)
            {
                return EScalingDirection.Up;
            }

            return EScalingDirection.None;
        }
    }
}