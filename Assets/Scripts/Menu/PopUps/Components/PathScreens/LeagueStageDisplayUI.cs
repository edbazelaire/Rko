using Assets;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Sound;
using Data.GameManagement;
using Enums;
using Menu.Common.Displayers;
using Menu.Common.Notifications;
using Menu.MainMenu.MainTab;
using Save;

namespace Menu.PopUps
{
    public class LeagueStageDisplayUI : StageDisplayUI
    {
        #region Members
       
        ELeague             m_League;
        int                 m_Level;

        #endregion


        #region Init & End

        public void Initialize(ELeague league, int level)
        {
            m_League = league;
            m_Level = level;

            base.Initialize();

            (m_StageSectionUI as LeagueStageSectionUI).Initialize(league, level);
        }

        #endregion


        #region State

        protected override void RefreshState()
        {
            if (NotificationCloudData.HasRewardsForLeagueAtLevel(m_League, m_Level))
            {
                SetState(EStageRewardState.Unlocked);
                return;
            }

            if (m_League < ProgressionCloudData.CurrentLeague)
            {
                SetState(EStageRewardState.Collected);
                return;
            }

            if (m_League > ProgressionCloudData.CurrentLeague)
            {
                SetState(EStageRewardState.Locked);
                return;
            }

            if (m_Level < ProgressionCloudData.CurrentLeagueLevel)
            {
                SetState(EStageRewardState.Collected);
                return;
            }

            SetState(EStageRewardState.Locked);
        }

        #endregion


        #region Rewards

        protected override SRewardsData GetRewards() 
        {
            return Main.LeagueDataConfig.GetLeagueLevelData(m_League, m_Level).Rewards;
        }


        protected override void CollectReward()
        {
            base.CollectReward();

            if (!NotificationCloudData.CollectLeagueReward(m_League, m_Level))
                return;

            ScreenManager.DisplayRewards(GetRewards(), ERewardContext.LeagueReward.ToString());
        }

        #endregion
    }
}