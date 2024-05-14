﻿using Enums;
using Menu.Common.Buttons;
using Menu.PopUps.Components.ProfilePopUp;
using Save;
using System.Collections.Generic;
using TMPro;
using Tools;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Menu.MainMenu
{
    public class ProfileDisplayUI : MObject
    {
        #region Members

        // -- avatar section
        AvatarButtonUI      m_AvatarButtonUI;
        // -- player name & title
        GameObject          m_GamerTagSection;
        TMP_InputField      m_GamerTagInput;
        // -- title
        Button              m_PlayerTitleButton;
        TMP_Text            m_PlayerTitle;
        // -- badges
        GameObject          m_BadgesSection;
        List<BadgeButtonUI> m_BadgeButtons;

        // ===============================================================================
        // Public Acessors
        public AvatarButtonUI           AvatarButtonUI      => m_AvatarButtonUI;
        public TMP_InputField           GamerTagInput       => m_GamerTagInput;
        public Button                   PlayerTitleButton   => m_PlayerTitleButton;
        public List<BadgeButtonUI>      BadgeButtons        => m_BadgeButtons;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // -- avatar section
            m_AvatarButtonUI    = Finder.FindComponent<AvatarButtonUI>(gameObject);

            // -- gamer tag
            m_GamerTagSection   = Finder.Find(gameObject, "GamerTagSection");
            m_GamerTagInput     = Finder.FindComponent<TMP_InputField>(m_GamerTagSection, "GamerTagInput");
            // -- title
            m_PlayerTitleButton = Finder.FindComponent<Button>(gameObject, "PlayerTitleButton");
            m_PlayerTitle       = Finder.FindComponent<TMP_Text>(gameObject, "PlayerTitle");
            // -- badges
            m_BadgesSection     = Finder.Find(gameObject, "BadgesSection");
            m_BadgeButtons      = Finder.FindComponents<BadgeButtonUI>(m_BadgesSection);
        }

        public void Initialize(SProfileCurrentData profileCurrentData)
        {
            base.Initialize();

            // setup Avatar Icon & Border
            m_AvatarButtonUI.Initialize(profileCurrentData.Avatar, profileCurrentData.Border);

            // setup GamerTag 
            m_GamerTagInput.text = profileCurrentData.GamerTag;

            // setup Title 
            m_PlayerTitle.text = TextHandler.Split(profileCurrentData.Title);
            if (m_PlayerTitle.text == ETitle.None.ToString())
                m_PlayerTitle.text = "";

            // setup Badges
            InitBadges(profileCurrentData.Badges);

            // deactivate buttons by default
            SetButtonsActive(false);
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Set all buttons active or not
        /// </summary>
        /// <param name="active"></param>
        public void SetButtonsActive(bool active)
        {
            m_AvatarButtonUI.Button.interactable = active;
            m_GamerTagInput.interactable = active;
            m_PlayerTitleButton.interactable = active;

            foreach (var badgeButton in m_BadgeButtons)
                badgeButton.Button.interactable = active;
        }

        public void SetGamerTag(string gamerTag)
        {
            m_GamerTagInput.text = ProfileCloudData.GamerTag;
        }

        public void SetTitle(string title)
        {
            m_PlayerTitle.text = TextHandler.Split(title);
        }

        public void InitBadges(string[] badges)
        {
            if (badges == null)
            {
                ErrorHandler.Error("Null badges provided ");
                return;
            }

            if (badges.Length != ProfileCloudData.N_BADGES_DISPLAYED)
            {
                ErrorHandler.Error("Number of badges " + badges.Length + " missmatch expected number of badges " + ProfileCloudData.N_BADGES_DISPLAYED);
                return;
            }

            for (int i = 0; i < m_BadgeButtons.Count; i++)
            {
                m_BadgeButtons[i].Initialize(badges[i]);
            }
        }

        public void RefreshBadges(string[] badges)
        {
            for (int index = 0; index < m_BadgeButtons.Count; index++)
            {
                m_BadgeButtons[index].RefreshUI(badges[index]);
            }
        }

        public void SelectBadge(int index)
        {
            m_BadgeButtons[index].SetSelected(false);
        }

        #endregion



    }
}