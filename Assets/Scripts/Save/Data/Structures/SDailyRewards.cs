using Data.GameManagement;
using System;
using System.Collections.Generic;

namespace Save.Data
{
    [Serializable]
    public struct SDailyRewardsData
    {
        public List<SRewardsData> Rewards;          // always 5
        public int WeekEndAt;                       // next monday
        public int Streak;                          // full week streak
        public int CurrentIndex;                    // next reward to claim (0..4)
        public int NextCollectAt;                   // next day unlock timestamp

        public bool IsExpired()
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now >= WeekEndAt;
        }

        public bool IsFullyCollected()
        {
            return CurrentIndex >= Rewards.Count && Rewards.Count > 0;
        }

        public bool CanCollect()
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now >= NextCollectAt;
        }

        public int TimeBeforeReset()
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, WeekEndAt - now);
        }

        public int TimeBeforeNextCollect()
        {
            if (NextCollectAt <= 0)
                return NextCollectAt;

            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, NextCollectAt - now);
        }
    }

}