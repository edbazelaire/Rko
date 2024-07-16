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

        public override string GetDescription()
        {
            if (Description != "")
                return Description;

            if (m_TriggerEffects.Count == 0)
            {
                ErrorHandler.Warning("No Trigger Effect for rune " + name);
                return "";
            }

            if (m_TriggerEffects.Count > 0)
                ErrorHandler.Warning("Multiple automatic description not handled");

            // description of the Rune is the description of the Trigger Effect (at the level of the current character)
            if (SpellLoader.StateEffectExists(m_TriggerEffects[0].SpellDataName))
            {
                return SpellLoader.GetStateEffect(m_TriggerEffects[0].SpellDataName, InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level).GetDescription();
            }

            ErrorHandler.Warning("Unhandled yet, spell have currently no description : TODO");
            return "";
        }

        #endregion
    }
}