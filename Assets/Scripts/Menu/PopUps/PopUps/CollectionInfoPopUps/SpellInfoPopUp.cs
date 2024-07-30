using Data;
using Menu.Common.Infos;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;

namespace Menu.PopUps
{
    public class SpellInfoPopUp : CollectableInfoPopUp
    {
        #region Members

        // =========================================================================================
        // GameObjects & Components
        StateEffectsInfoRow     m_StateEffectsInfoRow;
        TMP_Text                m_DescriptionText;

        // =========================================================================================
        // Dependent Members
        SpellData m_SpellData       => m_Data as SpellData;
        bool m_IsLinked             => m_SpellData.Linked;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_StateEffectsInfoRow = Finder.FindComponent<StateEffectsInfoRow>(gameObject, "StateEffectsInfoRow");
            m_DescriptionText = Finder.FindComponent<TMP_Text>(gameObject, "Description");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            SetUpStateEffects();
            SetupCollectionFillbar();
            SetUpDescription();
        }

        #endregion



        #region UIManipulators

        protected override void SetUpButtons()
        {
            if (m_IsLinked)
            {
                m_UpgradeButton.gameObject.SetActive(false);
                return;
            }

            base.SetUpButtons();
        }

        void SetupCollectionFillbar()
        {
            if (m_IsLinked)
                m_CollectableItemUI.CollectionFillBar.gameObject.SetActive(false);
        }

        void SetUpDescription()
        {
            m_DescriptionText.text = m_SpellData.GetDescription();
        }

        /// <summary>
        /// Display infos of the spell
        /// </summary>
        protected override void SetUpInfoRow(string key, object value, object newDataValue = null)
        {
            if (key == "Effects")
                return;
            
            base.SetUpInfoRow(key, value, newDataValue);
        }

        void SetUpStateEffects()
        {
            var spellData = m_Data.GetInfos();
            List<SStateEffectData> effectsData = spellData.ContainsKey("Effects") ? spellData["Effects"] as List<SStateEffectData> : new List<SStateEffectData>();

            if (effectsData.Count == 0)
            {
                m_StateEffectsInfoRow.gameObject.SetActive(false);
                return;
            }

            m_StateEffectsInfoRow.gameObject.SetActive(true);
            m_StateEffectsInfoRow.Initialize(effectsData, m_Level);
        }

        protected override void OnLevelUp(Enum collectable, int level)
        {
            base.OnLevelUp(collectable, level);

            // refresh state effects
            SetUpStateEffects();

        }

        protected override void RefreshUpgradeButtonUI()
        {
            if (m_IsLinked)
                return;

            base.RefreshUpgradeButtonUI();
        }

        #endregion
    }
}