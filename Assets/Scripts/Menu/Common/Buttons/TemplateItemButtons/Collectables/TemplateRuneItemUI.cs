using Assets.Scripts.Managers;
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
        Image m_RuneActivationIcon;

        // ========================================================================================
        // Button Data
        protected ERune m_Rune => (ERune)m_Collectable;
        protected int m_Index = -1;
        protected ERuneActivation m_RuneActivation;

        public ERune Rune => m_Rune;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ReplacementImage = Finder.FindComponent<Image>(gameObject, "ReplacementImage", false);
            m_RuneActivationIcon = Finder.FindComponent<Image>(gameObject, "RuneActivationIcon", false);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_RuneActivationIcon.gameObject.SetActive(false);
        }

        public void SetIndex(int index)
        {
            m_Index = index;
        }

        public void SetRuneActivation(ERuneActivation activation)
        {
            m_RuneActivation = activation;
            m_RuneActivationIcon.gameObject.SetActive(activation != ERuneActivation.None);

            if (m_RuneActivation == ERuneActivation.None)
                return;

            m_RuneActivationIcon.sprite = AssetLoader.Load<Sprite>(activation.ToString()+"Rune", AssetLoader.c_UISpritesPath); 
        }

        #endregion


        #region GUI Manipulators

        protected override void RefreshUI()
        {
            base.RefreshUI();
            if (m_BottomOverlay != null)
                m_BottomOverlay.gameObject.SetActive(m_Rune != ERune.None);
            CheckReplacementImage();
        }

        /// <summary>
        /// Setup icon of the spell and color of its border
        /// </summary>
        public virtual void RefreshRune(ERune rune)
        {
            UnRegisterListeners();
            base.Initialize(rune);
        }

        public override void ToggleSubButtons()
        {
            // dont Toggle empty rune
            if (m_Rune == ERune.None)
                return;

            if (m_CollectionFillBar != null && ! m_AsIconOnly)
                m_CollectionFillBar.gameObject.SetActive(! m_CollectionFillBar.gameObject.activeSelf);

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

        /// <summary>
        /// Do nothing on clicked while beeing locked
        /// </summary>
        protected override void OnClickLocked()
        {
            base.OnClickLocked();

            ScreenManager.SetPopUp(EPopUpState.RuneInfoPopUp, Collectable, 0);
        }

        #endregion
    }
}