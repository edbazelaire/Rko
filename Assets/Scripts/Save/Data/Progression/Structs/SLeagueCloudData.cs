using Enums;
using System;


namespace Save.Data.Progression.Structs
{
    [Serializable]
    public struct SLeagueCloudData
    {
        public ELeague CurrentLeague;
        public int CurrentLevel;
        public int CurrentStage;

        public SLeagueCloudData(ELeague league, int level = 0, int stage = 0)
        {
            CurrentLeague = league;
            CurrentLevel = level;
            CurrentStage = stage;
        }
    }
}