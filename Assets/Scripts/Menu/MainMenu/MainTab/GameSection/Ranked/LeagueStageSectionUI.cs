using Assets;
using Enums;
using Save;
using Tools;

namespace Menu.MainMenu.MainTab
{
    public class LeagueStageSectionUI : StageSectionUI
    {
        #region Members

        ELeague m_League;

        #endregion


        #region Init & End

        public void Initialize(ELeague league, int level)
        {
            m_League = league;

            base.Initialize(level, ProgressionCloudData.CurrentLeagueLevel, ProgressionCloudData.CurrentLeagueStage, Main.LeagueDataConfig.GetLeagueLevelData(league, level).NStages);
        }

        #endregion


        #region GUI Manipulators

        protected override void SetKnobColor(int index)
        {
            // Check League first
            if (m_League < ProgressionCloudData.CurrentLeague)
            {
                m_Knobs[index].sprite = m_KnobCompleted;
                return;
            }

            if (m_League > ProgressionCloudData.CurrentLeague)
            {
                m_Knobs[index].sprite = m_KnobNotDone;
                return;
            }

            base.SetKnobColor(index);
        }

        #endregion


        #region Helpers 

        protected override string GetLevelString()
        {
            return m_League.ToString() + " " + TextHandler.ToRoman(Main.LeagueDataConfig.GetLeagueData(m_League).LevelData.Count - m_Level);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            ProgressionCloudData.LeagueDataChangedEvent += OnLeagueDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProgressionCloudData.LeagueDataChangedEvent -= OnLeagueDataChanged;
        }

        void OnLeagueDataChanged()
        {
            m_Level         = ProgressionCloudData.CurrentLeagueLevel;
            m_CurrentLevel  = ProgressionCloudData.CurrentLeagueLevel;
            m_CurrentStage  = ProgressionCloudData.CurrentLeagueStage;
            m_NStages       = Main.LeagueDataConfig.GetLeagueLevelData(ProgressionCloudData.CurrentLeague, ProgressionCloudData.CurrentLeagueLevel).NStages;

            RefreshUI();
        }

        #endregion
    }
}