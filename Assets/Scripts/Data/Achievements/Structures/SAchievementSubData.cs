using Data.GameManagement;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Data
{
    [Serializable]
    public struct SAchievementSubData
    {
        [Tooltip("Treshold value that provides the reward")]
        public float MaxValue;

        [Tooltip("Extra rewards (golds, xp, chests, ...)")]
        public SRewardsData Rewards;

        public List<SAchievementReward> AchivementRewardData => Rewards.AchievementRewards;
    }
}