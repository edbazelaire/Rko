using Assets;
using Data.GameManagement;
using Enums;
using Game.AI;
using Menu.MainMenu;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Services.CloudSave.Models;

namespace Save
{
    [Serializable]
    public struct SArenaCloudData
    {
        public int CurrentLevel;
        public int CurrentStage;

        public SArenaCloudData(int level = 0, int stage = 0)
        {
            CurrentLevel = level;
            CurrentStage = stage;
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
        public const string KEY_LEAGUE         = "League";
        public const string KEY_SOLO_ARENAS     = "SoloArenas";

        // ===============================================================================================
        // ACTIONS
        public static Action LeagueDataChangedEvent;
        public static Action ArenaDataChangedEvent;

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

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert SCharacterBuildsCloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <returns></returns>
        protected override object Convert(Item item)
        {
            try
            {
                if (m_Data[item.Key].GetType() == typeof(Dictionary<EArenaType, SArenaCloudData>))
                    return item.Value.GetAs<Dictionary<EArenaType, SArenaCloudData>>();

                if (m_Data[item.Key].GetType() == typeof(SLeagueCloudData))
                    return item.Value.GetAs<SLeagueCloudData>();
            } catch (Exception ex) 
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

        public static void UpdateLeagueValue(bool up)
        {
            var leagueCloudData = LeagueCloudData;

            // Retrieve stage level
            if (!up)
            {
                // stage level is 0 : do nothing
                if (leagueCloudData.CurrentStage == 0)
                    return;

                leagueCloudData.CurrentStage--;
            }

            else
            {
                SLeagueData leagueData = Main.LeagueDataConfig.CurrentLeagueData;

                // ADD stage level
                if (leagueCloudData.CurrentStage == leagueData.LevelData[CurrentLeagueLevel].NStages)
                {
                    UpgradeLeagueLevel();
                    return;
                }

                leagueCloudData.CurrentStage++;
            }

            SaveLeagueData(leagueCloudData);
        }

        public static void UpgradeLeagueLevel()
        {
            // reached max level of the league : go to next league 
            if (CurrentLeagueLevel == Main.LeagueDataConfig.CurrentLeagueData.LevelData.Count - 1)
            {
                UpgradeLeague();
                return;
            }

            // add rewards to notification data so they can be collected later
            NotificationCloudData.AddLeagueLevelReward(CurrentLeague, CurrentLeagueLevel);

            SLeagueCloudData leagueCloudData = LeagueCloudData;
            leagueCloudData.CurrentStage = 0;
            leagueCloudData.CurrentLevel++;
            SaveLeagueData(leagueCloudData);
        }

        public static void UpgradeLeague()
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

        public static bool IsArenaCompleted(EArenaType arenaType)
        {
            return SoloArenas[arenaType].CurrentLevel >= AssetLoader.LoadArenaData(arenaType).MaxLevel;
        }

        public static void UpdateStageValue(EArenaType arenaType, bool up)
        {
            SArenaCloudData arenaCloudData = SoloArenas[arenaType];

            // Retrieve stage level
            if (! up)
            {
                // stage level is 0 : do nothing
                if (arenaCloudData.CurrentStage == 0)
                    return;

                arenaCloudData.CurrentStage--;
            } 
            
            else
            {
                ArenaData arenaData = AssetLoader.LoadArenaData(arenaType);

                // ADD stage level
                if (arenaCloudData.CurrentStage == arenaData.CurrentArenaLevelData.StageData.Count - 1)
                {
                    UpgradeArenaLevel(arenaType);
                    return;
                }

                arenaCloudData.CurrentStage++;
            }

            SoloArenas[arenaType] = arenaCloudData;
            Instance.SaveValue(KEY_SOLO_ARENAS);
        }

        public static void UpgradeArenaLevel(EArenaType arenaType)
        {
            SArenaCloudData arenaCloudData = SoloArenas[arenaType];
            ArenaData arenaData = AssetLoader.LoadArenaData(arenaType);

            if (arenaCloudData.CurrentLevel >= arenaData.ArenaLevelData.Count)
                return;

            // add rewards to notification data so they can be collected later
            NotificationCloudData.AddArenaReward(arenaType, arenaCloudData.CurrentLevel);

            SetArenaData(arenaType, arenaCloudData.CurrentLevel + 1, 0);
        }

        public static void SetArenaData(EArenaType arenaType, int level, int stage, bool save = true)
        {
            SoloArenas[arenaType] = new SArenaCloudData(level, stage);

            if (save)
                Instance.SaveValue(KEY_SOLO_ARENAS);

            ArenaDataChangedEvent?.Invoke();
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
                        SetArenaData(arenaType, 0, 0, false);
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