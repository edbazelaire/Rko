using Assets.Scripts.Managers;
using Enums;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Menu.MainMenu
{
    public class ProfileDisplayBE : MObject
    {
        #region Members

        // =============================================================================
        // Data
        int m_CurrentBadgeIndex = -1;

        // =============================================================================
        // GameObjects & Components
        AchievementsTabManager          m_AchievementTabsManager;
        ProfileDisplayUI                m_ProfileDisplayUI;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_AchievementTabsManager        = Finder.FindComponent<AchievementsTabManager>(transform.parent.gameObject);
            m_ProfileDisplayUI              = Finder.FindComponent<ProfileDisplayUI>(gameObject);
        }

        protected override void SetUpUI()
        {
            // initialize UI of the profile
            m_ProfileDisplayUI.Initialize(ProfileCloudData.CurrentProfileData);

            // set all profile disaply ui buttons active
            m_ProfileDisplayUI.SetButtonsActive(true);
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Set badge at provided index as currently selected badge that can will be changed when selecting a new badge
        /// </summary>
        /// <param name="index"></param>
        void SelectBadge(int index)
        {
            DeselectCurrentBadge();

            if (index < 0 || index >= m_ProfileDisplayUI.BadgeButtons.Count)
            {
                ErrorHandler.Error("Badges list has no index " + index);
                return;
            }

            ProfileCloudData.LastSelectedBadgeIndex = index;
            m_CurrentBadgeIndex = index;
            m_ProfileDisplayUI.BadgeButtons[m_CurrentBadgeIndex].SetSelected(true);
        }

        void DeselectCurrentBadge()
        {
            if (m_CurrentBadgeIndex < 0)
                return;

            if (m_CurrentBadgeIndex > m_ProfileDisplayUI.BadgeButtons.Count)
            {
                ErrorHandler.Error("Bades list has no index " + m_CurrentBadgeIndex);
                return;
            }

            m_ProfileDisplayUI.BadgeButtons[m_CurrentBadgeIndex].SetSelected(false);
            m_CurrentBadgeIndex = -1;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            // -- Account buttons
            m_ProfileDisplayUI.PseudoChangeButton.onClick.AddListener(OnPseudoButtonClicked);

            // -- Achievement Reward Profile button
            m_ProfileDisplayUI.AvatarButtonUI.Button.onClick.AddListener(() => m_AchievementTabsManager.SelectTab(EAchievementTab.Avatars));
            m_ProfileDisplayUI.PlayerTitleButton.onClick.AddListener(() => m_AchievementTabsManager.SelectTab(EAchievementTab.Titles));

            for (int i = 0; i < m_ProfileDisplayUI.BadgeButtons.Count; i++)
            {
                // duplicate i to avoid issue with setting listeners
                int badgeIndex = i;
                m_ProfileDisplayUI.BadgeButtons[i].Button.onClick.AddListener(() => OnCurrentBadgeButtonClicked(badgeIndex));
            }

            // -- External listeners
            ProfileCloudData.PseudoChangedEvent         += OnPseudoChanged;
            ProfileCloudData.CurrentDataChanged         += OnCurrentDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_ProfileDisplayUI.PseudoChangeButton.onClick.RemoveAllListeners();
            m_ProfileDisplayUI.AvatarButtonUI.Button.onClick.RemoveAllListeners();
            m_ProfileDisplayUI.PlayerTitleButton.onClick.RemoveAllListeners();

            // -- badges
            for (int i = 0; i < m_ProfileDisplayUI.BadgeButtons.Count; i++)
            {
                m_ProfileDisplayUI.BadgeButtons[i].Button.onClick.RemoveAllListeners();
            }

            ProfileCloudData.PseudoChangedEvent     -= OnPseudoChanged;
            ProfileCloudData.CurrentDataChanged     -= OnCurrentDataChanged;
        }


        void OnPseudoButtonClicked()
        {
            ScreenManager.SetPopUp(EPopUpState.PseudoPopUp);
        }

        void OnCurrentBadgeButtonClicked(int index)
        {
            // Re-click selected : close selection
            if (m_CurrentBadgeIndex == index)
            {
                DeselectCurrentBadge();
            }

            // Click other than selected 
            if (m_CurrentBadgeIndex != index)
            {
                // set clicked button as selected
                SelectBadge(index);
                m_AchievementTabsManager.SelectTab(EAchievementTab.Badges);
            }
        }

        void OnCurrentDataChanged(EAchievementReward achievementReward)
        {
            string newValue = ProfileCloudData.GetCurrentData(achievementReward);

            Debug.Log("OnCurrentDataChanged : " + achievementReward + " - value = " + newValue);

            switch (achievementReward)
            {
                case EAchievementReward.Avatar:
                    m_ProfileDisplayUI.AvatarButtonUI.SetAvatar(newValue);
                    return;

                case EAchievementReward.Border:
                    m_ProfileDisplayUI.AvatarButtonUI.SetBorder(newValue);
                    return;

                case EAchievementReward.Title:
                    m_ProfileDisplayUI.SetTitle(newValue);
                    return;

                case EAchievementReward.Badge:
                    m_ProfileDisplayUI.RefreshBadges(ProfileCloudData.CurrentBadges);
                    return;
            }
        }

        void OnPseudoChanged()
        {
            m_ProfileDisplayUI.SetPseudo(ProfileCloudData.GamerTag);
            m_ProfileDisplayUI.PseudoChangeButton.gameObject.SetActive(ProfileCloudData.CanChangePseudo);
        }

        #endregion
    }
}