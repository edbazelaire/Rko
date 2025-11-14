using Menu.Common.Buttons;
using Save;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;


namespace Menu.PopUps
{
    public class DailyRewardsPopUp : PopUp
    {
        #region Members

        List<DailyRewardItem> m_DailyRewardItems = new();
        GameObject m_NextCollectionTextContainer;
        TMP_Text m_NextCollectionText;
        TMP_Text m_HotStreakCounter;

        bool m_IsCollectionBlocked = false;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_DailyRewardItems              = Finder.FindComponents<DailyRewardItem>(gameObject);
            m_NextCollectionTextContainer   = Finder.Find(gameObject, "NextCollectionTextContainer");
            m_NextCollectionText            = Finder.FindComponent<TMP_Text>(m_NextCollectionTextContainer, "NextCollectionText");
            m_HotStreakCounter              = Finder.FindComponent<TMP_Text>(gameObject, "HotStreakCounter");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            int index = 0;
            foreach (var item in m_DailyRewardItems)
            {
                item.Initialize(index);
                index++;
            }

            RefreshUI();
        }

        #endregion


        #region GUI Manipulators

        protected override void Update()
        {
            base.Update();

            if (TimeCloudData.IsWeekCollected)
            {
                m_NextCollectionText.text = "Rest in " + TextHandler.FormatTimestamp(TimeCloudData.DailyRewards.TimeBeforeReset());
            }
            else if (! TimeCloudData.DailyRewards.CanCollect())
            {
                m_NextCollectionText.text = "Next collect in " + TextHandler.FormatTimestamp(TimeCloudData.DailyRewards.TimeBeforeNextCollect());
            } else if (m_IsCollectionBlocked)
            {
                OnPrefabLoaded();
            }
        }

        void RefreshUI()
        {
            RefreshHotStreak();

            if (TimeCloudData.IsWeekCollected || ! TimeCloudData.DailyRewards.CanCollect())
            {
                m_IsCollectionBlocked = true;
                m_NextCollectionTextContainer.SetActive(true);
            }
            else
            {
                m_IsCollectionBlocked = false;
                m_NextCollectionTextContainer.SetActive(false);
            }
        }

        void RefreshHotStreak()
        {
            m_HotStreakCounter.text = TimeCloudData.DailyRewards.Streak.ToString();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            TimeCloudData.DailyRewardCollected += RefreshUI;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            TimeCloudData.DailyRewardCollected -= RefreshUI;
        }

        #endregion
    }

}
