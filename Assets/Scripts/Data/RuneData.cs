using Assets.Scripts.Data.PowerUp;
using Data.DataStructures;
using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class SRunePower
    {
        protected string            m_RuneName;
        protected ERuneActivation   m_RuneActivation;

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

        public string Name => m_RuneName + " - " + m_RuneActivation.ToString();
        public string                       RuneName        => m_RuneName;
        public ERuneActivation              RuneActivation  => m_RuneActivation;
        public int                          Level           => m_Level;
        public int                          BonusLevel      => m_BonusLevel;
        public List<STriggerEffect>         TriggerEffects  => m_TriggerEffects;
        public List<SCharacterStatScaling>  BonusStats      => m_BonusStats;


        #region Config Mananagement

        public void SetName(string runeName, ERuneActivation runeActivation)
        {
            m_RuneName = runeName;
            m_RuneActivation = runeActivation;
        }

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

        public static bool TrySplitPowerUpName(string baseName, out string runeName, out ERuneActivation runeActivation, bool throwError = true)
        {
            runeName = "";
            runeActivation = ERuneActivation.None;

            var split = baseName.Split("-");
            if (split.Length != 2)
            {
                if (throwError)
                    ErrorHandler.Error("Unable to format " + baseName + " as runeName - ERuneActivation");
                return false;
            }

            runeName = split[0].Trim();

            if (! SpellLoader.RunesData.ContainsKey(runeName))
            {
                if (throwError)
                    ErrorHandler.Error("Unable to find rune named " + runeName + " for rune power named " + baseName);
                return false;
            }

            if (! Enum.TryParse(split[1].Trim(), out runeActivation))
            {
                if (throwError)
                    ErrorHandler.Error("Unable to parse " + split[1] + " as ERuneActivation for rune power named " + baseName);
                return false;
            }

            if (! SpellLoader.RunesData[runeName].HasActivationPower(runeActivation))
            {
                if (throwError)
                    ErrorHandler.Error("No " + runeActivation + " data was found for Rune " + runeName);
                return false;
            }

            return true;
        }

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

        public SRunePower GetRunePower(ERuneActivation runeActivation)
        {
            SRunePower runePower;

            switch (runeActivation) 
            {
                case ERuneActivation.Minor:
                    runePower = m_MinorPower;
                    break;

                case ERuneActivation.Major:
                    runePower = m_MajorPower;
                    break;

                case ERuneActivation.Primal:
                    runePower = m_PrimalPower;
                    break;

                default:
                    ErrorHandler.Error("Unhandled RunePower : " + runeActivation);
                    return default;
            }

            runePower.SetName(Name, runeActivation);
            return runePower;
        }

        public bool HasActivationPower(ERuneActivation runeActivation)
        {
            return GetRunePower(runeActivation) != default;
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

        public override void SetLevel(int level)
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


        #region Power Up conversion


        #endregion
    }
}