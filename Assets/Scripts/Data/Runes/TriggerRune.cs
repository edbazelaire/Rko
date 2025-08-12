using Data.DataStructures;
using Enums;
using Game.Loaders;
using Save;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "TriggerRune", menuName = "Game/Runes/TriggerRune")]
    public class TriggerRune : RuneData
    {
        #region Members

        [SerializeField]
        protected List<STriggerEffect> m_TriggerEffects;

        public List<STriggerEffect> TriggerEffects => m_TriggerEffects;

        #endregion


        #region Level

        public override void SetLevel(int level)
        {
            base.SetLevel(level);

            if (m_TriggerEffects == null)
                return;

            // upgrade level of all trigger effects
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                STriggerEffect effect = m_TriggerEffects[i];
                effect.Level = m_Level;
                m_TriggerEffects[i] = effect;
            }
        }

        #endregion


        #region Infos

        public override string GetDescription(ERuneActivation runeActivation)
        {
            var description = base.GetDescription(runeActivation);

            if (description == "")
                description = "[TriggerEffect.0]";

            return TextHandler.ReplaceTriggerEffectTokens(description, m_TriggerEffects);
        }

        #endregion
    }
}