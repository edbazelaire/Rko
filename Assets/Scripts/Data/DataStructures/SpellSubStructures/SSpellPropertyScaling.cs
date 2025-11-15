using Enums;
using System.Collections.Generic;
using System;
using Tools;
using UnityEngine;
using MyBox;

namespace Data.DataStructures.SpellSubStructures
{

    [Serializable]
    public struct SScalingTreshold
    {
        public int Level;
        public float Value;
    }


    [Serializable]
    public struct SSpellPropertyScaling
    {
        public ESpellProperty           Property;
        public float                    Value;
        public EScalingType             ScalingType;
        public ERoundingType            RoundingType;
        public List<SScalingTreshold>   ScalingTresholds;

        public SSpellPropertyScaling(
            ESpellProperty prop, 
            float value                             = 0f, 
            EScalingType scalingType                = EScalingType.Exponential,
            ERoundingType roundingType              = ERoundingType.Round,
            List<SScalingTreshold> scalingTresholds = null
        )
        {
            Property            = prop;
            Value               = value;
            ScalingType         = scalingType;
            RoundingType        = roundingType;
            ScalingTresholds    = scalingTresholds ?? new List<SScalingTreshold>();
        }

        public float Get(float value, int level)
        {
            return RoundValue(GetScaledValue(value, level));
        }

        float GetScaledValue(float value, int level)
        {
            if (! ScalingTresholds.IsNullOrEmpty())
                return GetTreshold(value, level);

            switch (ScalingType)
            {
                case EScalingType.Exponential:
                    return value * (float)Math.Pow(1 + Value, level - 1);

                case EScalingType.Additive:
                    return value + Value * (level - 1);

                case EScalingType.Multiply:
                    return value * (1 + Value * (level - 1));

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

        float GetTreshold(float value, int level)
        {
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
            if (Value > 0)
            {
                return EScalingDirection.Up;
            }

            if (Value < 0)
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