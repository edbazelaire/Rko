using Data;
using Enums;
using Menu.Common.Displayers;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class TemplateAchievementButton : MObject
    {
        #region Members

        // ==============================================================================================
        // Actions
        public static Action<bool> AchievementUpdateEvent;

        // ==============================================================================================
        // Data
        AchievementData m_Achievement;
        bool m_IsMaxed;

        // ==============================================================================================
        // GameObjects & Components
        Button              m_Button;
        RewardsDisplayer    m_RewardDisplayer;
        TMP_Text            m_Title;
        CollectionFillBar   m_FillBar;

        public Button Button => m_Button;
        public bool IsMaxed => m_IsMaxed;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button            = Finder.FindComponent<Button>(gameObject);
            m_RewardDisplayer   = Finder.FindComponent<RewardsDisplayer>(gameObject);
            m_Title             = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_FillBar           = Finder.FindComponent<CollectionFillBar>(gameObject);
        }
        
        public void Initialize(AchievementData achievement)
        {
            // if has no Current value (e.q : is finished) : remove
            if (!achievement.Current.HasValue)
            {
                Destroy(gameObject);
                return;
            }

            m_Achievement = achievement;
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            m_Title.text = TextLocalizer.SplitCamelCase(m_Achievement.name);
            m_FillBar.Initialize(m_Achievement.Count, m_Achievement.RequestedValue);
            RefreshReward();
            RefreshIsMaxed();
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            RefreshReward();
            RefreshIsMaxed();
            m_FillBar.UpdateCollection(m_Achievement.Count, m_Achievement.RequestedValue);
        }

        void RefreshReward()
        {
            // if has no Current value (e.q : is finished) : remove
            if (!m_Achievement.Current.HasValue)
            {
                Destroy(gameObject);
                return;
            }

            // check if rewards displayer was provided
            if (m_RewardDisplayer == null)
            {
                ErrorHandler.Error("No RewardDisplayer");
                return;
            }
            
            m_RewardDisplayer.Initialize(m_Achievement.Current.Value.Rewards, 2);
        }

        void RefreshIsMaxed()
        {
            // check if is a new completion (to update notifications)
            bool isMaxed = m_Achievement.Count >= m_Achievement.RequestedValue;
            if (isMaxed != m_IsMaxed)
            {
                AchievementUpdateEvent?.Invoke(isMaxed);
            }

            m_IsMaxed = isMaxed;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClicked);
            StatCloudData.AnalyticsDataChanged += OnAnalyticsDataChanged;
        }


        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Button == null)
                return;

            m_Button.onClick.RemoveAllListeners();
            StatCloudData.AnalyticsDataChanged -= OnAnalyticsDataChanged;
        }

        /// <summary>
        /// Action happening when the button is clicked : open the achievement
        /// info popup
        /// </summary>
        void OnClicked()
        {
            if (! m_Achievement.IsUnlockable)
            {
                Debug.LogWarning("Achivement not unlockable");
                return;
            }

            m_Achievement.Unlock();
            RefreshUI();
        }

        /// <summary>
        /// Refresh UI of the template when the Annalytics linked to this
        /// achievement is updated in the cloud data
        /// </summary>
        /// <param name="analytics"></param>
        void OnAnalyticsDataChanged(EAnalytics analytics)
        {
            if (m_Achievement.Analytics != analytics)
                return;

            RefreshUI();
        }

        #endregion
    }
}