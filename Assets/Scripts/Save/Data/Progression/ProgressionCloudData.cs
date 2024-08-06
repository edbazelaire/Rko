using Assets;
using Data.GameManagement;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Services.CloudSave.Models;

namespace Save
{
    [Serializable]
    public struct SArenaCloudData
    {
        public int              CurrentLevel;
        public int              CurrentStage;
        public EArenaDifficulty CurrentDifficulty;

        public SArenaCloudData(int level = 0, int stage = 0, EArenaDifficulty currentDifficulty = EArenaDifficulty.Normal)
        {
            CurrentLevel = level;
            CurrentStage = stage;
            CurrentDifficulty = currentDifficulty;
        }
    }

    [Serializable]
    public struct SLeagueCloudData
    {
        public ELeague  CurrentLeague;
        public int      CurrentLevel;
        public int      CurrentStage;

        public SLeagueCloudData(ELeague league, int level = 0, int stage = 0)
        {
            CurrentLeague   = league;
            CurrentLevel    = level;
            CurrentStage    = stage;
        }
    }

    public class ProgressionCloudData : CloudData
    {
        #region Members

        public new static ProgressionCloudData Instance => Main.CloudSaveManager.GetCloudData(typeof(ProgressionCloudData)) as ProgressionCloudData;

        // ===============================================================================================
        // CONSTANTS
        public const string KEY_LEAGUE          = "League";
        public const string KEY_SOLO_ARENAS     = "SoloArenas";

