using Assets;
using Assets.Scripts.Data.GameManagement;
using Data.GameManagement;
using Enums;
using Inventory;
using Managers;
using MyBox;
using Save.Data.Progression.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Save
{
    public class ProgressionCloudData : CloudData
    {
        #region Members

        public new static ProgressionCloudData Instance => CloudSaveManager.Instance.GetCloudData(typeof(ProgressionCloudData)) as ProgressionCloudData;

        // ===============================================================================================
        // CONSTANTS
        public const string KEY_LEAGUE                  = "League";
        public const string KEY_CURRENT_ARENA           = "CurrentArena";
        public const string KEY_UNLOCKED_ARENAS         = "UnlockedArenas";
        public const string KEY_UNLOCKED_ARENA_REWARDS  = "UnlockedArenaRewards";

        public static string[] DEFAULT_POWER_UPS => new string[4] { "", "", "", "" };

        // ===============================================================================================
        // ACTIONS
        public static Action        LeagueDataChangedEvent;
        public static Action        CurrentArenaDataChangedEvent;
        public static Action        UnlockingArenaDifficultyEvent;
        public static Action<bool>  CurrentArenaEndedEvent;

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_LEAGUE,                   new SLeagueCloudData(ELeague.Iron)                  },
            { KEY_CURRENT_ARENA,            new SCurrentArenaCloudData(EArenaType.None)         },
            { KEY_UNLOCKED_ARENAS,          new Dictionary<EArenaType, EArenaDifficulty>()      },
            { KEY_UNLOCKED_ARENA_REWARDS,   new Dictionary<EArenaType, SArenaPosition>()        },
        };

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        public static SLeagueCloudData                              LeagueCloudData         => (SLeagueCloudData)Instance.m_Data[KEY_LEAGUE];
        public static ELeague                                       CurrentLeague           => LeagueCloudData.CurrentLeague;
        public static int                                           CurrentLeagueLevel      => LeagueCloudData.CurrentLevel;
        public static int                                           CurrentLeagueStage      => LeagueCloudData.CurrentStage;
        public static SCurrentArenaCloudData                        CurrentArena            => (SCurrentArenaCloudData)Instance.m_Data[KEY_CURRENT_ARENA];
        public static SPowerOrb                                     CurrentArenaReward      => CurrentArena.GetPowerOrb();
        public static bool                                          HasArenaInProgress      => CurrentArena.InProgress();
        public static Dictionary<EArenaType, EArenaDifficulty>      UnlockedArenas          => Instance.m_Data[KEY_UNLOCKED_ARENAS] as Dictionary<EArenaType, EArenaDifficulty>;
        public static Dictionary<EArenaType, SArenaPosition>        UnlockedArenaRewards    => Instance.m_Data[KEY_UNLOCKED_ARENA_REWARDS] as Dictionary<EArenaType, SArenaPosition>;
        public static EArenaDifficulty                              MaxArenaDifficulty      => ((EArenaDifficulty[])Enum.GetValues(typeof(EArenaDifficulty))).Last();

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

                if (itemType == typeof(Dictionary<EArenaType, EArenaDifficulty>))
                {
                    return item.Value.GetAs<Dictionary<EArenaType, EArenaDifficulty>>();
                }

                if (itemType == typeof(Dictionary<EArenaType, SArenaPosition>))
                {
                    return item.Value.GetAs<Dictionary<EArenaType, SArenaPosition>>();
                }

                if (itemType == typeof(SCurrentArenaCloudData))
                {
                    return item.Value.GetAs<SCurrentArenaCloudData>();
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
                Reset(item.Key);
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
            // debug tool
            if (Main.StopPreventiveLoss)
                return false;

            switch (gameMode)
            {
                case EGameMode.Arena:
                    AddArenaLoss();
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

                    leagueCloudData.CurrentStage--;
                }

                else
                {
                    SLeagueData leagueData = Main.LeagueDataConfig.CurrentLeagueData;

                    // ADD stage level
                    if (leagueCloudData.CurrentStage == leagueData.LevelData[CurrentLeagueLevel].NStages)
                    {
                        UpgradeLeagueLevel(false);
                        continue;
                    }
                    
                    leagueCloudData.CurrentStage++;
                }

                // update league data (locally)
                SetLeagueData(leagueCloudData, false);
            }

            // save after each modifications is completed
            if (save)
                Instance.SaveValue(KEY_LEAGUE);
        }

        public static void UpgradeLeagueLevel(bool save = true)
        {
            // add rewards to notification data so they can be collected later
            NotificationCloudData.AddLeagueLevelReward(CurrentLeague, CurrentLeagueLevel);

            // reached max level of the league : go to next league 
            if (CurrentLeagueLevel == Main.LeagueDataConfig.CurrentLeagueData.LevelData.Count - 1)
            {
                UpgradeLeague(save);
                return;
            }

            SLeagueCloudData leagueCloudData = LeagueCloudData;
            leagueCloudData.CurrentStage = 0;
            leagueCloudData.CurrentLevel++;

            SetLeagueData(leagueCloudData, save);
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

            SetLeagueData(leagueCloudData, save);

            // add chest open boost on unlocking new league
            TimeCloudData.AddBoost(EBoost.ChestSpeedBoost, 48 * 3600);
        }

        public static void SetLeagueData(SLeagueCloudData leagueCloudData, bool save = true)
        {
            Instance.m_Data[KEY_LEAGUE] = leagueCloudData;

            if (save)
                Instance.SaveValue(KEY_LEAGUE);

            LeagueDataChangedEvent?.Invoke();
        }

        #endregion


        #region Arena Data

        public static EArenaDifficulty GetUnlockedArenaDifficulty(EArenaType arenaType, bool clamp = false)
        {
            if (! UnlockedArenas.ContainsKey(arenaType))
            {
                ErrorHandler.Warning("Unable to find arena (" + arenaType + ") in Unlocked Arenas Cloud Data - adding it manually");
                UnlockedArenas[arenaType] = EArenaDifficulty.Easy;

                Instance.SaveValue(KEY_UNLOCKED_ARENAS);
            }

            // check if we need to get the "clamped" value
            if (clamp && UnlockedArenas[arenaType] >= MaxArenaDifficulty)
                return MaxArenaDifficulty;

            return UnlockedArenas[arenaType];
        }

        public static bool IsUnlocked(EArenaType arenaType, EArenaDifficulty arenaDifficulty)
        {
            return UnlockedArenas.ContainsKey(arenaType) && UnlockedArenas[arenaType] >= arenaDifficulty;
        }

        public static bool IsMaxUnlocked(EArenaType arenaType, EArenaDifficulty arenaDifficulty)
        {
            return UnlockedArenas.ContainsKey(arenaType) && UnlockedArenas[arenaType] == arenaDifficulty;
        }

        public static bool IsCompleted(EArenaType arenaType, EArenaDifficulty arenaDifficulty)
        {
            // no data at all
            if (!UnlockedArenas.ContainsKey(arenaType))
                return false;

            // not unlocked difficulty
            if (UnlockedArenas[arenaType] < arenaDifficulty)
                return false;

            // is below max unlocked difficulty
            if (UnlockedArenas[arenaType] > arenaDifficulty)
                return true;

            // check is last arena and all reward of that arena has been collected
            return arenaDifficulty == ProgressionCloudData.MaxArenaDifficulty && UnlockedArenaRewards.ContainsKey(arenaType) && UnlockedArenaRewards[arenaType] >= new SArenaPosition(arenaDifficulty, 4, 2);
        }

        public static SArenaPosition GetUnlockedArenaReward(EArenaType arenaType)
        {
            if (! UnlockedArenaRewards.ContainsKey(arenaType))
            {
                ErrorHandler.Warning("Unable to find arena (" + arenaType + ") in UnlockedArenaRewards Cloud Data - adding it manually");
                UnlockedArenaRewards[arenaType] = new SArenaPosition();
                Instance.SaveValue(KEY_UNLOCKED_ARENA_REWARDS);
            }

            return UnlockedArenaRewards[arenaType];
        }

        public static void AddArenaWin(int nWins = 1, bool save = true)
        {
            // Repeat the action nTimes
            for (int i = 0; i < nWins; i++)
            {
                // ADD level 
                if (CurrentArena.IsMaxStage())
                    UpgradeCurrentArenaLevel(false);

                // ADD stage
                else
                    UpdateCurrentArena(stage: CurrentArena.Stage + 1);
            }

            // SAVE
            if (save)
                Instance.SaveValue(KEY_CURRENT_ARENA);
        }

        public static void AddArenaLoss(int nLoss = 1, bool save = true)
        {
            UpdateCurrentArena(losses: Math.Clamp(CurrentArena.Losses + nLoss, 0, CurrentArena.GetMaxLosses()), erosion: 0f, save: save);
        }

        public static void AddCurrentArenaPowerUp(string powerUpName, bool save = true)
        {
            SetCurrentArenaPowerUp(powerUpName, CurrentArena.Level - 1 , save);
        }

        public static void SetCurrentArenaBuild(SBuildData buildData, bool save = true)
        {
            var currentArena = CurrentArena;
            currentArena.SetBuildData(buildData);

            Instance.m_Data[KEY_CURRENT_ARENA] = currentArena;

            // fire event that current arena data have been changed
            CurrentArenaDataChangedEvent?.Invoke();

            if (save)
                Instance.SaveValue(KEY_CURRENT_ARENA);
        }

        public static void SetCurrentArenaBuildValue(Enum value, int level, int index, bool save = true)
        {
            var currentArena = CurrentArena;
            currentArena.SetBuildValue(value, level, index);

            Instance.m_Data[KEY_CURRENT_ARENA] = currentArena;

            // fire event that current arena data have been changed
            CurrentArenaDataChangedEvent?.Invoke();

            if (save)
                Instance.SaveValue(KEY_CURRENT_ARENA);
        }

        public static void SetCurrentArenaMods(List<EArenaMod> arenaMods, bool save = true)
        {
            var currentArena = CurrentArena;
            currentArena.ArenaMods = arenaMods;

            Instance.m_Data[KEY_CURRENT_ARENA] = currentArena;

            // fire event that current arena data have been changed
            CurrentArenaDataChangedEvent?.Invoke();

            if (save)
                Instance.SaveValue(KEY_CURRENT_ARENA);
        }

        public static void SetCurrentArenaPowerUp(string powerUpName, int index, bool save = true)
        {
            var currentArena = CurrentArena;

            if (index < 0)
            {
                ErrorHandler.Error("Bad index PowerUpData index : " + index);
                return;
            }

            if (CurrentArena.PowerUps == null)
            {
                ErrorHandler.Error("No PowerUpData provided to CurrentArena");
                currentArena.PowerUps = DEFAULT_POWER_UPS;
            }

            if (index >= currentArena.PowerUps.Length)
            {
                ErrorHandler.Error("Bad index PowerUpData index (" + index + ") : PowerUp max length is " + CurrentArena.PowerUps.Length);
                return;
            }

            Debug.Log("SETTING PowerUp (" + powerUpName + ") at index " + index);

            currentArena.PowerUps[index] = powerUpName;
            Instance.m_Data[KEY_CURRENT_ARENA] = currentArena;

            // fire event that current arena data have been changed
            CurrentArenaDataChangedEvent?.Invoke();

            if (save)
                Instance.SaveValue(KEY_CURRENT_ARENA);
        }

        public static void UpgradeCurrentArenaLevel(bool save = true)
        {
            if (CurrentArena.IsMaxArenaLevel())
            {
                // call event that the current arena is over
                CurrentArenaEndedEvent?.Invoke(true);

                if (CurrentArena.GetArenaDifficulty() == UnlockedArenas[CurrentArena.ArenaType])
                    UnlockNextArenaDifficulty(CurrentArena.ArenaType, save: true);
            } 

            UpdateCurrentArena(stage: 0, level: CurrentArena.Level + 1, save: save);
        }

        /// <summary>
        /// Upgrade the difficulty level of an arena on completion 
        /// </summary>
        /// <param name="arenaType"></param>
        /// <param name="save"></param>
        public static void UnlockNextArenaDifficulty(EArenaType arenaType, bool save = true)
        {
            // unlock next difficulty
            UnlockedArenas[arenaType]++;

            // notify the player
            NotificationCloudData.AddUnlockedArena(arenaType);

            // set unlocked arena as currently selected difficulty
            PlayerPrefsHandler.SetArenaDifficulty(arenaType, UnlockedArenas[arenaType]);

            if (save)
                Instance.SaveValue(KEY_UNLOCKED_ARENAS);
        }

        /// <summary>
        /// Check if rewards for provided arena level has already been collected
        /// </summary>
        /// <param name="arenaType"></param>
        /// <param name="arenaDifficulty"></param>
        /// <param name="arenaLevel"></param>
        /// <returns></returns>
        public static bool IsArenaRewardCollected(EArenaType arenaType, EArenaDifficulty arenaDifficulty, int arenaLevel)
        {
            // check that current level is above unlocked level (if same difficulty)
            if (new SArenaPosition(arenaDifficulty, arenaLevel) <= GetUnlockedArenaReward(arenaType))
                return true;

            return false;
        }

        /// <summary>
        /// Update arena unlocked rewards to match current arena data
        /// </summary>
        /// <param name="save"></param>
        public static void UpdateArenaUnlockedRewards(bool save = true)
        {
            var unlockedArenaReward = UnlockedArenaRewards[CurrentArena.ArenaType];

            if (CurrentArena.Level - 1 < 0)
                return;

            // check if was already collected
            if (IsArenaRewardCollected(CurrentArena.ArenaType, CurrentArena.GetArenaDifficulty(), CurrentArena.Level - 1))
                return;

            // update unlocked values with current values
            SetArenaUnlockedReward(CurrentArena.ArenaType, CurrentArena.GetArenaDifficulty(), CurrentArena.Level, save);
        }

        public static void SetArenaUnlockedReward(EArenaType arenaType, EArenaDifficulty arenaDifficulty, int level, bool save = true)
        {
            UnlockedArenaRewards[arenaType] = new SArenaPosition(arenaDifficulty, level);
            if (save)
                Instance.SaveValue(KEY_UNLOCKED_ARENA_REWARDS);
        }

        /// <summary>
        /// Create a new run for the Arena mode
        /// </summary>
        /// <param name="arenaType"></param>
        /// <param name="arenaDifficulty"></param>
        public static void CreateNewCurrentArena(EArenaType arenaType, SArenaDifficulty arenaDifficulty, List<EArenaMod> arenaMods, SBuildData buildData)
        {
            Instance.SetData(KEY_CURRENT_ARENA, new SCurrentArenaCloudData(
                arenaType,
                arenaDifficulty:    arenaDifficulty,
                arenaMods:          arenaMods,
                buildData:          buildData,
                maxLifes:           CalculateArenaMaxLifes(arenaMods),
                rewardRarety:       CalculateArenaRarety(arenaMods)
            ));

            CurrentArenaDataChangedEvent?.Invoke();
        }

        /// <summary>
        /// Reset current arena run
        /// </summary>
        /// <param name="arenaType"></param>
        public static void ResetCurrentArena()
        {
            Instance.Reset(KEY_CURRENT_ARENA);
            CurrentArenaDataChangedEvent?.Invoke();
        }

        /// <summary>
        /// Set current arena to be over
        /// </summary>
        /// <param name="save"></param>
        public static void EndCurrentArena()
        {
            var currentArena = CurrentArena;
            currentArena.SetIsOver(true);

            Instance.SetData(KEY_CURRENT_ARENA, currentArena);
            CurrentArenaDataChangedEvent?.Invoke();
        }

        public static void UpdateCurrentArena(int? level = null, int? stage = null, int? losses = null, float? erosion = null, bool save = true)
        {
            var currentArena = CurrentArena;
            if (level.HasValue)
                currentArena.Level = level.Value;
            if (stage.HasValue)
                currentArena.Stage = stage.Value;
            if (losses.HasValue)
                currentArena.Losses = losses.Value > currentArena.GetMaxLosses() ? currentArena.GetMaxLosses() : losses.Value;
            if (erosion.HasValue)
                currentArena.Erosion = erosion.Value;

            // check that provided values are consistant 
            currentArena.CheckArenaData();

            // save
            Instance.SetData(KEY_CURRENT_ARENA, currentArena, save);
            CurrentArenaDataChangedEvent?.Invoke();
        }

        public static void AddCurrentArenaPowerOrbReward(int power, ERarety? rarety = null, bool save = true)
        {
            var currentArena = CurrentArena;
            currentArena.RewardPower += power;

            if (rarety.HasValue)
                currentArena.RewardRarety = rarety.Value;

            Instance.SetData(KEY_CURRENT_ARENA, currentArena, save);
            CurrentArenaDataChangedEvent?.Invoke();
        }

        public static void AddCurrentArenaRefreshToken(int nTokens, bool save = true)
        {
            var currentArena = CurrentArena;
            currentArena.RefreshTokens += nTokens;
            if (currentArena.RefreshTokens < 0)
            {
                ErrorHandler.Warning("Trying to set currentArena.RefreshTokens with value < 0 : " + currentArena.RefreshTokens);
                currentArena.RefreshTokens = 0;
            }

            Instance.SetData(KEY_CURRENT_ARENA, currentArena, save);
        }

        #endregion


        #region Current Arena Mods

        public static ERarety CalculateArenaRarety(List<EArenaMod> arenaMods)
        {
            return arenaMods.Contains(EArenaMod.Random) ? ERarety.Rare : ERarety.Common;
        }

        public static int CalculateArenaMaxLifes(List<EArenaMod> arenaMods = null)
        {
            if (arenaMods == null)
                return CurrentArena.HasMod(EArenaMod.NoDeath) ? 1 : 3;
            return arenaMods.Contains(EArenaMod.NoDeath) ? 1 : 3;
        }

        #endregion


        #region Default Data

        public override void Reset(string key, bool save = true)
        {
            base.Reset(key, save);

            switch (key)
            {
                case KEY_LEAGUE:
                    Instance.SetData(key, new SLeagueCloudData(ELeague.Iron));
                    break;
                
                case KEY_UNLOCKED_ARENAS:
                    var unlockedArenaData = new Dictionary<EArenaType, EArenaDifficulty>();
                    foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType))) 
                    {
                        unlockedArenaData.Add(arenaType, EArenaDifficulty.Easy);
                    }

                    // when reset unlocked arenas, reset current arena data
                    ResetCurrentArena();
                    Instance.SetData(key, unlockedArenaData);
                    break;

                case KEY_UNLOCKED_ARENA_REWARDS:
                    var data = new Dictionary<EArenaType, SArenaPosition>();
                    foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
                    {
                        data.Add(arenaType, new SArenaPosition(EArenaDifficulty.Easy, 0));
                    }
                    Instance.SetData(key, data);
                    break;

                case KEY_CURRENT_ARENA:
                    Instance.SetData(key, new SCurrentArenaCloudData(EArenaType.None));
                    break;
            }

            if (save)
                Instance.SaveValue(key);
        }

        #endregion


        #region Checkers

        protected override void CheckData() 
        {
            CheckLeague();
            CheckUnlockedArenaData();
            CheckUnlockedArenaRewardsData();
            CheckCurrentArenaData();
        }

        void CheckLeague()
        {
            if (CurrentLeague == ELeague.None)
                Reset(KEY_LEAGUE);
        }

        void CheckCurrentArenaData()
        {
            if (CurrentArena.GetArenaDifficulty() > MaxArenaDifficulty)
            {
                ErrorHandler.Error("Bad CurrentArena data : SArenaDifficulty > MaxArenaDifficulty - Reseting");
                Reset(KEY_CURRENT_ARENA);
                return;
            }

            bool save = false;
            var currentArena = CurrentArena;

            if (CurrentArena.ArenaType == EArenaType.None)
                return;

            if (! UnlockedArenas.ContainsKey(CurrentArena.ArenaType))
            {
                ErrorHandler.Error($"CurrentArena ("+ CurrentArena.ArenaType + ") is not unlocked - reseting data");
                ResetCurrentArena();
                return;
            }

            if (CurrentArena.SArenaDifficulty.Difficulty > UnlockedArenas[CurrentArena.ArenaType])
            {
                ErrorHandler.Error($"CurrentArena data has difficulty ({UnlockedArenas[CurrentArena.ArenaType]}) - reseting data");
                PlayerPrefsHandler.SetArenaDifficulty(currentArena.ArenaType, UnlockedArenas[currentArena.ArenaType]);
                ResetCurrentArena();
                return;
            }

            if (CurrentArena.SArenaDifficulty.Level < 0)
            {
                ErrorHandler.Error($"CurrentArena data has level ({CurrentArena.SArenaDifficulty.Level}) < 0");
                currentArena.SArenaDifficulty.Level = 0;
                save = true;
            }

            if (currentArena.SArenaDifficulty.Level > ArenaManagementData.NDifficultyLevels)
            {
                ErrorHandler.Error($"CurrentArena data : has level ({currentArena.SArenaDifficulty.Level}) > " + ArenaManagementData.NDifficultyLevels);
                currentArena.SArenaDifficulty.Level = ArenaManagementData.NDifficultyLevels;
                save = true;
            }

            if (! currentArena.CheckPowerUps())
                save = true;

            if (! currentArena.CheckMods())
                save = true;

            if (currentArena.HasBuildData() && ! currentArena.BuildData.Check())
            {
                ResetCurrentArena();
                return;
            }

            if (save)
                Instance.SetData(KEY_CURRENT_ARENA, currentArena);
        }

        void CheckUnlockedArenaData()
        {
            bool save = false;

            if (UnlockedArenas.IsNullOrEmpty())
            {
                Reset(KEY_UNLOCKED_ARENAS);
                return;
            }

            if (UnlockedArenas.ContainsKey(EArenaType.None))
            {
                ErrorHandler.Warning($"EArenaType.None was found in UnlockedArenas - Removed");
                UnlockedArenas.Remove(EArenaType.None);
                save = true;
            }

            foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
            {
                if (arenaType == EArenaType.None)
                    continue;

                if (! UnlockedArenas.ContainsKey(arenaType))
                {
                    ErrorHandler.Error($"Missing arena in arena {arenaType} data : instantiating new one with default values");
                    UnlockedArenas[arenaType] = EArenaDifficulty.Easy;
                    save = true;
                }
                
                if (UnlockedArenas[arenaType] > MaxArenaDifficulty)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has difficulty ({UnlockedArenas[arenaType]}) > MaxArenaDifficulty " + MaxArenaDifficulty);
                    UnlockedArenas[arenaType] = MaxArenaDifficulty;
                    save = true;
                }
            }

            if (save)
                Instance.SaveValue(KEY_UNLOCKED_ARENAS);
        }

        void CheckUnlockedArenaRewardsData()
        {
            bool save = false;

            if (UnlockedArenaRewards.ContainsKey(EArenaType.None))
            {
                ErrorHandler.Warning($"EArenaType.None was found in UnlockedArenas - Removed");
                UnlockedArenas.Remove(EArenaType.None);
                save = true;
            }

            if (UnlockedArenaRewards.Count == 0)
            {
                Reset(KEY_UNLOCKED_ARENA_REWARDS);
                return;
            }

            foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
            {
                if (arenaType == EArenaType.None)
                    continue;

                if (!UnlockedArenaRewards.ContainsKey(arenaType))
                {
                    ErrorHandler.Error($"Missing arena in arena {arenaType} data : instantiating new one with default values");
                    UnlockedArenaRewards[arenaType] = new SArenaPosition();
                    save = true;
                }

                if (UnlockedArenaRewards[arenaType].ArenaDifficulty > MaxArenaDifficulty)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has difficulty ({UnlockedArenaRewards[arenaType]}) > " + MaxArenaDifficulty);
                    UnlockedArenaRewards[arenaType] = new SArenaPosition(MaxArenaDifficulty, ArenaManagementData.MaxLevels);
                    save = true;
                }
            }

            if (save)
                Instance.SaveValue(KEY_UNLOCKED_ARENAS);
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
        }

        #endregion
    }
}