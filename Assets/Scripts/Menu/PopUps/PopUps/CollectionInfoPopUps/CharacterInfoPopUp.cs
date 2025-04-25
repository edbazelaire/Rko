using Data;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common.Infos;
using Save;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;

namespace Menu.PopUps
{
    public class CharacterInfoPopUp : CollectableInfoPopUp
    {
        #region Members

        // =========================================================================================
        // GameObjects & Components
        TMP_Text m_DescriptionText;
        StateEffectsInfoRow m_StateEffectsInfoRow;

        // =========================================================================================
        // Dependent Members
        CharacterData m_CharacterData => m_Data as CharacterData;
        protected override bool m_CanUpgrade => base.m_CanUpgrade && m_Data.Level < ProfileCloudData.AccountLevel;


        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
            m_DescriptionText       = Finder.FindComponent<TMP_Text>(gameObject, "Description");
            m_StateEffectsInfoRow   = Finder.FindComponent<StateEffectsInfoRow>(gameObject);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();
            SetUpDescription();
            SetUpSpecialEffects();
        }

        #endregion



        #region UIManipulators

        void SetUpSpecialEffects()
        {
            List<SStateEffectData> stateEffects = new List<SStateEffectData>();
            foreach (var runePower in m_CharacterData.SpecialPowers)
            {
                foreach (var triggerEffect in runePower.TriggerEffects) 
                {
                    if (! Enum.TryParse(triggerEffect.SpellDataName, out EStateEffect stateEffect))
                    {
                        ErrorHandler.Error("Unhandled case : " + triggerEffect.SpellDataName + " is not a state effect");
                        continue;
                    }

                    SStateEffectData stateEffectData = new SStateEffectData(stateEffect);
                    stateEffects.Add(stateEffectData);
                }
            }

            m_StateEffectsInfoRow.Initialize(stateEffects, m_Level);
        }

        void SetUpDescription()
        {
            m_DescriptionText.text = m_CharacterData.GetDescription();
        }

        #endregion
    }
}