using Enums;
using Game.Spells;
using Menu.MainMenu;
using Save;
using System;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.Unicode;

namespace Menu.Common.Buttons
{
    public class TemplateRuneItemUI : TemplateCollectableItemUI
    {
        #region Members

        // ========================================================================================
        // GameObeject & Component
        Image m_ReplacementImage;

        // ========================================================================================
        // Button Data
        protected ERune m_Rune => (ERune)m_Collectable;
        protected int m_Index = -1;

        public ERune Rune => m_Rune;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ReplacementImage = Finder.FindComponent<Image>(gameObject, "ReplacementImage", false);
        }

        public void SetIndex(int index)
        {
            m_Index = index;
        }

        #endregion


        #region GUI Manipulators

        protected override void RefreshUI()
        {
            base.RefreshUI();
            m_BottomOverlay.gameObject.SetActive(m_Rune != ERune.None);
            CheckReplacementImage();
        }

        /// <summary>
        /// Setup icon of the spell and color of its border
        /// </summary>
        public virtual void RefreshRune(ERune rune)
        {
            base.Initialize(rune);
        }

        protected override void ToggleSubButtons()
        {
            // dont Toggle empty rune
            if (m_Rune == ERune.None)
                return;

            base.ToggleSubButtons();
        }

        void CheckReplacementImage()
        {
            if (m_ReplacementImage == null)
                return;

            m_ReplacementImage.gameObject.SetActive(m_Rune == ERune.None);
            m_Content.gameObject.SetActive(m_Rune != ERune.None);
        }

        #endregion


        #region Listeners

        /// <summary>
        /// Action happening when the button is clicked on - depending on button context
        /// </summary>
        protected override void OnClick()
        {
            if (m_AsIconOnly)
                return;

            // behavior when the USE button was clicked and this is in the current build
            if (CurrentBuildDisplayUI.CurrentSelectedItem != null
                && CurrentBuildDisplayUI.CurrentSelectedItem.GetType() == m_Collectable.GetType()
                && CharacterBuildsCloudData.IsInCurrentBuild(m_Collectable))
            {
                // replace this item with the "CurrentSelectedItem"
                CharacterBuildsCloudData.SetInCurrentBuild(CurrentBuildDisplayUI.CurrentSelectedItem, m_Index);
                RefreshRune((ERune)CurrentBuildDisplayUI.CurrentSelectedItem);
                return;
            }

            base.OnClick();
        }

        #endregion
    }
}