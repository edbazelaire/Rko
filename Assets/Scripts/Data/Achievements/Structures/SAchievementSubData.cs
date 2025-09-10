using Data.GameManagement;
using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Data
{
    [Serializable]
    public class SAchievementSubData
    {
        [Tooltip("Treshold value that provides the reward")]
        public float MaxValue;

        [Tooltip("Extra rewards (golds, xp, chests, ...)")]
        public SRewardsData Rewards;

        public List<SAchievementReward> AchivementRewardData => Rewards.AchievementRewards;
    }

    [Serializable]
    public class SArenaAchievementSubData : SAchievementSubData
    {
        public EArenaDifficulty ArenaDifficulty;
        public List<EArenaMod>  ArenaMods;
        public int              ArenaExtraDifficulty = 0;

        /// <summary>
        /// Check at for this specific step if all Arena context requirements are met
        /// </summary>
        /// <param name="arenaDifficulty"></param>
        /// <param name="arenaMods"></param>
        /// <param name="arenaArenaExtraDifficulty"></param>
        /// <returns></returns>
        public bool Check(EArenaDifficulty arenaDifficulty, List<EArenaMod> arenaMods, int arenaArenaExtraDifficulty)
        {
            if (arenaDifficulty < ArenaDifficulty)
                return false;

            if (arenaArenaExtraDifficulty < ArenaExtraDifficulty)
                return false;

            if (ArenaMods.IsNullOrEmpty())
                return true;

            foreach (EArenaMod mod in ArenaMods)
            {
                if (!arenaMods.Contains(mod))
                    return false;
            }

            return true;
        }
    }
}