using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class RuneSelectionPopUp : PopUp
    {
        #region Members

        // =========================================================================================
        // Data
        ERune                               m_CurrentRune;
        GameObject                          m_TemplateRuneButton;

        // =========================================================================================
        // GameObjects & Components
        GameObject                          m_RunesContent;
        TemplateRuneItemUI                  m_CurrentRuneItem;
        TMP_Text                            m_CurrentRuneTitle;
        TMP_Text                            m_CurrentRuneDescription;
        Button                              m_UpgradeButton;
        TMP_Text                            m_CostText;

        // =========================================================================================
        // Dependent Members
        bool m_CanUpgrade => true;
        bool m_IsMaxedLevel => false;


        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // -- scroller
            var rightSection = Finder.Find(m_WindowContent, "RightSection");
            m_RunesContent = Finder.Find(rightSection, "Content");
            m_TemplateRuneButton = AssetLoader.LoadTemplateItem(CharacterBuildsCloudData.CurrentRune);

            // -- left section
            var leftSection = Finder.Find(m_WindowContent, "LeftSection");
            m_CurrentRuneItem = Finder.FindComponent<TemplateRuneItemUI>(leftSection, "CurrentRune");
            m_CurrentRuneTitle = Finder.FindComponent<TMP_Text>(leftSection, "RuneTitle");
            m_CurrentRuneDescription = Finder.FindComponent<TMP_Text>(leftSection, "Description");
            m_CurrentRuneItem.Initialize(CharacterBuildsCloudData.CurrentRune);

            // -- buttons
            m_UpgradeButton = Finder.FindComponent<Button>(m_Buttons, "UpgradeSubButton");
            m_CostText = Finder.FindComponent<TMP_Text>(m_UpgradeButton.gameObject, "CostText");
        }

        /// <summary>
        /// Called when the prefab is loaded : register all components & game objects, then initilaize UI
        /// </summary>
        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            // set ui of current rune
            m_CurrentRuneItem.Initialize(CharacterBuildsCloudData.CurrentRune, asIconOnly: true);
            RefreshCurrentRuneSelection(CharacterBuildsCloudData.CurrentRune);

            // initialize scroller of Runes
            SetUpRuneScroller();

            // setup listeners
            TemplateRuneItemUI.ButtonClickedEvent += RefreshCurrentRuneSelection;
        }

        /// <summary>
        /// Exiting the PopUp
        /// </summary>
        protected override void Exit()
        {
            CharacterBuildsCloudData.SetCurrentRune(m_CurrentRune);

            base.Exit();

            TemplateRuneItemUI.ButtonClickedEvent -= RefreshCurrentRuneSelection;
        }

        #endregion


        #region UIManipulators

        /// <summary>
        /// Display selected rune to the current selection
        /// </summary>
        /// <param name="rune"></param>
        void RefreshCurrentRuneSelection(Enum collectable)
        {
            ERune rune = (ERune)collectable;

            var runeData = SpellLoader.GetRuneData(rune);
            if (runeData == default)
                return;

            m_CurrentRune = rune;
            m_CurrentRuneItem.RefreshRune(rune);
            m_CurrentRuneTitle.text         = TextLocalizer.SplitCamelCase(rune.ToString());
            m_CurrentRuneDescription.text   = runeData.GetDescription();

            RefreshUpgradeButtonUI();
        }

        /// <summary>
        /// Initilaize scroller of runes
        /// </summary>
        void SetUpRuneScroller()
        {
            UIHelper.CleanContent(m_RunesContent);
            foreach (ERune rune in Enum.GetValues(typeof(ERune)))
            {
                var runeItem = Instantiate(m_TemplateRuneButton, m_RunesContent.transform).GetComponent<TemplateRuneItemUI>();
                runeItem.Initialize(rune);
            }
        }

        /// <summary>
        /// Refresh UI to display if the UpgradeButton can be use or not
        /// </summary>
        void RefreshUpgradeButtonUI()
        {
            // TODO : handle upgrade of rune button
            int cost = 0;
            if (m_IsMaxedLevel || cost <= 0)
            {
                m_UpgradeButton.gameObject.SetActive(false);
                return;
            }

            var buttonImage = Finder.FindComponent<Image>(m_UpgradeButton.gameObject);

            m_UpgradeButton.interactable = m_CanUpgrade;
            buttonImage.color = m_CanUpgrade ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            m_CostText.text = cost.ToString();
        }

        #endregion


        #region Listeners

        protected override void OnUIButton(string bname)
        {
            switch (bname)
            {
                case "UpgradeSubButton":
                    OnUpgrade();
                    return;

                case "UseSubButton":
                    OnUse();
                    return;

                default:
                    base.OnUIButton(bname);
                    return;
            }
        }

        void OnUpgrade()
        {
            Debug.Log("UPGRADE");
        }

        void OnUse()
        {
            Exit();
        }

        #endregion
    }
}