        // ===============================================================================================
        // ACTIONS
        public static Action LeagueDataChangedEvent;
        public static Action<EArenaType> ArenaDataChangedEvent;

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_LEAGUE,               new SLeagueCloudData(ELeague.Iron)              },
            { KEY_SOLO_ARENAS,          new Dictionary<EArenaType, SArenaCloudData>()   },
        };

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        public static SLeagueCloudData                          LeagueCloudData         => (SLeagueCloudData)Instance.m_Data[KEY_LEAGUE];
        public static ELeague                                   CurrentLeague           => LeagueCloudData.CurrentLeague;
        public static int                                       CurrentLeagueLevel      => LeagueCloudData.CurrentLevel;
        public static int                                       CurrentLeagueStage      => LeagueCloudData.CurrentStage;
        public static Dictionary<EArenaType, SArenaCloudData>   SoloArenas              => Instance.m_Data[KEY_SOLO_ARENAS] as Dictionary<EArenaType, SArenaCloudData>;
        public static EArenaDifficulty                          MaxArenaDifficulty      => ((EArenaDifficulty[])Enum.GetValues(typeof(EArenaDifficulty))).Last();

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert CloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <returns></returns>
        protected override object Convert(Item item)
        {
            try
            {
                var itemType = m_Data[item.Key].GetType();

                if (itemType == typeof(Dictionary<EArenaType, SArenaCloudData>))
                {
                    return item.Value.GetAs<Dictionary<EArenaType, SArenaCloudData>>();
                }

                if (itemType == typeof(SLeagueCloudData))
                {
                    return item.Value.GetAs<SLeagueCloudData>();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Error("Unable to convert item " + item.Key);
                ErrorHandler.Error(ex.Message);
                return m_Data[item.Key];
            }

            return base.Convert(item);
        }

        #endregion


        #region General Data

        /// <summary>
        /// When a game starts, apply a preventive game loss that allows to handle disconnections
        /// </summary>
        /// <param name="gameMode"></param>
        /// <returns></returns>
        public static bool ApplyPreventiveLoss(EGameMode gameMode)
        {
            switch (gameMode)
            {
                case EGameMode.Arena:
                    if (SoloArenas[PlayerPrefsHandler.GetArenaType()].CurrentStage == 0)
                        return false;

                    UpdateStageValue(PlayerPrefsHandler.GetArenaType(), false);
                    break;

                case EGameMode.Ranked:
                    if (CurrentLeagueStage == 0)
                        return false;

                    UpdateLeagueValue(false);
                    break;
            }

            return true;
        }

        #endregion


        #region League Data

        public static bool IsLeagueCompleted()
        {
            return CurrentLeague >= Main.LeagueDataConfig.LeagueDataList[^1].League;
        }

        public static void UpdateLeagueValue(bool up, int nTimes = 1, bool save = true)
        {
            var leagueCloudData = LeagueCloudData;
            bool hasChanged = false;

            // Check that nTimes is only 1 or 2, other cases should not occure
            if (nTimes != 1 && nTimes != 2)
            {
                ErrorHandler.Error("Unhandled case : nTimes != 1 or 2");
                return;
            }

            // Repeat the action nTimes
            for (int i = 0; i < nTimes; i++)
            {
                // Retrieve stage level
                if (!up)
                {
                    // stage level is 0 : do nothing
                    if (leagueCloudData.CurrentStage == 0)
                        break;

                    hasChanged = true;
                    leagueCloudData.CurrentStage--;
                }

                else
                {
                    hasChanged = true;
                    SLeagueData leagueData = Main.LeagueDataConfig.CurrentLeagueData;

                    // ADD stage level
                    if (leagueCloudData.CurrentStage == leagueData.LevelData[CurrentLeagueLevel].NStages)
                        UpgradeLeagueLevel(false);
                    else
                        leagueCloudData.CurrentStage++;
                }
            }

            if (save && hasChanged)
                SaveLeagueData(leagueCloudData);
        }

        public static void UpgradeLeagueLevel(bool save = true)
        {
            // reached max level of the league : go to next league 
            if (CurrentLeagueLevel == Main.LeagueDataConfig.CurrentLeagueData.LevelData.Count - 1)
            {
                UpgradeLeague(save);
                return;
            } 
           
            // add rewards to notification data so they can be collected later
            NotificationCloudData.AddLeagueLevelReward(CurrentLeague, CurrentLeagueLevel);

            SLeagueCloudData leagueCloudData = LeagueCloudData;
            leagueCloudData.CurrentStage = 0;
            leagueCloudData.CurrentLevel++;

            if (save)
                SaveLeagueData(leagueCloudData);
        }

        public static void UpgradeLeague(bool save = true)
        {
            SLeagueCloudData leagueCloudData = LeagueCloudData;

            ELeague newLeague = ELeague.Champion;
            if (Enum.IsDefined(typeof(ELeague), (int)CurrentLeague + 1))
            {
                newLeague = (ELeague)((int)CurrentLeague + 1);
            }

            leagueCloudData.CurrentStage = 0;
            leagueCloudData.CurrentLevel = 0;
            leagueCloudData.CurrentLeague = newLeague;

            if (save)
                SaveLeagueData(leagueCloudData);
        }

        public static void SaveLeagueData(SLeagueCloudData leagueCloudData)
        {
            Instance.m_Data[KEY_LEAGUE] = leagueCloudData;
            Instance.SaveValue(KEY_LEAGUE);

            LeagueDataChangedEvent?.Invoke();
        }

        #endregion


        #region Arena Data

        public static EArenaDifficulty GetArenaDifficulty(EArenaType arenaType) => SoloArenas[arenaType].CurrentDifficulty;

        public static bool IsArenaDifficultyCompleted(EArenaType arenaType)
        {
            return SoloArenas[arenaType].CurrentLevel >= AssetLoader.LoadArenaData(arenaType).MaxLevel;
        }

        public static bool IsArenaCompleted(EArenaType arenaType)
        {
            return SoloArenas[arenaType].CurrentLevel >= AssetLoader.LoadArenaData(arenaType).MaxLevel && GetArenaDifficulty(arenaType) == EArenaDifficulty.HardCore;
        }

        public static void UpdateStageValue(EArenaType arenaType, bool up, int nTimes = 1, bool save = true)
        {
            bool hasChanged = false;

            // Check that nTimes is only 1 or 2, other cases should not occure
            if (nTimes != 1 && nTimes != 2)
            {
                ErrorHandler.Error("Unhandled case : nTimes != 1 or 2");
                return;
            }

            // Repeat the action nTimes
            for (int i = 0; i < nTimes; i++)
            {
                // Retrieve stage level
                if (!up)
                {
                    // stage level is 0 : do nothing
                    if (SoloArenas[arenaType].CurrentStage == 0)
                        break;

                    hasChanged = true;
                    SetArenaData(arenaType, SoloArenas[arenaType].CurrentLevel, SoloArenas[arenaType].CurrentStage - 1, SoloArenas[arenaType].CurrentDifficulty, false);
                }

                else
                {
                    hasChanged = true;
                    ArenaData arenaData = AssetLoader.LoadArenaData(arenaType, SoloArenas[arenaType].CurrentDifficulty);

                    // ADD level 
                    if (SoloArenas[arenaType].CurrentStage == arenaData.CurrentArenaLevelData.StageData.Count - 1)
                        UpgradeArenaLevel(arenaType, false);
                    // ADD stage
                    else
                        SetArenaData(arenaType, SoloArenas[arenaType].CurrentLevel, SoloArenas[arenaType].CurrentStage + 1, SoloArenas[arenaType].CurrentDifficulty, false);
                }
            }

            // SAVE
            if (save && hasChanged)
                Instance.SaveValue(KEY_SOLO_ARENAS);
        }

        public static void UpgradeArenaLevel(EArenaType arenaType, bool save = true)
        {
            SArenaCloudData arenaCloudData = SoloArenas[arenaType];
            ArenaData arenaData = AssetLoader.LoadArenaData(arenaType, arenaCloudData.CurrentDifficulty);

            if (arenaCloudData.CurrentLevel >= arenaData.ArenaLevelData.Count)
                return;

            // add rewards to notification data so they can be collected later
            NotificationCloudData.AddArenaReward(arenaType, arenaCloudData.CurrentLevel);

            SetArenaData(arenaType, arenaCloudData.CurrentLevel + 1, 0, SoloArenas[arenaType].CurrentDifficulty, save);
        }

        /// <summary>
        /// Upgrade the difficulty level of an arena on completion 
        /// </summary>
        /// <param name="arenaType"></param>
        /// <param name="save"></param>
        public static void UpgradeArenaDifficulty(EArenaType arenaType, bool save = true)
        {
            if (SoloArenas[arenaType].CurrentDifficulty >= MaxArenaDifficulty)
                return;

            SetArenaData(arenaType, 0, 0, SoloArenas[arenaType].CurrentDifficulty + 1, save);
        }

        public static void SetArenaData(EArenaType arenaType, int level, int stage, EArenaDifficulty difficulty, bool save = true)
        {
            SoloArenas[arenaType] = new SArenaCloudData(level, stage, difficulty);

            if (save)
                Instance.SaveValue(KEY_SOLO_ARENAS);

            ArenaDataChangedEvent?.Invoke(arenaType);
        }

        #endregion


        #region Default Data

        public override void Reset(string key)
        {
            base.Reset(key);

            switch (key)
            {
                case KEY_LEAGUE:
                    m_Data[key] = new SLeagueCloudData(ELeague.Iron);
                    SaveValue(key);
                    break;
                
                case KEY_SOLO_ARENAS:
                    foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType))) 
                    {
                        SetArenaData(arenaType, 0, 0, EArenaDifficulty.Normal, false);
                    }

                    break;
            }
        }

        #endregion


        #region Checkers

        void CheckLeague()
        {
            if (ProgressionCloudData.CurrentLeague == ELeague.None)
                Reset(KEY_LEAGUE);
        }

        void CheckArenaData()
        {
            if (SoloArenas.Count == 0)
                Reset(KEY_SOLO_ARENAS);

            foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
            {
                if (! SoloArenas.ContainsKey(arenaType))
                {
                    ErrorHandler.Error($"Missing arena in arena {arenaType} data : instantiating new one with default values");
                    SoloArenas[arenaType] = new SArenaCloudData();
                }
            }
        }

        #endregion


        #region Listeners

        protected override void OnCloudDataKeyLoaded(string key)
        {
            base.OnCloudDataKeyLoaded(key);

            if (!m_Data.ContainsKey(key) || m_Data[key] == null)
            {
                ErrorHandler.Error("Missing data " + key + " in cloud data : reseting with default values");
                Reset(key);
                return;
            }

            switch (key)
            {
                case KEY_LEAGUE:
                    CheckLeague();
                    break;

                case KEY_SOLO_ARENAS:
                    CheckArenaData();
                    break;
            }
        }

        #endregion
    }
}