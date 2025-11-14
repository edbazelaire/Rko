using Assets;
using Assets.Scripts.Game.Loaders.Filters;
using Assets.Scripts.Managers;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Google.Apis.Util;
using Inventory;
using Menu.Common.Buttons;
using Menu.Common.Infos;
using Save;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

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
        protected GameObject            m_MasteryContent;
        protected AbilityInfoRowUI      m_TemplateAbilityInfoRowUI;
        protected Button                m_MasteryUpgradeButton;
        protected TMP_Text              m_MasteryUpgradeCostText;

        // =========================================================================================
        // Local Variables
        protected SPriceData            m_UpgradeMasteryPrice;

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

            // -- mastery content
            m_MasteryContent            = Finder.Find(gameObject, "MasteryContent", false);
            m_MasteryUpgradeButton      = Finder.FindComponent<Button>(gameObject, "MasteryUpgradeButton", false);
            if (m_MasteryUpgradeButton != null)
                m_MasteryUpgradeCostText = Finder.FindComponent<TMP_Text>(m_MasteryUpgradeButton.gameObject, "CostText", false);

            m_TemplateAbilityInfoRowUI = AssetLoader.Load<AbilityInfoRowUI>("AbilityInfoRow", AssetLoader.c_MainUIComponentsInfosPath);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();
            SetUpAbilities();
            SetUpSpecialEffects();
            RefreshMasteryDisplay();
        }

        #endregion


        #region GUI Manipulators

        protected override void RefreshUI()
        {
            base.RefreshUI();

            RefreshMasteryDisplay();
        }

        void SetUpSpecialEffects()
        {
            List<SStateEffectData> stateEffects = new List<SStateEffectData>();
            foreach (var runePower in m_CharacterData.SpecialPowers)
            {
                foreach (var triggerEffect in runePower.TriggerEffects)
                {
                    if (!Enum.TryParse(triggerEffect.SpellDataName, out EStateEffect stateEffect))
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

        protected override void RefreshButtons()
        {
            base.RefreshButtons();

            RefreshMasteryUpgradeButton();
        }

        protected override bool HandleSpecialCases(GameObject container, string key, object value, object newDataValue = null, EScalingDirection scaling = EScalingDirection.None)
        {
            // skip bonus hp (contained in Character's health)
            if (key == EStateEffectProperty.Hp.ToString())
                return true;

            return base.HandleSpecialCases(container, key, value, newDataValue, scaling);
        }

        #endregion


        #region Abilities

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


        #region Mastery Management

        protected void RefreshMasteryDisplay()
        {
            if (m_InfoOnly || !m_IsUnlocked || !CharacterLoader.IsCharacter(m_CharacterData.Character.ToString()))
            {
                DeactivateMastery();
                return;
            }

            //if (!AchievementLoader.CharacterAchievements.ContainsKey(m_CharacterData.Character))
            if (AchievementLoader.Achievements.FilterByCharacter(m_CharacterData.Character).Count == 0)
            {
                ErrorHandler.Warning("No achievements were found for " + m_CharacterData.Character);
                DeactivateMastery();
                return;
            }

            if (m_MasteryContent == null)
            {
                ErrorHandler.Error("Unable to find mastery content");
                DeactivateMastery();
                return;
            }

            SetUpAchievements();
        }

        void SetUpAchievements()
        {
            UIHelper.CleanContent(m_MasteryContent);

            var template = AssetLoader.LoadTemplateItem<TemplateCharacterAchievement>();
            var achievements = AchievementLoader.Achievements.FilterByCharacter(m_CharacterData.Character, strict: true);
            foreach (var achievement in achievements)
            {
                var characterAchivement = Instantiate(template, m_MasteryContent.transform);
                characterAchivement.Initialize(achievement);
            }
        }

        void RefreshMasteryUpgradeButton()
        {
            // SAFETY : make sure the button exists
            if (m_MasteryUpgradeButton == null)
            {
                return;
            }

            // CHECK : mastery already maxed
            if (m_IsMaxedMastery)
            {
                m_MasteryUpgradeButton.gameObject.SetActive(false);
                return;
            }

            // SAFETY : make sure the text exists
            if (m_MasteryUpgradeCostText == null)
            {
                ErrorHandler.Error("Unable to find MasteryUpgradeCostText");
                m_MasteryUpgradeButton.gameObject.SetActive(false);
                return;
            }

            // SAFETY : make sure the price is properly configured in data
            if (! CollectablesManagementData.TryGetMasteryUpgradeCost(m_Data.Rarety, m_Mastery, out SPriceData price))
            {
                m_MasteryUpgradeButton.gameObject.SetActive(false);
                return;
            }

            m_UpgradeMasteryPrice = price;
            m_MasteryUpgradeButton.gameObject.SetActive(true);
            m_MasteryUpgradeCostText.text = price.Price.ToString();
        }

        /// <summary>
        /// If there is an error, or the context does not allow "Mastery", deactivate everything related to Mastery
        /// </summary>
        void DeactivateMastery()
        {
            // if there is a mastery content, deactivate it
            if (m_MasteryContent != null)
                m_MasteryContent.SetActive(false);

            // try to find Mastery Tab Button and deactivate it
            var masteryTabButton = Finder.Find(gameObject, "MasteryButton", false);
            if (masteryTabButton != null)
            {
                masteryTabButton.SetActive(false);
            }

            if (m_MasteryUpgradeButton != null)
                m_MasteryUpgradeButton.gameObject.SetActive(false);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_MasteryUpgradeButton != null)
                m_MasteryUpgradeButton.onClick.AddListener(OnMasteryUpgradeButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_MasteryUpgradeButton != null)
                m_MasteryUpgradeButton.onClick.RemoveAllListeners();
        }

        protected override void OnCollectableDataChanged(SCollectableCloudData collectableCloudData)
        {
            base.OnCollectableDataChanged(collectableCloudData);
            if (collectableCloudData.CollectableName != m_CollectableName)
                return;

            RefreshMasteryDisplay();
        }

        void OnMasteryUpgradeButtonClicked()
        {
            ScreenManager.ConfirmUpgradeMastery(m_Collectable, m_Mastery + 1, m_UpgradeMasteryPrice);
        }

        #endregion
    }
}