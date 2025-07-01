using Data;
using Game.Loaders;
using Menu.Common.Buttons;
using Menu.Common.Notifications;
using Save;
using Tools;
using UnityEngine;

namespace Menu.MainMenu.ProfileTab
{
    public class ProfileTab : MainMenuTabContent
    {
        #region Members

        ProfileDisplayBE m_ProfileDisplayBE;
        int m_NAchivementsToCollect;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ProfileDisplayBE = Finder.FindComponent<ProfileDisplayBE>(gameObject, "ProfileDisplay");
        }

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            RefreshTabButtonNotifications();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_ProfileDisplayBE.Initialize();
        }

        #endregion


        #region Notifications

        void RefreshTabButtonNotifications()
        {
            m_NAchivementsToCollect = 0;
            foreach (var achievementData in AchievementLoader.Achievements)
            {
                if (achievementData.IsUnlockable)
                    m_NAchivementsToCollect++;
            }

            if (m_NAchivementsToCollect < 0)
            {
                ErrorHandler.Error("m_NAchivementsToCollect (" + m_NAchivementsToCollect + ") < 0");
                m_NAchivementsToCollect = 0;
            }

            if (m_NAchivementsToCollect > 0)
            {
                NotificationParticles.Add(m_TabButton.gameObject, m_TabButton.BackgroundImage, new Vector2(1f, 1f));
            }
            else
            {
                NotificationParticles.Remove(m_TabButton.gameObject);
            }
        }

        #endregion



        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            ProfileCloudData.AchievementChangedEvent    += OnAchievementChanged;
            ProfileCloudData.AchievementCompletedEvent  += OnAchievementChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProfileCloudData.AchievementChangedEvent    -= OnAchievementChanged;
            ProfileCloudData.AchievementCompletedEvent  -= OnAchievementChanged;
        }

        protected void OnAchievementChanged(string _)
        {
            RefreshTabButtonNotifications();
        }

        #endregion
    }
}