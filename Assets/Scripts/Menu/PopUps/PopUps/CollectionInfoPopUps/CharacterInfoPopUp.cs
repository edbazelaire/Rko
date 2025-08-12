using Data;
using Enums;
using Google.Apis.Util;
using Menu.Common.Infos;
using Save;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class CharacterInfoPopUp : CollectableInfoPopUp
    {
        #region Members

        // =========================================================================================
        // GameObjects & Components
        protected TMP_Text              m_DescriptionText;
        protected StateEffectsInfoRow   m_StateEffectsInfoRow;
        protected GameObject            m_SpellsContent;
        protected AbilityInfoRowUI      m_TemplateAbilityInfoRowUI;

        // =========================================================================================
        // Dependent Members
        protected CharacterData m_CharacterData => m_Data as CharacterData;
        protected override bool m_CanUpgrade => base.m_CanUpgrade && m_Data.Level < ProfileCloudData.AccountLevel;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_DescriptionText           = Finder.FindComponent<TMP_Text>(gameObject, "Description");
            m_StateEffectsInfoRow       = Finder.FindComponent<StateEffectsInfoRow>(gameObject);
            m_SpellsContent             = Finder.Find(gameObject, "SpellsContent");
            m_TemplateAbilityInfoRowUI  = AssetLoader.Load<AbilityInfoRowUI>("AbilityInfoRow", AssetLoader.c_MainUIComponentsInfosPath);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();
            SetUpAbilities();
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

        protected override void SetUpDescription()
        {
            m_DescriptionText.text = m_CharacterData.GetDescription();
        }

        protected virtual List<string> GetAbilities()
        {
            var abilities = new List<string>(); 
            if (m_CharacterData.AutoAttack != ESpell.None)
                abilities.Add(m_CharacterData.AutoAttack.ToString());
            if (m_CharacterData.SpecialAbility != ESpell.None)
                abilities.Add(m_CharacterData.SpecialAbility.ToString());
            if (m_CharacterData.Ultimate != ESpell.None)
                abilities.Add(m_CharacterData.Ultimate.ToString());

            return abilities;
        }

        void SetUpAbilities()
        {
            List<string> abilities = GetAbilities();

            UIHelper.CleanContent(m_SpellsContent);
            foreach (string ability in abilities)
            {
                AbilityInfoRowUI abilityInfoRow = Instantiate(m_TemplateAbilityInfoRowUI, m_SpellsContent.transform);
                abilityInfoRow.Initialize(ability, m_Level);
            }
        }

        #endregion
    }
}