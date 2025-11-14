using Assets.Scripts.Managers;
using Data;
using Enums;
using Menu.Common.Displayers;
using Save;
using System;
using TMPro;
using Tools;
using Unity.VisualScripting;
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
        protected IAchievement m_AchievementData;

        // ==============================================================================================
        // GameObjects & Components
        Button              m_Button;
        RewardsDisplayer    m_RewardDisplayer;
        TMP_Text            m_Title;
        CollectionFillBar   m_FillBar;
        TMP_Text            m_CurrentIndexText;

        public Button Button => m_Button;
        public bool IsUnlockable => m_AchievementData.IsUnlockable;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button            = Finder.FindComponent<Button>(gameObject);
            m_RewardDisplayer   = Finder.FindComponent<RewardsDisplayer>(gameObject);
            m_Title             = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_FillBar           = Finder.FindComponent<CollectionFillBar>(gameObject);
            m_CurrentIndexText  = Finder.FindComponent<TMP_Text>(gameObject, "CurrentIndexText");
        }
        
        public void Initialize(IAchievement achievement)
        {
            // if has no Current value (e.q : is finished) : remove
            if (achievement.Current == null)
            {
                Destroy(gameObject);
                return;
            }

            m_AchievementData = achievement;
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            m_Title.text = TextLocalizer.SplitCamelCase(m_AchievementData.GetName());
            m_CurrentIndexText.text = (m_AchievementData.CurrentIndex + 1).ToString();
            m_FillBar.Initialize(m_AchievementData.GetCount(), m_AchievementData.RequestedValue);
            RefreshReward();
        }

        #endregion


        #region GUI Manipulators

        protected virtual void RefreshUI()
        {
            RefreshReward();
            m_CurrentIndexText.text = (m_AchievementData.CurrentIndex + 1).ToString();
            m_FillBar.UpdateCollection(m_AchievementData.GetCount(), m_AchievementData.RequestedValue);
        }

        void RefreshReward()
        {
            // if has no Current value (e.q : is finished) : remove
            if (m_AchievementData.Current == null)
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
            
            m_RewardDisplayer.Initialize(m_AchievementData.Current.Rewards, 2);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClicked);
            StatCloudData.AnalyticsDataChanged          += OnAnalyticsDataChanged;
            ProfileCloudData.AchievementChangedEvent    += OnAchievementChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Button == null)
                return;

            m_Button.onClick.RemoveAllListeners();
            StatCloudData.AnalyticsDataChanged          -= OnAnalyticsDataChanged;
            ProfileCloudData.AchievementChangedEvent    -= OnAchievementChanged;
        }

        /// <summary>
        /// Action happening when the button is clicked : open the achievement
        /// info popup
        /// </summary>
        void OnClicked()
        {
            if (! m_AchievementData.IsUnlockable)
            {
                ScreenManager.QuickRewardMessage(m_AchievementData.GetDescription(), m_AchievementData.GetCurrent().Rewards, duration: 5f);
                return;
            }

            m_AchievementData.Unlock();
            RefreshUI();
        }

        /// <summary>
        /// Refresh UI of the template when the Annalytics linked to this
        /// achievement is updated in the cloud data
        /// </summary>
        /// <param name="analytics"></param>
        void OnAnalyticsDataChanged(EAnalytics analytics)
        {
            if (m_AchievementData is not AnalyticsAchievementData analyticsAchievementData || analyticsAchievementData.Analytics != analytics)
                return;

            RefreshUI();
        }

        void OnAchievementChanged(string achievementId)
        {
            if (m_AchievementData.GetID() != achievementId)
                return;

            RefreshUI();
        }

        #endregion
    }
}