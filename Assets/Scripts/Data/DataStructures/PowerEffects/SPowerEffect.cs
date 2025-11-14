using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.SpellSubStructures;
using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;


namespace Data.DataStructures.PowerEffects
{
    [Serializable]
    public class SPowerEffect
    {
        #region Members

        // ==========================================================================================
        // Serialized Fields
        [Description("Description informations of the Rune"), TextArea(minLines: 0, maxLines: 5)]
        public string Description;

        [SerializeField, Tooltip("When set : gain the effects of the base effect with a BonusLevel")]
        int m_BonusLevel;

        [SerializeField, Tooltip("List of effects that gets triggered while Rune is active")]
        List<STriggerEffect> m_TriggerEffects;

        [SerializeField]
        List<SCharacterStatScaling> m_BonusStats;

        // ==========================================================================================
        // Local Members
        protected string            m_BaseName;
        protected ERuneActivation   m_RuneActivation;
        protected int               m_Level;

        // ==========================================================================================
        // Dependent Members
        public string                       Name                => m_BaseName + " - " + m_RuneActivation.ToString();
        public string                       BaseName            => m_BaseName;
        public ERuneActivation              RuneActivation      => m_RuneActivation;
        public int                          Level               => m_Level;
        public int                          BonusLevel          => m_BonusLevel;
        public List<STriggerEffect>         TriggerEffects      => m_TriggerEffects;
        public List<SCharacterStatScaling>  BonusStats          => m_BonusStats;

        #endregion


        #region Config Mananagement

        public void SetName(string runeName, ERuneActivation runeActivation)
        {
            m_BaseName = runeName;
            m_RuneActivation = runeActivation;
        }

        public void SetParent(string parent)
        {
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                var triggerEffect = m_TriggerEffects[i];
                triggerEffect.SetParent(parent);
                m_TriggerEffects[i] = triggerEffect;
            }
        }

        public virtual void SetLevel(int level)
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

        /// <summary>
        /// Split the raw name of the effect "Name - RuneActivation" 
        /// into the name of the effect + the Rune Activation
        /// </summary>
        /// <param name="baseName">         Raw name : "Berserker - Major"                              </param>
        /// <param name="powerUpName">      output name of the effect : "Berserker"                     </param>
        /// <param name="runeActivation">   output rune activation of the effect : "Major"              </param>
        /// <param name="throwError">       throw an error if the rune or powerup name is not found ?   </param>
        /// <returns></returns>
        public static bool TrySplitPowerUpName(string baseName, out string powerUpName, out ERuneActivation runeActivation, bool throwError = true)
        {
            powerUpName = "";
            runeActivation = ERuneActivation.None;

            var split = baseName.Split("-");
            if (split.Length != 2)
            {
                if (throwError)
                    ErrorHandler.Error("Unable to format " + baseName + " as runeName - ERuneActivation");
                return false;
            }

            if (!Enum.TryParse(split[1].Trim(), out runeActivation))
            {
                if (throwError)
                    ErrorHandler.Error("Unable to parse " + split[1] + " as ERuneActivation for rune power named " + baseName);
                return false;
            }

            powerUpName = split[0].Trim();

            if (SpellLoader.RunesData.ContainsKey(powerUpName))
            {
                if (!SpellLoader.RunesData[powerUpName].HasActivationPower(runeActivation))
                {
                    if (throwError)
                        ErrorHandler.Error("No " + runeActivation + " data was found for Rune " + powerUpName);
                    return false;
                }
            }

            else if (SpellLoader.PowerUpsData.ContainsKey(powerUpName))
            {
                // TODO : HasActivationPower(runeActivation)
            }

            else
            {
                if (throwError)
                    ErrorHandler.Error("Unable to find " + split[0] + " as RuneData or PowerUpData from : " + baseName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Try to get the value of a property from bonus stats.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="characterStat"></param>
        /// <param name="throwError"></param>
        /// <returns> 
        ///     + true : found
        ///     + false : value not found
        /// </returns>
        bool TryGetCharacterStat(EStateEffectProperty property, out SCharacterStatScaling characterStat, bool throwError = false)
        {
            characterStat = default;
            if (m_BonusStats == null)
                return false;

            foreach (var stat in m_BonusStats)
            {
                if (stat.StateEffectProperty == property)
                {
                    characterStat = stat;
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
        public virtual string GetDescription()
        {
            List<string> values = new List<string>();

            string description = string.Format(Description, values.ToArray());

            if (description == "" && m_TriggerEffects.Count > 0)
                description = "[TriggerEffect.0]";

            description = TextHandler.ReplaceStateEffectTokens(TextHandler.ReplaceTriggerEffectTokens(description, m_TriggerEffects));
            description = TextHandler.ReplaceKeyWords(description);
            return TextHandler.ReplaceCharacterStat(description, m_BonusStats, m_Level - m_BonusLevel);
        }

        /// <summary>
        /// Get all spawns data in trigger effects
        /// </summary>
        /// <returns></returns>
        public List<CharacterData> GetSpawnsData()
        {
            var spawns = new List<CharacterData>();

            foreach (var triggerEffect in m_TriggerEffects)
            {
                if (!SpellLoader.IsSpell(triggerEffect.SpellDataName))
                    continue;

                // get the spell data at the level of the trigger effect
                SpellData spellData = SpellLoader.GetSpellData(triggerEffect.SpellDataName, triggerEffect.Level);
                if (spellData is not SpawnerData spawnerData)
                    continue;

                // add spawns data
                spawns.AddRange(spawnerData.GetSpawnsCharacterData());
            }

            return spawns;
        }

        #endregion
    }

}