using Data;
using Enums;
using Save;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Loaders
{
    public static class AchievementLoader
    {
        #region Members

        static List<AchievementData> m_Achievements;

        public static List<AchievementData> Achievements => m_Achievements;

        #endregion


        #region Init & End
        
        public static void Initialize()
        {
            m_Achievements = AssetLoader.LoadAll<AchievementData>(AssetLoader.c_AchievementsDataPath).ToList();

            RegisterListeners();
        }

        #endregion


        #region Accessors

        public static T Get<T> () where T : AchievementData
        {
            return (T)m_Achievements.First((AchievementData data) => data.GetType() == typeof(T));
        }

        public static AchievementData Get (string name) 
        {
            return m_Achievements.First((AchievementData data) => data.Name == name);
        }

        #endregion


        #region Filters

        /// <summary>
        /// Get only Achievements that have a linke to the provided StatData
        /// </summary>
        /// <param name="achievements"></param>
        /// <param name="statData"></param>
        /// <returns></returns>
        public static List<AchievementData> FilterAchievementsByStatData(List<AchievementData> achievements, EAnalytics analytics)
        {
            ErrorHandler.Warning("Call deactivated method : FilterAchievementsByStatData()");
            return achievements;
        }

        #endregion


        #region Listeners

        static void RegisterListeners()
        {
            // hook "on arena ended" events
            foreach (var achievement in m_Achievements)
            {
                if (achievement is ArenaAchievementData arenaAchievement)
                {
                    ProgressionCloudData.CurrentArenaEndedEvent += arenaAchievement.CheckOnArenaEnded;
                }
            }
        }

        #endregion
    }
}