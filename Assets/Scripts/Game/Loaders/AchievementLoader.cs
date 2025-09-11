using Assets.Scripts.Game.Loaders.Filters;
using Data;
using Data.GameManagement;
using Enums;
using MyBox;
using Save;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Game.Loaders
{
    public static class AchievementLoader
    {
        #region Members

        static List<IAchievement> m_Achievements;
        public static List<IAchievement> Achievements => m_Achievements;

        #endregion


        #region Init & End

        public static void Initialize()
        {
            m_Achievements = new();

            // DEFAULT achievements
            m_Achievements = Resources.LoadAll<ScriptableObject>(AssetLoader.c_AchievementsDataPath).OfType<IAchievement>().ToList();

            RegisterListeners();
        }

        #endregion


        #region Accessors

        public static List<T> Get<T> (ECharacter character, bool strict = false)
        {
            return m_Achievements.FilterByCharacter(character, strict).FilterByType<T>();
        }

        public static IAchievement Get (EAchievement achievement) 
        {
            return m_Achievements.FilterByName(achievement.ToString());
        }

        #endregion


        #region Character Achievements

        public static SRewardsData GetAllRewardsAtMastery(ECharacter character, int mastery)
        {
            SRewardsData rewards = new SRewardsData();

            var achievements = m_Achievements.FilterByCharacter(character, strict: true);

            // CHECK : character has achievements
            if (achievements.Count == 0) 
            {
                ErrorHandler.Warning("Unable to find any achievement for character " + character);
                return rewards;
            }

            // get all rewards for each achievements
            foreach (var achievementData in achievements)
            {
                rewards.Add(achievementData.GetAllRewardsAtMastery(mastery));
            }

            return rewards;
        }

        #endregion


        #region Listeners

        static void RegisterListeners()
        {
            // hook "on arena ended" events
            //foreach (var achievement in m_Achievements)
            //{
            //    if (achievement is ArenaAchievementData arenaAchievement)
            //    {
            //        ProgressionCloudData.CurrentArenaEndedEvent += arenaAchievement.CheckOnArenaEnded;
            //    }
            //}
        }

        #endregion
    }

    
}