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
            // load all scriptables in Achievements data file
            var loadedAssets = Resources.LoadAll<ScriptableObject>(AssetLoader.c_AchievementsDataPath);

            // clone all achievements (to be able to manipulate without re-writting)
            m_Achievements = new List<IAchievement>();
            foreach (var asset in loadedAssets)
            {
                var clone = Object.Instantiate(asset);

                if (clone is ArenaAchievementData arenaData)
                {
                    arenaData.OverrideSubAchievements();
                    m_Achievements.Add(arenaData);
                }
                else if (clone is IAchievement ach)
                {
                    m_Achievements.Add(ach);
                }
                else
                {
                    ErrorHandler.Warning("Found ScriptableObject that is not an IAchievement in " + AssetLoader.c_AchievementsDataPath + " : " + clone.name);
                }
            }
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


        #endregion
    }

    
}