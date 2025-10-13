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
        protected List<string> m_SpecialConditions;

        public EStateEffectProperty Property => m_Property;
        public List<string> SpecialConditions => m_SpecialConditions;

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
                value += GetProperty(m_Property, spellHitSummary, specialValues);
            }
            else
            {
                foreach (string specialCondition in m_SpecialConditions)
                {
                    value += GetPropertySpecialCondition(m_Property, specialCondition, spellHitTypeDatas);
                }
            }

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
        int GetProperty(EStateEffectProperty property, SSpellHitTypeData spellHitSummary, Dictionary<ESpecialValue, float> specialValues)
        {
            switch (property)
            {
                // DAMAGE -------------------------------------------------
                case EStateEffectProperty.Damage:
                    return spellHitSummary.GetTotal(EHitType.Damage);

                case EStateEffectProperty.TickDamage:
                    return spellHitSummary.GetCategoryValue(EHitType.Damage, EHitCategory.Dot);

                case EStateEffectProperty.ExecutionDamage:
                    return spellHitSummary.GetCategoryValue(EHitType.Damage, EHitCategory.Execution);

                // HEALING -------------------------------------------------
                case EStateEffectProperty.Heal:
                    return spellHitSummary.GetTotal(EHitType.Heal);

                case EStateEffectProperty.TickHeal:
                    return spellHitSummary.GetCategoryValue(EHitType.Heal, EHitCategory.Dot);

                // SHIELD -------------------------------------------------
                case EStateEffectProperty.Shield:
                    return spellHitSummary.GetTotal(EHitType.Shield);

                case EStateEffectProperty.TickShield:
                    return spellHitSummary.GetCategoryValue(EHitType.Shield, EHitCategory.Dot);

                // -----------------------------------------------------------
                // SPECIAL VALUES
                // -----------------------------------------------------------
                case EStateEffectProperty.ResistanceFix:
                    if (specialValues.IsNullOrEmpty() || ! specialValues.ContainsKey(ESpecialValue.DamageReduction))
                        return 0;
                    return (int)Math.Round(specialValues[ESpecialValue.DamageReduction]);

                default:
                    ErrorHandler.Warning("Unhandled case : " + m_Property);
                    return 0;
            }
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

            return GetProperty(property, spellHitData[0], null);
        }

        #endregion
    }
}