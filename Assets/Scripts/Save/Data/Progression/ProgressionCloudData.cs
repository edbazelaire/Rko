using Assets;
using Assets.Scripts.Data.GameManagement;
using Data;
using Data.GameManagement;
using Enums;
using Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Save
{
    [Serializable]
    public struct SCurrentArenaCloudData
    {
        public EArenaType           ArenaType;
        public SArenaDifficulty     SArenaDifficulty;

        public int              Level;
        public int              Stage;
        public int              Losses;
        public float            Erosion;
        public string[]         PowerUps;
        public int              RewardPower;
        public ERarety          RewardRarety;
        private bool            m_IsOver;

        public readonly EArenaDifficulty GetArenaDifficulty() => SArenaDifficulty.Difficulty;
        public readonly bool InProgress() => ArenaType != EArenaType.None;
        public readonly bool IsOver() => m_IsOver || Losses >= ArenaData.MAX_LOSSES || Level > AssetLoader.LoadArenaData(ArenaType, SArenaDifficulty).MaxLevel;
        public readonly bool IsBoss() => Stage == AssetLoader.LoadArenaData(ArenaType, SArenaDifficulty).GetArenaLevelData(Level).StageData.Count - 1;

        public SCurrentArenaCloudData(EArenaType arenaType, SArenaDifficulty sArenaDifficulty = default, int level = 0, int stage = 0, int losses = 0, float erosion = 0f, string[] powerUps = default, int rewardPower = 0, ERarety rewardRarety = 0, bool isOver = false)
        {
            if (losses < 0)
            {
                ErrorHandler.Error("Bad current losses value : " + losses);
                losses = 0;
            }

            ArenaType               = arenaType;
            SArenaDifficulty        = sArenaDifficulty;
            Level                   = level;
            Stage                   = stage;
            Losses                  = losses;
            Erosion                 = erosion;
            PowerUps                = powerUps;
            RewardPower             = rewardPower;
            RewardRarety            = rewardRarety;
            m_IsOver                = isOver;
        }

        #region Rewards

        public SPowerOrb GetPowerOrb()
        {
            return new SPowerOrb(RewardPower, RewardRarety);
        }

        #endregion

        public ArenaData LoadArenaData()
        {
            return AssetLoader.LoadArenaData(ArenaType, SArenaDifficulty);
        }

        public void SetIsOver(bool isOver)
        {
            m_IsOver = isOver;
        }

        public List<string> GetActivePowerUps()
        {
            // security check
            if (PowerUps == null || PowerUps.Length == 0)
                return new List<string>();

            var powerUpList = PowerUps.ToList().Where(powerUpName => powerUpName != "");

            // check that list is not null
            if (powerUpList == null)
                return new List<string>();

            // return as list
            return powerUpList.ToList();
        }

        public bool CheckPowerUps()
        {
            var test = true;

            // =====================================================================================
            // check is default
            if (PowerUps == default)
            {
                PowerUps = ProgressionCloudData.DEFAULT_POWER_UPS;
                return false;
            }

            // =====================================================================================
            // check LENGTH
            if (PowerUps.Length != ProgressionCloudData.DEFAULT_POWER_UPS.Length)
            {
                // set default values
                string[] basePowerUps = ProgressionCloudData.DEFAULT_POWER_UPS;

                for (int i = 0; i < ProgressionCloudData.DEFAULT_POWER_UPS.Length; i++)
                {
                    if (PowerUps.Length <= i)
                        break;

                    // overwritte with previous data if possible
                    basePowerUps[i] = PowerUps[i];
                }

                // set powerUps as overriten default powerUps
                PowerUps = basePowerUps;
                test = false;
            }

            // =====================================================================================
            // check EXISTS
            for (int i=0; i < PowerUps.Length; i++)
            {
                string powerUpName = PowerUps[i];

                if (PowerUps[i] == "")
                    continue;

                // if no longer exists : reset value
                if (! SRunePower.TrySplitPowerUpName(powerUpName, out string _, out ERuneActivation _))
                {
                    PowerUps[i] = "";
                    test = false;
                }
            }

            return test;
        }
    }

    [Serializable]
    public struct SArenaDifficulty
    {
        public EArenaDifficulty     Difficulty;
        public int                  Level;

        public SArenaDifficulty(EArenaDifficulty difficulty = 0, int level = 1)
        {
            Difficulty = difficulty;
            Level = level;
        }

        public override string ToString() => Difficulty + " " + new string('+', Level);

        public SArenaDifficulty FromString(string str)
        {
            var splits = str.Split(" ");
            if (splits.Length != 2)
            {
                ErrorHandler.Error("Bad SArenaDifficulty string : " + str);
                return this;
            }

            if (!Enum.TryParse(splits[0], out Difficulty))
            {
                ErrorHandler.Error("Bad SArenaDifficulty string - unable to parse difficulty : " + str);
                return this;
            }

            Level = splits[1].Count(f => f == '+'); ;

            return this;
        }

        // Overload the "==" operator
        public static bool operator ==(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty == rhs.Difficulty && lhs.Level == rhs.Level;
        }

        // Overload the "!=" operator
        public static bool operator !=(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty != rhs.Difficulty && lhs.Level != rhs.Level;
        }

        // Overload the ">=" operator
        public static bool operator >=(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty > rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level >= rhs.Level);
        }

        // Overload the "<=" operator
        public static bool operator <=(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty < rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level <= rhs.Level);
        }

        // Overload the ">" operator
        public static bool operator >(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty > rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level > rhs.Level);
        }

        // Overload the "<" operator
        public static bool operator <(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty < rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level < rhs.Level);
        }

        // You also need to override Equals() and GetHashCode() when overloading == and !=
        public override bool Equals(object obj)
        {
            if (!(obj is SArenaDifficulty))
            {
                return false;
            }
            var other = (SArenaDifficulty)obj;
            return this.Difficulty == other.Difficulty && this.Level == other.Level;
        }

        public override int GetHashCode()
        {
            return Difficulty.GetHashCode() + Level.GetHashCode();
        }
    }

    [Serializable]
    public struct SUnlockedArenaReward
    {
        public SArenaDifficulty ArenaDifficulty;
        public int ArenaLevel;

        public SUnlockedArenaReward(SArenaDifficulty arenaDifficulty, int arenaLevel = -1)
        {
            ArenaDifficulty = arenaDifficulty != null ? arenaDifficulty : new SArenaDifficulty(EArenaDifficulty.Easy, 0);
            ArenaLevel = arenaLevel;
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
        public const string KEY_LEAGUE                  = "League";
        public const string KEY_CURRENT_ARENA           = "CurrentArena";
        public const string KEY_UNLOCKED_ARENAS         = "UnlockedArenas";
        public const string KEY_UNLOCKED_ARENA_REWARDS  = "UnlockedArenaRewards";

        public static string[] DEFAULT_POWER_UPS => new string[4] { "", "", "", "" };

        // ===============================================================================================
        // ACTIONS
        public static Action LeagueDataChangedEvent;
        public static Action CurrentArenaDataChangedEvent;
        public static Action UnlockingArenaDifficultyEvent;

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_LEAGUE,                   new SLeagueCloudData(ELeague.Iron)                  },
            { KEY_CURRENT_ARENA,            new SCurrentArenaCloudData(EArenaType.None)         },
            { KEY_UNLOCKED_ARENAS,          new Dictionary<EArenaType, SArenaDifficulty>()      },
            { KEY_UNLOCKED_ARENA_REWARDS,   new Dictionary<EArenaType, SUnlockedArenaReward>()  },
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
        public static Dictionary<EArenaType, SArenaDifficulty>      UnlockedArenas          => Instance.m_Data[KEY_UNLOCKED_ARENAS] as Dictionary<EArenaType, SArenaDifficulty>;
        public static Dictionary<EArenaType, SUnlockedArenaReward>  UnlockedArenaRewards    => Instance.m_Data[KEY_UNLOCKED_ARENA_REWARDS] as Dictionary<EArenaType, SUnlockedArenaReward>;
        public static SArenaDifficulty                              MaxArenaDifficulty      => new SArenaDifficulty(((EArenaDifficulty[])Enum.GetValues(typeof(EArenaDifficulty))).Last(), ArenaManagementData.NDifficultyLevels - 1);

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

                if (itemType == typeof(Dictionary<EArenaType, SArenaDifficulty>))
                {
                    return item.Value.GetAs<Dictionary<EArenaType, SArenaDifficulty>>();
                }

                if (itemType == typeof(Dictionary<EArenaType, SUnlockedArenaReward>))
                {
                    return item.Value.GetAs<Dictionary<EArenaType, SUnlockedArenaReward>>();
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

        public static SArenaDifficulty GetUnlockedArenaDifficulty(EArenaType arenaType)
        {
            if (! UnlockedArenas.ContainsKey(arenaType))
            {
                ErrorHandler.Warning("Unable to find arena (" + arenaType + ") in Unlocked Arenas Cloud Data - adding it manually");
                UnlockedArenas[arenaType] = new SArenaDifficulty(0, 1);

                Instance.SaveValue(KEY_UNLOCKED_ARENAS);
            }

            return UnlockedArenas[arenaType];
        }

        public static SUnlockedArenaReward GetUnlockedArenaReward(EArenaType arenaType)
        {
            if (! UnlockedArenaRewards.ContainsKey(arenaType))
            {
                ErrorHandler.Warning("Unable to find arena (" + arenaType + ") in UnlockedArenaRewards Cloud Data - adding it manually");
                UnlockedArenaRewards[arenaType] = new SUnlockedArenaReward(new SArenaDifficulty(0, 0), -1);
                Instance.SaveValue(KEY_UNLOCKED_ARENA_REWARDS);
            }

            return UnlockedArenaRewards[arenaType];
        }

        public static void AddArenaWin(int nWins = 1, bool save = true)
        {
            // Repeat the action nTimes
            for (int i = 0; i < nWins; i++)
            {
                ArenaData arenaData = AssetLoader.LoadArenaData(CurrentArena.ArenaType, CurrentArena.SArenaDifficulty);

                // ADD level 
                if (CurrentArena.Stage == arenaData.CurrentArenaLevelData.StageData.Count - 1)
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
            UpdateCurrentArena(losses: Math.Clamp(CurrentArena.Losses + nLoss, 0, ArenaData.MAX_LOSSES), erosion: 0f, save: save);
        }

        public static void AddCurrentArenaPowerUp(string powerUpName, bool save = true)
        {
            SetCurrentArenaPowerUp(powerUpName, CurrentArena.Level - 1 , save);
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
            ArenaData arenaData = AssetLoader.LoadArenaData(CurrentArena.ArenaType, CurrentArena.SArenaDifficulty);

            if (CurrentArena.Level >= arenaData.MaxLevel)
            {
                if (CurrentArena.SArenaDifficulty == UnlockedArenas[CurrentArena.ArenaType])
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
            var arenaDifficulty = UnlockedArenas[arenaType];

            if (arenaDifficulty >= MaxArenaDifficulty)
                return;

            if (arenaDifficulty.Level < ArenaManagementData.NDifficultyLevels - 1)
            {
                arenaDifficulty.Level += 1;
            }
            else
            {
                arenaDifficulty.Difficulty += 1;
                arenaDifficulty.Level = 0;
            }

            UnlockedArenas[arenaType] = arenaDifficulty;
            NotificationCloudData.AddUnlockedArena(arenaType);

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
        public static bool IsArenaRewardCollected(EArenaType arenaType, SArenaDifficulty arenaDifficulty, int arenaLevel)
        {
            // check that current difficulty is above unlocked difficulty
            if (arenaDifficulty < GetUnlockedArenaReward(arenaType).ArenaDifficulty)
                return true;

            // check that current level is above unlocked level (if same difficulty)
            if (arenaDifficulty == GetUnlockedArenaReward(arenaType).ArenaDifficulty && arenaLevel <= GetUnlockedArenaReward(arenaType).ArenaLevel)
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
            if (IsArenaRewardCollected(CurrentArena.ArenaType, CurrentArena.SArenaDifficulty, CurrentArena.Level - 1))
                return;

            // update unlocked values with current values
            unlockedArenaReward.ArenaDifficulty = CurrentArena.SArenaDifficulty;
            unlockedArenaReward.ArenaLevel = CurrentArena.Level - 1;

            // save
            UnlockedArenaRewards[CurrentArena.ArenaType] = unlockedArenaReward;
            if (save)
                Instance.SaveValue(KEY_UNLOCKED_ARENA_REWARDS);
        }

        /// <summary>
        /// Create a new run for the Arena mode
        /// </summary>
        /// <param name="arenaType"></param>
        /// <param name="arenaDifficulty"></param>
        public static void CreateNewCurrentArena(EArenaType arenaType, SArenaDifficulty arenaDifficulty)
        {
            Instance.SetData(KEY_CURRENT_ARENA, new SCurrentArenaCloudData(arenaType, arenaDifficulty));
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
                currentArena.Losses = losses.Value > ArenaData.MAX_LOSSES ? ArenaData.MAX_LOSSES : losses.Value;
            if (erosion.HasValue)
                currentArena.Erosion = erosion.Value;

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

        #endregion


        #region Default Data

        public override void Reset(string key)
        {
            base.Reset(key);

            switch (key)
            {
                case KEY_LEAGUE:
                    Instance.SetData(key, new SLeagueCloudData(ELeague.Iron));
                    break;
                
                case KEY_UNLOCKED_ARENAS:
                    var unlockedArenaData = new Dictionary<EArenaType, SArenaDifficulty>();
                    foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType))) 
                    {
                        unlockedArenaData.Add(arenaType, new SArenaDifficulty());
                    }
                    Instance.SetData(key, unlockedArenaData);
                    break;

                case KEY_UNLOCKED_ARENA_REWARDS:
                    var data = new Dictionary<EArenaType, SUnlockedArenaReward>();
                    foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
                    {
                        data.Add(arenaType, new SUnlockedArenaReward());
                    }
                    Instance.SetData(key, data);
                    break;

                case KEY_CURRENT_ARENA:
                    Instance.SetData(key, new SCurrentArenaCloudData(EArenaType.None));
                    break;
            }
        }

        #endregion


        #region Checkers

        void CheckLeague()
        {
            if (CurrentLeague == ELeague.None)
                Reset(KEY_LEAGUE);
        }

        void CheckCurrentArenaData()
        {
            if (CurrentArena.SArenaDifficulty > MaxArenaDifficulty)
            {
                ErrorHandler.Error("Bad CurrentArena data : SArenaDifficulty > MaxArenaDifficulty - Reseting");
                Reset(KEY_CURRENT_ARENA);
                return;
            }

            bool save = false;
            var currentArena = CurrentArena;

            if (CurrentArena.SArenaDifficulty.Level < 0)
            {
                ErrorHandler.Error($"CurrentArena data has level ({CurrentArena.SArenaDifficulty.Level}) < 0");
                currentArena.SArenaDifficulty.Level = 0;
                save = true;
            }

            if (currentArena.SArenaDifficulty.Level >= ArenaManagementData.NDifficultyLevels)
            {
                ErrorHandler.Error($"CurrentArena data : has level ({currentArena.SArenaDifficulty.Level}) >= " + ArenaManagementData.NDifficultyLevels);
                currentArena.SArenaDifficulty.Level = ArenaManagementData.NDifficultyLevels;
                save = true;
            }

            if (! currentArena.CheckPowerUps())
                save = true;

            if (save)
                Instance.SetData(KEY_CURRENT_ARENA, currentArena);
        }

        void CheckUnlockedArenaData()
        {
            bool save = false;

            if (UnlockedArenas.ContainsKey(EArenaType.None))
            {
                ErrorHandler.Warning($"EArenaType.None was found in UnlockedArenas - Removed");
                UnlockedArenas.Remove(EArenaType.None);
                save = true;
            }

            if (UnlockedArenas.Count == 0)
            {
                Reset(KEY_UNLOCKED_ARENAS);
                return;
            }
            
            foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
            {
                if (arenaType == EArenaType.None)
                    continue;

                if (! UnlockedArenas.ContainsKey(arenaType))
                {
                    ErrorHandler.Error($"Missing arena in arena {arenaType} data : instantiating new one with default values");
                    UnlockedArenas[arenaType] = new SArenaDifficulty(EArenaDifficulty.Easy, level: 1);
                    save = true;
                }
                if (UnlockedArenas[arenaType].Level < 0)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has level ({UnlockedArenas[arenaType].Level}) < 0");
                    UnlockedArenas[arenaType] = new SArenaDifficulty(EArenaDifficulty.Easy, level: 0);
                    save = true;
                }

                if (UnlockedArenas[arenaType].Level >= ArenaManagementData.NDifficultyLevels)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has level ({UnlockedArenas[arenaType].Level}) >= " + ArenaManagementData.NDifficultyLevels);
                    UnlockedArenas[arenaType] = new SArenaDifficulty(UnlockedArenas[arenaType].Difficulty, ArenaManagementData.NDifficultyLevels - 1);
                    save = true;
                }

                EArenaDifficulty maxArenaDifficulty = Enum.GetValues(typeof(EArenaDifficulty)).Cast<EArenaDifficulty>().Last();
                if (UnlockedArenas[arenaType].Difficulty > maxArenaDifficulty)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has difficulty ({UnlockedArenas[arenaType].Difficulty}) > " + maxArenaDifficulty);
                    UnlockedArenas[arenaType] = new SArenaDifficulty(maxArenaDifficulty, ArenaManagementData.NDifficultyLevels - 1);
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
                Reset(KEY_UNLOCKED_ARENAS);
                return;
            }

            foreach (EArenaType arenaType in Enum.GetValues(typeof(EArenaType)))
            {
                if (arenaType == EArenaType.None)
                    continue;

                if (!UnlockedArenaRewards.ContainsKey(arenaType))
                {
                    ErrorHandler.Error($"Missing arena in arena {arenaType} data : instantiating new one with default values");
                    UnlockedArenaRewards[arenaType] = new SUnlockedArenaReward(new SArenaDifficulty((EArenaDifficulty)0, level: 0), -1);
                    save = true;
                }

                if (UnlockedArenaRewards[arenaType].ArenaDifficulty.Level < 0)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has level ({UnlockedArenas[arenaType].Level}) < 0");
                    UnlockedArenaRewards[arenaType] = new SUnlockedArenaReward(new SArenaDifficulty((EArenaDifficulty)0, level: 0), -1);
                    save = true;
                }

                if (UnlockedArenaRewards[arenaType].ArenaDifficulty.Level >= ArenaManagementData.NDifficultyLevels)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has level ({UnlockedArenaRewards[arenaType].ArenaDifficulty.Level}) >= " + ArenaManagementData.NDifficultyLevels);
                    UnlockedArenaRewards[arenaType] = new SUnlockedArenaReward(new SArenaDifficulty(UnlockedArenaRewards[arenaType].ArenaDifficulty.Difficulty, level: ArenaManagementData.NDifficultyLevels - 1), -1);
                    save = true;
                }

                EArenaDifficulty maxArenaDifficulty = Enum.GetValues(typeof(EArenaDifficulty)).Cast<EArenaDifficulty>().Last();
                if (UnlockedArenaRewards[arenaType].ArenaDifficulty.Difficulty > maxArenaDifficulty)
                {
                    ErrorHandler.Error($"UnlockedArenas {arenaType} data : has difficulty ({UnlockedArenaRewards[arenaType].ArenaDifficulty.Difficulty}) > " + maxArenaDifficulty);
                    UnlockedArenaRewards[arenaType] = new SUnlockedArenaReward(new SArenaDifficulty(maxArenaDifficulty, level: ArenaManagementData.NDifficultyLevels - 1), -1);
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

            switch (key)
            {
                case KEY_LEAGUE:
                    CheckLeague();
                    break;

                case KEY_CURRENT_ARENA:
                    CheckCurrentArenaData();
                    break;

                case KEY_UNLOCKED_ARENAS:
                    CheckUnlockedArenaData();
                    break;

                case KEY_UNLOCKED_ARENA_REWARDS:
                    CheckUnlockedArenaRewardsData();
                    break;
            }
        }

        #endregion
    }
}