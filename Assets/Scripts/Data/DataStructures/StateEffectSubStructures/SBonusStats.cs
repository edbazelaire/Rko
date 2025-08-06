using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public class SBonusStats
    {
        public EStateEffectProperty StateEffectProperty;
        public float                BaseValue;
        public float                LevelScalingFactor;
        public float                StackScalingFactor;
        public List<string>         SpecialConditions;

        public EScalingDirection ScalingDirection
        {
            get
            {
                if (LevelScalingFactor > 0)
                    return EScalingDirection.Up;

                if (LevelScalingFactor < 0)
                    return EScalingDirection.Down;

                return EScalingDirection.None;
            }
        }

        public SBonusStats(EStateEffectProperty stateEffectProperty, float baseValue = 0f, float levelScalingFactor = 0.1f, float stackScalingFactor = 0.1f, List<string> specialConditions = default)
        {
            StateEffectProperty = stateEffectProperty;
            BaseValue           = baseValue;
            LevelScalingFactor  = levelScalingFactor;
            StackScalingFactor  = stackScalingFactor;
            SpecialConditions   = specialConditions ?? new();
        }

        public float Get(int level, int stacks, string specialCondition = "")
        {
            if (! HasSpecialCondition(specialCondition))
            {
                return 0f;
            }

            return BaseValue * Mathf.Pow(1 + LevelScalingFactor, level - 1) * (StackScalingFactor == 0 ? 1 : stacks * StackScalingFactor);
        }

        public bool HasSpecialCondition(string specialCondition, bool mustContains = false)
        {
            // UNIQUE - can only apply IF has the special condition allowed
            if (IsUnique(specialCondition, out string formatedString))
                return HasSpecialCondition(formatedString, mustContains);

            // No special condition, depends if the value must be contained or not
            if (SpecialConditions.IsNullOrEmpty())
                return !mustContains;

            // check if special case of "specialCondition" ("ultimate", "auto attack", ...)
            if (CheckConditionSpecialCases(specialCondition))
            {
                return true;
            }

            return SpecialConditions.Contains(specialCondition);
        }

        public bool CheckConditionSpecialCases(string specialCondition)
        {
            // TODO : handle special cases
            return false;
        }

        /// <summary>
        /// Set the condition as unique, meaning that the special condition wont trigger if not specificaly in the SpecialConditions
        /// </summary>
        /// <param name="specialCondition"></param>
        /// <returns></returns>
        public static string AsUnique(string specialCondition)
        {
            return specialCondition.StartsWith("==") ? specialCondition : "==" + specialCondition;
        }

        /// <summary>
        /// Check (and clean) if special condition has specific tag
        /// </summary>
        /// <param name="specialCondition"></param>
        /// <param name="formatedName"></param>
        /// <returns></returns>
        public static bool IsUnique(string specialCondition, out string formatedName)
        {
            if (specialCondition.StartsWith("=="))
            {
                formatedName = specialCondition.Replace("==", "");
                return true;
            }

            formatedName = specialCondition;
            return false;
        }
    }
}