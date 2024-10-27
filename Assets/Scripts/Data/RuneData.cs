using Data.DataStructures;
using Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;

namespace Data
{
    [Serializable]
    public struct SRunePower
    {
        [Description("Description informations of the Rune")]
        public string Description;

        [SerializeField]
        List<SDescriptionVariable> m_DescriptionVariables;

        [SerializeField, Tooltip("When set : gain the effects of the previous Rune value with a BonusLevel")]
        int m_BonusLevel;

        [SerializeField, Tooltip("List of effects that gets triggered while Rune is active")]
        List<STriggerEffect> m_TriggerEffects;

        [SerializeField]
        List<SCharacterStatScaling> m_BonusStats;

        int m_Level;

        public readonly int Level => m_Level;
        public readonly int BonusLevel => m_BonusLevel;
        public readonly List<STriggerEffect> TriggerEffects => m_TriggerEffects;
        public readonly List<SCharacterStatScaling> BonusStats => m_BonusStats;


        #region Level Mananagement

        public void SetLevel(int level)
        {
            m_Level = level + m_BonusLevel;

            if (m_TriggerEffects == null)
                m_TriggerEffects = new List<STriggerEffect>();

            // upgrade level of all trigger effects
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                STriggerEffect effect = m_TriggerEffects[i];
                effect.Level = m_Level;
                m_TriggerEffects[i] = effect;
            }
        }

        #endregion


        #region Info

        bool TryGetProperty(EStateEffectProperty property, out float value, bool throwError = false)
        {
            value = 0f;
            if (m_BonusStats == null)
                return false;

            foreach (var stat in m_BonusStats)
            {
                if (stat.StateEffectProperty == property)
                {
                    value = stat.BonusValue + stat.BaseValue * Mathf.Pow(1 + stat.ScalingFactor, m_Level - 1);
                    return true;
                }
            }

            if (throwError)
                ErrorHandler.Error("Unable to find any property " + property + " in " + this);

            return false;
        }

        /// <summary>
        /// Get Description info of the StateEffect
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            List<string> values = new List<string>();

            foreach (SDescriptionVariable descriptionVariable in m_DescriptionVariables)
            {
                if (Enum.TryParse(descriptionVariable.Name, out EStateEffect _))
                {
                    values.Add(TextHandler.FormatStateEffectIcon(descriptionVariable.Name, descriptionVariable.WithIcon));
                }

                else if (Enum.TryParse(descriptionVariable.Name, out EStateEffectProperty property))
                {
                    if (!TryGetProperty(property, out float value))
                    {
                        ErrorHandler.Error("Unable to find property " + property + " in RUNE " + this);
                        values.Add("<b>UNDEFINED</b>");
                        continue;
                    }
                    
                    values.Add($"<b>{TextHandler.FormatPropertyValue(value, descriptionVariable.Name)}</b>");
                }

                else
                {
                    ErrorHandler.Error("Unable to find property " + descriptionVariable.Name + " in info dict of spell " + this);
                    values.Add("<b>UNDEFINED</b>");
                }
            }

            string description = string.Format(Description, values.ToArray());

            if (description == "" && m_TriggerEffects.Count > 0)
                description = "[TriggerEffect.0]";

            return TextHandler.ReplaceStateEffectTokens(TextHandler.ReplaceTriggerEffectTokens(description, m_TriggerEffects));
        }

        #endregion
    }

    [CreateAssetMenu(fileName = "RuneData", menuName = "Game/Runes/Default")]
    public class RuneData : CollectableData
    {
        [Description("Description informations of the Rune")]
        public string Description;

        [Description("List of Element catagories of the spell")]
        [SerializeField] protected List<ESpellElement> m_SpellElements;

        [SerializeField, Tooltip("Default power of the Rune")]
        protected SRunePower m_MinorPower;
        [SerializeField, Tooltip("Secondary power of the Rune")]
        protected SRunePower m_MajorPower;
        [SerializeField, Tooltip("Primal power of the Rune")]
        protected SRunePower m_PrimalPower;

        /// <summary> current activation of the rune </summary>
        protected ERuneActivation m_RuneActivation = ERuneActivation.Primal;

        // ==========================================================================
        // DEPENDENT PROPERTIES
        protected override Type m_EnumType => typeof(ERune);
        public ERune Rune => Enum.TryParse(Name, out ERune rune) ? rune : ERune.None;
        public List<ESpellElement> SpellElements => m_SpellElements;


        #region Rune Power Activation

        public void SetActivation(ERuneActivation runePower)
        {
            m_RuneActivation = runePower;
        }

        #endregion


        #region Effects

        SRunePower GetRunePower(ERuneActivation runeActivation)
        {
            switch (runeActivation) 
            {
                case ERuneActivation.Minor:
                    return m_MinorPower;

                case ERuneActivation.Major:
                    return m_MajorPower;

                case ERuneActivation.Primal:
                    return m_PrimalPower;

                default:
                    ErrorHandler.Error("Unhandled RunePower : " + runeActivation);
                    return default;
            }
        }

        bool IsPowerActive(ERuneActivation runeActivation)
        {
            return runeActivation == m_RuneActivation || runeActivation == ERuneActivation.None;
        }

        public List<SCharacterStatScaling> GetBonusStats()
        {
            List<SCharacterStatScaling> stats = new ();

            foreach (var stat in GetRunePower(m_RuneActivation).BonusStats)
            {
                stat.AsBonus(m_Level);

                // check if already in list
                int index = stats.FindIndex(value => value.StateEffectProperty.Equals(stat.StateEffectProperty));

                // if not in list : add as new bonus value
                if (index < 0)
                {
                    stats.Add(stat);
                    continue;
                }

                // add bonus value to existing bonus value
                var newStat = stats[index];
                newStat.BonusValue += stat.BonusValue;
                stats[index] = newStat;
            }

            return stats;
        }

        public List<STriggerEffect> GetTriggerEffects()
        {
            return GetRunePower(m_RuneActivation).TriggerEffects;
        }

        #endregion


        #region Info

        public virtual string GetDescription(ERuneActivation runeActivation = ERuneActivation.None)
        {
            string description = Description;

            if (runeActivation == ERuneActivation.None)
                runeActivation = m_RuneActivation;

            if (runeActivation == ERuneActivation.None)
                return description;

            if (description != "")
                description += "\n\n    ";

            return description + GetRunePower(runeActivation).GetDescription();
        }

        #endregion


        #region Level

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_MinorPower.SetLevel(level);
            m_MajorPower.SetLevel(level);
            m_PrimalPower.SetLevel(level);
        }

        public new RuneData Clone(int level = 0, bool destroy = false)
        {
            return (RuneData)base.Clone(level, destroy);
        }

        #endregion
    }
}