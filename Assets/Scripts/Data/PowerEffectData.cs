using Data.DataStructures;
using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.PowerEffects;
using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    public class PowerEffectData<T> : CollectableData where T : SPowerEffect
    {
        #region Members

        [SerializeField, Tooltip("Default power of the Rune")]
        public T        m_MinorPower;
        [SerializeField, Tooltip("Secondary power of the Rune")]
        public T        m_MajorPower;
        [SerializeField, Tooltip("Primal power of the Rune")]
        public T        m_PrimalPower;

        /// <summary> current activation of the rune </summary>
        protected ERuneActivation m_RuneActivation = ERuneActivation.Primal;

        #endregion


        #region Rune Power Activation

        public void SetActivation(ERuneActivation runePower)
        {
            m_RuneActivation = runePower;
        }

        #endregion


        #region Effects

        public T GetRunePower(ERuneActivation runeActivation)
        {
            T powerEffect;

            switch (runeActivation) 
            {
                case ERuneActivation.Minor:
                    powerEffect = m_MinorPower;
                    break;

                case ERuneActivation.Major:
                    powerEffect = m_MajorPower;
                    break;

                case ERuneActivation.Primal:
                    powerEffect = m_PrimalPower;
                    break;

                default:
                    ErrorHandler.Error("Unhandled RunePower : " + runeActivation);
                    return default;
            }

            powerEffect.SetName(Name, runeActivation);
            return powerEffect;
        }

        public bool HasActivationPower(ERuneActivation runeActivation)
        {
            var runePower = GetRunePower(runeActivation);
            return ! runePower.BonusStats.IsNullOrEmpty() || ! runePower.TriggerEffects.IsNullOrEmpty();
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
            var triggerEffects = GetRunePower(m_RuneActivation).TriggerEffects;

            // set parent (for tracability)
            for (int i = 0; i < triggerEffects.Count; i++)
            {
                var triggerEffect = triggerEffects[i];
                triggerEffect.SetParent(Name);
                triggerEffects[i] = triggerEffect;
            }

            return triggerEffects;
        }

        #endregion


        #region Info

        public virtual string GetDescription(ERuneActivation runeActivation = ERuneActivation.None)
        {
            string description = m_Description;

            if (runeActivation == ERuneActivation.None)
                runeActivation = m_RuneActivation;

            if (runeActivation == ERuneActivation.None)
                return description;

            if (description != "")
                description += "\n\n    ";

            return description + GetRunePower(runeActivation).GetDescription();
        }

        public List<CharacterData> GetSpawnsData(ERuneActivation runeActivation)
        {
            return GetRunePower(runeActivation).GetSpawnsData();
        }

        public List<ERuneActivation> GetNotAllowedActivations()
        {
            var notAllowedActivations = new List<ERuneActivation>();
            foreach (ERuneActivation runeActivation in Enum.GetValues(typeof(ERuneActivation)))
            {
                if (runeActivation == ERuneActivation.None)
                    continue;

                if (! HasActivationPower(runeActivation))
                    notAllowedActivations.Add(runeActivation);
            }

            return notAllowedActivations;
        }

        #endregion


        #region Level

        public new PowerEffectData<T> Clone(int level = 0, bool destroy = false)
        {
            return (PowerEffectData<T>)base.Clone(level, destroy);
        }

        public override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_MinorPower.SetLevel(level);
            m_MajorPower.SetLevel(level);
            m_PrimalPower.SetLevel(level);
        }

        #endregion
    }
}