using Data;
using Data.GameManagement;
using Enums;
using Save;
using System.Collections.Generic;
using System.Linq;
using Tools;

namespace Game.Loaders
{
    public static class AchievementLoader
    {
        #region Members

        static List<AchievementData> m_Achievements;
        static Dictionary<ECharacter, List<CharacterAchievementData>> m_CharacterAchievements;

        public static List<AchievementData> Achievements => m_Achievements;
        public static Dictionary<ECharacter, List<CharacterAchievementData>> CharacterAchievements => m_CharacterAchievements;

        #endregion


        #region Init & End

        public static void Initialize()
        {
            m_Achievements = new();
            m_CharacterAchievements = new();

            var allAchivements = AssetLoader.LoadAll<AchievementData>(AssetLoader.c_AchievementsDataPath).ToList();
            foreach (var achvievementData in allAchivements)
            {
                if (achvievementData is CharacterAchievementData characterAchievementData)
                {
                    if (!m_CharacterAchievements.ContainsKey(characterAchievementData.Character))
                        m_CharacterAchievements.Add(characterAchievementData.Character, new());
                    m_CharacterAchievements[characterAchievementData.Character].Add(characterAchievementData);
                }

                else
                {
                    m_Achievements.Add(achvievementData);
                }
            }

            RegisterListeners();
        }

        #endregion


        #region Accessors

        public static T Get<T> (string name) where T : AchievementData
        {
            return (T)m_Achievements.First((AchievementData data) => data.GetType() == typeof(T) && data.Name == name);
        }

        public static AchievementData Get (EAchievement achievement) 
        {
            return Get(achievement.ToString());
        }

        public static AchievementData Get (string name) 
        {
            return m_Achievements.First((AchievementData data) => data.Name == name);
        }

        #endregion


        #region Character Achievements

        public static SRewardsData GetAllRewardsAtMastery(ECharacter character, int mastery)
        {
            SRewardsData rewards = new SRewardsData();

            // CHECK : character has achievements
            if (! m_CharacterAchievements.ContainsKey(character)) 
            {
                ErrorHandler.Warning("Unable to find any achievement for character " + character);
                return rewards;
            }

            // get all rewards for each achievements
            foreach (var achievementData in m_CharacterAchievements[character])
            {
                rewards.Add(achievementData.GetAllRewardsAtMastery(mastery));
            }

            return rewards;
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