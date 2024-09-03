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