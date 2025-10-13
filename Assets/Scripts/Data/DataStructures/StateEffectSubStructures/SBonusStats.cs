using Data.DataStructures.SpellSubStructures;
using Enums;
using Game.Loaders;
using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.VisualScripting;
using UnityEngine;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public class SBonusStatsOverride
    {
        public EOverridingType  OverridingType;
        public SBonusStats      BonusStat;
    }

    [Serializable]
    public class SBonusStats
    {
        public EStateEffectProperty     StateEffectProperty;
        public float                    BaseValue;
        public float                    LevelScalingFactor;
        public float                    StackScalingFactor;
        public List<EDamageCategory>    DamageCategories;
        public List<EHitCategory>       HitCategories;
        public List<string>             SpecialConditions;

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

        public SBonusStats(EStateEffectProperty stateEffectProperty, float baseValue = 0f, float levelScalingFactor = 0.1f, float stackScalingFactor = 0.1f, List<EDamageCategory> damageCategories = default, List<EHitCategory> hitCategories = default, List<string> specialConditions = default)
        {
            StateEffectProperty = stateEffectProperty;
            BaseValue           = baseValue;
            LevelScalingFactor  = levelScalingFactor;
            StackScalingFactor  = stackScalingFactor;
            DamageCategories    = damageCategories  ?? new();
            HitCategories       = hitCategories     ?? new();
            SpecialConditions   = specialConditions ?? new();
        }

        public float Get(int level, int stacks, EDamageCategory? damageCategory = null, EHitCategory? hitCategory = null, string specialCondition = "")
        {
            if (!CheckConditions(damageCategory, hitCategory, specialCondition))
            {
                return 0f;
            }

            return BaseValue * Mathf.Pow(1 + LevelScalingFactor, level - 1) * (StackScalingFactor == 0 ? 1 : stacks * StackScalingFactor);
        }

        public bool CheckConditions(EDamageCategory? damageCategory, EHitCategory? hitCategory, string specialCondition)
        {
            return HasDamageCategory(damageCategory) && HasHitCategory(hitCategory) && HasSpecialCondition(specialCondition);
        }

        public bool HasDamageCategory(EDamageCategory? damageCategory)
        {
            // This effect has no specific damage category - return true
            if (DamageCategories.IsNullOrEmpty())
                return true;

            // No damage category for the requested value - return true
            if (damageCategory == null)
                return true;

            return DamageCategories.Contains(damageCategory.Value);
        }

        public bool HasHitCategory(EHitCategory? hitCategory)
        {
            // This effect has no specific damage category - return true
            if (HitCategories.IsNullOrEmpty())
                return true;

            // No damage category for the requested value - return true
            if (hitCategory == null)
                return true;

            return HitCategories.Contains(hitCategory.Value);
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
            if (!specialCondition.IsNullOrEmpty() && specialCondition.StartsWith("=="))
            {
                formatedName = specialCondition.Replace("==", "");
                return true;
            }

            formatedName = specialCondition;
            return false;
        }

        #region Info & Description

        public string GetPrettyName()
        {
            string prettyName = "";
            if (!DamageCategories.IsNullOrEmpty() && DamageCategories.Count == 1)
                prettyName += DamageCategories[0].ToString();

            prettyName += " " + TextHandler.Split(StateEffectProperty.ToString());
            if (!HitCategories.IsNullOrEmpty())
                prettyName += " (" + string.Join(", ", HitCategories) + ")";

            if (! SpecialConditions.IsNullOrEmpty())
            {
                prettyName += " to";
                foreach (string specialCondition in SpecialConditions)
                {
                    string specialConditionString = specialCondition;

                    if (specialConditionString.StartsWith("=="))
                        specialConditionString.Remove(2);

                    if (SpellLoader.IsStateEffect(specialConditionString))
                        specialConditionString = TextHandler.ReplaceStateEffectTokens("["+ specialConditionString + "]");

                    prettyName += " " + specialConditionString;
                }
            }

            return prettyName;
        }

        #endregion
    }
}