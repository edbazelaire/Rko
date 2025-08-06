using Data;
using Enums;
using Menu.Common.Infos;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;


namespace Menu.PopUps
{
    public class SpellInfoPopUp : CollectableInfoPopUp
    {
        #region Members

        // =========================================================================================
        // GameObjects & Components
        StateEffectsInfoRow     m_StateEffectsInfoRow;
        TMP_Text                m_DescriptionText;
        ClipTabContent          m_ClipTabContent;

        // =========================================================================================
        // Dependent Members
        SpellData m_SpellData       => m_Data as SpellData;
        bool m_IsLinked             => m_SpellData.Linked;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_StateEffectsInfoRow   = Finder.FindComponent<StateEffectsInfoRow>(gameObject, "StateEffectsInfoRow");
            m_DescriptionText       = Finder.FindComponent<TMP_Text>(gameObject, "Description");
            m_ClipTabContent        = Finder.FindComponent<ClipTabContent>(gameObject);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_BuyButton.gameObject.SetActive(false);

            SetUpSpecialCases();

            SetUpStateEffects();
            SetUpCollectionFillbar();
            m_ClipTabContent.SetUpVideo(m_SpellData.Spell);
        }

        #endregion


        #region UIManipulators

        void SetUpSpecialCases()
        {
            switch (m_SpellData.SpellType) 
            {
                case ESpellType.Counter:
                    SetUpCounterRow();
                    break;
            }
        }

        void SetUpCounterRow()
        {
            if (m_SpellData is not CounterData counterData)
            {
                ErrorHandler.Warning("Trying to SetUpCounterRow() but spell data are not counter data");
                return;
            }

            // check that has "Proc" effect
            if (counterData.CounterType != ECounterType.Proc || counterData.OnCounterProc == null)
                return;

            // add a title to seperate counter data from "proc" data
            var infoTitle = Finder.FindComponent<TMP_Text>(Instantiate(m_TemplateInfoTitleSection, m_InfosSection.transform));
            infoTitle.text = "Proc Effect";

            // create a new info content
            var newInfoContent = Instantiate(m_InfosContent, m_InfosSection.transform);
            // setup info rows for the sub-spell
            SetUpInfos(newInfoContent, counterData.OnCounterProc, new List<string>() { "CastDuration", "Cooldown" });
        }

        protected override void RefreshButtons()
        {
            base.RefreshButtons();
        }

        void SetUpCollectionFillbar()
        {
            if (m_IsLinked)
                m_CollectableItemUI.CollectionFillBar.gameObject.SetActive(false);
        }

        protected override void SetUpRarety()
        {
            if (m_RaretyContainer == null)
                return;

            // hide rarety for linked spells
            if (m_SpellData.Linked)
            {
                m_RaretyContainer.gameObject.SetActive(false);
                return;
            }

            base.SetUpRarety();
        }

        protected override void SetUpDescription()
        {
            m_DescriptionText.text = m_SpellData.GetDescription();
        }

        /// <summary>
        /// Display infos of the spell
        /// </summary>
        protected override void SetUpInfoRow(GameObject container, string key, object value, object newDataValue = null, EScalingDirection scalingDirection = EScalingDirection.None)
        {
            if (key == "Effects")
                return;

            if (key == "MaxIndexThreshold")
                return;
            
            base.SetUpInfoRow(container, key, value, newDataValue, scalingDirection);
        }

        void SetUpStateEffects()
        {
            var spellInfo = m_Data.GetInfo();
            List<SStateEffectData> effectsData = spellInfo.ContainsKey("Effects") ? spellInfo["Effects"] as List<SStateEffectData> : new List<SStateEffectData>();

            if (effectsData.Count == 0)
            {
                m_StateEffectsInfoRow.gameObject.SetActive(false);
                return;
            }

            m_StateEffectsInfoRow.gameObject.SetActive(true);
            m_StateEffectsInfoRow.Initialize(effectsData, m_Level);
        }

        protected override void RefreshUpgradeButtonUI()
        {
            if (m_IsLinked)
            {
                m_UpgradeButton.gameObject.SetActive(false);
                return;
            }

            base.RefreshUpgradeButtonUI();
        }

        #endregion


        #region Listeners

        protected override void OnLevelUp(Enum collectable, int level)
        {
            base.OnLevelUp(collectable, level);

            // refresh state effects
            SetUpStateEffects();
        }

        #endregion
    }
}