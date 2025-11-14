using Assets.Scripts.Game;
using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "PropertyAchievementData", menuName = "Game/Achievements/Analytics/Property")]
    public class PropertyAchievementData : DefaultAchievementData
    {
        #region Members

        [SerializeField]
        protected EStateEffectProperty m_Property;
        [SerializeField]
        protected List<EDamageCategory> m_DamageCategories;
        [SerializeField]
        protected List<EHitCategory> m_HitCategories;
        [SerializeField]
        protected List<string> m_SpecialConditions;

        public EStateEffectProperty     Property            => m_Property;
        public List<EDamageCategory>    DamageCategories    => m_DamageCategories;
        public List<EHitCategory>       HitCategories       => m_HitCategories;
        public List<string>             SpecialConditions   => m_SpecialConditions;

        #endregion


        #region Check

        /// <summary>
        /// Use end game analytics data (GameAnalyticsManager) to check end game achievements
        /// </summary>
        /// <param name="spellHitSummary"></param>
        /// <param name="specialValues"></param>
        /// <param name="spellHitTypeDatas"></param>
        public virtual bool Check(SSpellHitTypeData spellHitSummary, Dictionary<ESpecialValue, float> specialValues, List<SSpellHitTypeData> spellHitTypeDatas, bool save = false)
        {
            int value = 0;
            if (m_SpecialConditions == null || m_SpecialConditions.Count == 0)
            {
                value += GetProperty(m_Property, m_DamageCategories, m_HitCategories, spellHitSummary, specialValues);
            }
            else
            {
                foreach (string specialCondition in m_SpecialConditions)
                {
                    value += GetPropertySpecialCondition(m_Property, specialCondition, spellHitTypeDatas);
                }
            }

            // no value to increment - no need to trigger save
            if (value == 0)
                return false;

            if (value < 0)
            {
                ErrorHandler.Warning("Found value (" + value + ") < 0");
                return false;
            }

            Increase(value, save: save);
            return true;
        }

        /// <summary>
        /// Get value of property at the end of the game
        /// </summary>
        /// <param name="property"></param>
        /// <param name="spellHitSummary"></param>
        /// <param name="specialValues"></param>
        /// <returns></returns>
        int GetProperty(EStateEffectProperty property, List<EDamageCategory> damageCategories, List<EHitCategory> hitCategories, SSpellHitTypeData spellHitSummary, Dictionary<ESpecialValue, float> specialValues)
        {
            // -----------------------------------------------------------
            // SPECIAL VALUES
            // -----------------------------------------------------------
            if (property == EStateEffectProperty.Resistance)
            {
                if (specialValues.IsNullOrEmpty() || !specialValues.ContainsKey(ESpecialValue.DamageReduction))
                    return 0;
                return (int)Math.Round(specialValues[ESpecialValue.DamageReduction]);
            }

            // -----------------------------------------------------------
            // PROPERTIES 
            // -----------------------------------------------------------
            if (hitCategories.IsNullOrEmpty())
                hitCategories = Enum.GetValues(typeof(EHitCategory)).Cast<EHitCategory>().ToList();

            int value = 0;
            foreach (EHitCategory hitCategory in hitCategories)
            {
                switch (property)
                {
                    // DAMAGE -------------------------------------------------
                    case EStateEffectProperty.Damage:
                        if (damageCategories.IsNullOrEmpty())
                            damageCategories = Enum.GetValues(typeof(EDamageCategory)).Cast<EDamageCategory>().ToList();
                        foreach (EDamageCategory damageCategory in damageCategories)
                        {
                            var hitType = damageCategory == EDamageCategory.Physical ? EHitType.PhysicalDamage : EHitType.MagicalDamage;
                            value += spellHitSummary.GetCategoryValue(hitType, hitCategory);
                        }
                        break;

                    // HEALING -------------------------------------------------
                    case EStateEffectProperty.Heal:
                        value += spellHitSummary.GetCategoryValue(EHitType.Heal, hitCategory);
                        break;

                    // SHIELD -------------------------------------------------
                    case EStateEffectProperty.Shield:
                        value += spellHitSummary.GetCategoryValue(EHitType.Shield, hitCategory);
                        break;

                    default:
                        ErrorHandler.Warning("Unhandled case : " + m_Property);
                        return 0;
                }
            }

            return value;
        }

        /// <summary>
        /// Get value of a property matching a special condition (spell, state effect, ...)
        /// </summary>
        /// <param name="property"></param>
        /// <param name="specialCondition"></param>
        /// <param name="spellHitTypeDatas"></param>
        /// <returns></returns>
        int GetPropertySpecialCondition(EStateEffectProperty property, string specialCondition, List<SSpellHitTypeData> spellHitTypeDatas)
        {
            var spellHitData = spellHitTypeDatas.Where(data => data.SpellName == specialCondition).ToList();
            if (spellHitData.Count == 0) 
                return 0;

            if (spellHitData.Count > 1) 
            {
                ErrorHandler.Warning("Found multiple spellHitData with SpellName : " + specialCondition);
            }

            return GetProperty(property, m_DamageCategories, m_HitCategories, spellHitData[0], null);
        }

        #endregion
    }
}