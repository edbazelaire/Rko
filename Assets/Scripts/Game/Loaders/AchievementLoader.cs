using Data;
using Enums;
using System.Collections.Generic;
using System.Linq;
using Tools;

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

            //if (statData == EStatData.None)
            //    return achievements;

            //List<AchievementData> filteredAchivements = new();

            //foreach (AchievementData achievement in achievements)
            //{
            //    if (achievement.Ac == statData)
            //        filteredAchivements.Add(achievement);
            //}

            //return filteredAchivements;
        }

        #endregion


    }
}