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
        public EArenaType       ArenaType           = EArenaType.None;
        public EArenaDifficulty ArenaDifficulty     = EArenaDifficulty.Easy;
        public List<EArenaMod>  ArenaMods           = new();

        /// <summary>
        /// Check at for this specific step if all Arena context requirements are met
        /// </summary>
        /// <param name="arenaDifficulty"></param>
        /// <param name="arenaMods"></param>
        /// <param name="arenaArenaExtraDifficulty"></param>
        /// <returns></returns>
        public bool Check(EArenaType arenaType, EArenaDifficulty arenaDifficulty, List<EArenaMod> arenaMods)
        {
            if (arenaType != EArenaType.None && arenaType != ArenaType)
                return false;

            if (arenaDifficulty < ArenaDifficulty)
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

        #region Override

        /// <summary>
        /// Allow parent ArenaAchievement to override threshod conditions (to set values only once if shared by all subconditions)
        /// </summary>
        /// <param name="arenaType"></param>
        /// <param name="arenaDifficulty"></param>
        /// <param name="arenaExtraDifficulty"></param>
        /// <param name="arenaMods"></param>
        public void Override(EArenaType arenaType, EArenaDifficulty arenaDifficulty, List<EArenaMod> arenaMods)
        {
            if (arenaType > ArenaType)
                ArenaType = arenaType;

            if (arenaDifficulty > ArenaDifficulty)
                ArenaDifficulty = arenaDifficulty;

            if (arenaMods != null)
            {
                foreach (EArenaMod mod in arenaMods)
                {
                    if (! ArenaMods.Contains(mod))
                        ArenaMods.Add(mod);
                }
            }
        }

        #endregion


        #region Description

        public string GetDescription()
        {
            string arena = ArenaType != 0 && ArenaType > EArenaType.None ? ArenaType.ToString() : "any arena";
            string description = $"Finish <i>{arena}</i> in difficulty <b>{ArenaDifficulty}</b>";
            if (!ArenaMods.IsNullOrEmpty())
                description += $" in <b>{String.Join(", ", ArenaMods)}</b> mod";
            if (MaxValue > 1)
                description += $" {MaxValue} times";

            return description;
        }

        #endregion

    }
}