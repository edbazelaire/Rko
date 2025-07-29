using Assets.Scripts.Managers;
using Enums;
using Menu.Common.Buttons;
using Menu.PopUps.Components.ProfilePopUp;
using Save;
using System.Collections.Generic;
using System.Linq;
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
        // -- pseudo
        GameObject          m_PseudoSection;
        TMP_Text            m_Pseudo;
        // -- account manipulation buttons
        Button              m_PseudoChangeButton;
        // -- title
        Button              m_PlayerTitleButton;
        TMP_Text            m_PlayerTitle;
        // -- badges
        GameObject          m_BadgesSection;
        List<BadgeButtonUI> m_BadgeButtons;

        // ===============================================================================
        // Public Acessors
        public AvatarButtonUI           AvatarButtonUI      => m_AvatarButtonUI;
        public Button                   PseudoChangeButton  => m_PseudoChangeButton;
        public Button                   PlayerTitleButton   => m_PlayerTitleButton;
        public List<BadgeButtonUI>      BadgeButtons        => m_BadgeButtons;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // -- avatar section
            m_AvatarButtonUI        = Finder.FindComponent<AvatarButtonUI>(gameObject);

            // -- pseudo
            m_PseudoSection         = Finder.Find(gameObject, "PseudoSection");
            m_Pseudo                = Finder.FindComponent<TMP_Text>(m_PseudoSection, "Pseudo");
            // -- account manipulation buttons
            m_PseudoChangeButton    = Finder.FindComponent<Button>(gameObject, "PseudoChangeButton");
            // -- title
            m_PlayerTitleButton     = Finder.FindComponent<Button>(gameObject, "PlayerTitleButton");
            m_PlayerTitle           = Finder.FindComponent<TMP_Text>(gameObject, "PlayerTitle");
            // -- badges
            m_BadgesSection         = Finder.Find(gameObject, "BadgesSection");
            m_BadgeButtons          = Finder.FindComponents<BadgeButtonUI>(m_BadgesSection);
        }

        public void Initialize(SProfileCurrentData profileCurrentData)
        {
            base.Initialize();

            // setup Avatar Icon & Border
            m_AvatarButtonUI.Initialize(profileCurrentData.Avatar.ToString(), profileCurrentData.Border.ToString());

            // setup GamerTag 
            m_Pseudo.text = profileCurrentData.GamerTag.ToString();

            // setup Title 
            SetTitle(profileCurrentData.Title.ToString());

            // setup Badges
            InitBadges(profileCurrentData.Badges.Select(badge => badge.ToString()).ToArray());

            // deactivate buttons by default
            SetButtonsActive(false);
        }

        public void Initialize(SProfileDataNetwork profileCurrentData)
        {
            base.Initialize();

            // setup Avatar Icon & Border
            m_AvatarButtonUI.Initialize(profileCurrentData.Avatar.ToString(), profileCurrentData.Border.ToString());

            // setup GamerTag 
            m_Pseudo.text = profileCurrentData.GamerTag.ToString();

            // setup Title 
            SetTitle(profileCurrentData.Title.ToString());

            // setup Badges
            InitBadges(profileCurrentData.Badges.Select(badge => badge.ToString()).ToArray());

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
            // account manipulation buttons
            m_PseudoChangeButton.gameObject.SetActive(active && ProfileCloudData.CanChangePseudo);

            // account achievements buttons
            m_AvatarButtonUI.Button.interactable = active;
            m_PlayerTitleButton.interactable = active;
            foreach (var badgeButton in m_BadgeButtons)
                badgeButton.Button.interactable = active;
        }

        public void SetPseudo(string pseudo)
        {
            m_Pseudo.text = pseudo;
        }

        public void SetTitle(string title)
        {
            // clean title value into string
            title = TextHandler.Split(title);

            // set to empty title if title is "None"
            if (title == ETitle.None.ToString())
                title = "";

            // display title
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


        #region Listeners



        #endregion

    }
}