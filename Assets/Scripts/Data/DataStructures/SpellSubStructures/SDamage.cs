using Enums;
using Game.Loaders;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public class SDamage
    {
        [SerializeField, Tooltip("Base damage value")]
        protected float m_BaseValue;

        [SerializeField, Tooltip("Category of Damage")]
        protected EDamageCategory m_DamageCategory = EDamageCategory.Physical;

        [SerializeField, Tooltip("Types of Damage : Execution, Dot, ...")]
        protected EHitCategory m_HitCategory = EHitCategory.Direct;

        [SerializeField]
        protected SSpellPropertyScaling m_SpellPropertyScaling = new SSpellPropertyScaling(ESpellProperty.Damage, 0.1f, EScalingType.Exponential, ERoundingType.Round);

        [SerializeField]
        protected List<SDamageModifier> m_DamageModifiers;

        /// <summary> Bonus value added by external effects - added after the damage level scaling </summary>
        protected float m_BonusValue;

        public float                    BaseValue               => m_BaseValue;
        public EDamageCategory          DamageCategory          => m_DamageCategory;
        public EHitCategory             HitCategory             => m_HitCategory;
        public SSpellPropertyScaling    SpellPropertyScaling    => m_SpellPropertyScaling;
        public List<SDamageModifier>    DamageModifiers         => m_DamageModifiers;
        public EScalingDirection        ScalingDirection        => SpellPropertyScaling.GetScalingDirection();


        #region Init

        public SDamage(float baseValue = 0, EDamageCategory damageCategory = EDamageCategory.Magical, EHitCategory hitCategory = EHitCategory.Direct, SSpellPropertyScaling spellPropertyScaling = default)
        {
            m_BaseValue = baseValue;
            m_DamageCategory = damageCategory;
            m_HitCategory = hitCategory;
            m_SpellPropertyScaling = spellPropertyScaling;
        }

        #endregion


        #region Accessors

        public int Get(int level, Controller caster, Controller targetController)
        {
            float damage = m_SpellPropertyScaling.Get(m_BaseValue, level) + m_BonusValue;
            if (DamageModifiers.IsNullOrEmpty() || caster == null)
                return (int)damage;

            float baseDamage = damage;      // save "base damage" before modifications (to avoid applying modifications on modified damage)
            damage = 0;                     // reset damage to 0 to add all damage modificators
            foreach (var modifier in m_DamageModifiers) 
            {
                damage += modifier.Apply(baseDamage, level, caster, targetController);
            }

            ErrorHandler.Log(() => "Final damage after Damage Modifiers : " + Math.Round(damage), ELogTag.BonusDamage);
            return (int)Math.Round(damage);
        }

        public void OverrideBaseValue(float value)
        {
            m_BaseValue = value;
        }

        public void AddBonusValue(float value)
        {
            m_BonusValue = value;
        }

        public string PropertyName()
        {
            if (m_HitCategory == EHitCategory.Direct || m_HitCategory == EHitCategory.Piercing )
                return $"{m_DamageCategory}Damage";

            return $"{m_HitCategory}Damage";
        }

        public string GetPrettyName()
        {
            string name = $"{m_DamageCategory} Damage";
            if (m_HitCategory != EHitCategory.Direct)
                name += $" ({m_HitCategory})";
            return name;
        }

        #endregion
    }

    [Serializable]
    public class SDamageModifier
    {
        public string                   StateEffectName;
        public bool                     IsConsuming;
        public int                      MaxStacks;
        public EOverridingType          OverridingType;
        public ESpellTarget             Target;
        public SScaling                 Scaling;

        public float Apply(float baseValue, int level, Controller caster, Controller targetController)
        {
            Controller controller = Target == ESpellTarget.Self ? caster : targetController;
            if (controller == null)
            {
                ErrorHandler.Warning("Provided Controller is null : " + Target);
                return baseValue;
            }

            ErrorHandler.Log(() => "Applying damage modifier based on " + StateEffectName, ELogTag.BonusDamage);
            ErrorHandler.Log(() => "      + Base Value : " + baseValue, ELogTag.BonusDamage);

            int nStacks;
            // Check : StateEffect
            if (SpellLoader.IsStateEffect(StateEffectName))
                nStacks = GetStateEffectStacks(controller);

            // Check special cases : Shield, Hp, ...
            else if (! CheckSpecialCases(controller, out nStacks))
            {
                ErrorHandler.Error("Unhandled case : " + StateEffectName);
                return baseValue;
            }
            
            float overrideValue = nStacks * Scaling.Get(level);

            switch (OverridingType)
            {
                case EOverridingType.Replacement:
                    ErrorHandler.Log(() => "      + Replaced with value : " + overrideValue, ELogTag.BonusDamage);
                    return overrideValue;

                case EOverridingType.Additive:
                    ErrorHandler.Log(() => "      + Added with value : " + overrideValue + " -> " + Math.Round(overrideValue + baseValue), ELogTag.BonusDamage);
                    return baseValue + overrideValue;

                case EOverridingType.Multiply:
                    ErrorHandler.Log(() => "      + Multiplied by value : " + (1 + overrideValue) + " -> " + (1 + overrideValue) * baseValue, ELogTag.BonusDamage);
                    return baseValue * (1 + overrideValue);

                default:
                    ErrorHandler.Warning("Unhandled case : " + OverridingType);
                    return baseValue;
            }
        }

        /// <summary>
        /// Get Stacks of the requested StateEffect
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public int GetStateEffectStacks(Controller controller)
        {
            if (IsConsuming)
                return controller.StateHandler.RemoveStateEffect(StateEffectName, true, MaxStacks);
            return Math.Min(controller.StateHandler.GetStacks(StateEffectName), MaxStacks);
        }

        public bool CheckSpecialCases(Controller controller, out int value)
        {
            value = 0;
            if (StateEffectName.ToLower() == "shield")
            {
                value = controller.Life.FinalShield.Value;
                return true;
            }

            if (StateEffectName.ToLower() == "hp" || StateEffectName.ToLower() == "maxhp")
            {
                value = controller.Life.MaxHp.Value;
                return true;
            }

            if (StateEffectName.ToLower() == "currenthp")
            {
                value = controller.Life.Hp.Value;
                return true;
            }

            return false;
        }


        #region Description

        public string GetDescription(int level)
        {
            string overriding = "increased";
            switch(OverridingType)
            {
                case EOverridingType.Replacement:
                    overriding = "replaced";
                    break;
            }

            string target = Target == ESpellTarget.Self ? "your" : "the target's";

            string description;
            if (SpellLoader.IsStateEffect(StateEffectName))
            {
                string perc = OverridingType == EOverridingType.Multiply ? "%" : "";
                description = $"{overriding} by {TextHandler.FormatScaling(Scaling.Get(level).ToString(), Scaling.GetScalingDirection())}{perc} for each of {target} stacks of {TextHandler.Split(StateEffectName)}";
            }
            else
            {
                description = $"{overriding} by {TextHandler.FormatScaling((100 * Scaling.Get(level)).ToString("0"), Scaling.GetScalingDirection())}% of {target} current {TextHandler.Split(StateEffectName)}";
            }

            return description;
        }

        #endregion
    }
}