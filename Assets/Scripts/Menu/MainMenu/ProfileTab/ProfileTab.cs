using Data;
using Menu.Common.Buttons;
using Menu.Common.Notifications;
using Tools;
using UnityEngine;

namespace Menu.MainMenu
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

            CheckNotifications();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_ProfileDisplayBE.Initialize();
        }

        #endregion


        #region Notifications

        /// <summary>
        /// Check if achievements are 
        /// </summary>
        void CheckNotifications()
        {
            var achievementButtons = Finder.FindComponents<TemplateAchievementButton>(gameObject);
            foreach (var acButton in achievementButtons)
            {
                if (acButton.IsMaxed)
                    m_NAchivementsToCollect++;
            }

            RefreshTabButtonNotifications();
        }

        void RefreshTabButtonNotifications()
        {
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
            TemplateAchievementButton.AchievementUpdateEvent += OnAchievementUpdate;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            TemplateAchievementButton.AchievementUpdateEvent -= OnAchievementUpdate;
        }

        protected void OnAchievementUpdate(bool isCompleted)
        {
            m_NAchivementsToCollect += isCompleted ? 1 : -1;
            RefreshTabButtonNotifications();
        }

        #endregion
    }
}