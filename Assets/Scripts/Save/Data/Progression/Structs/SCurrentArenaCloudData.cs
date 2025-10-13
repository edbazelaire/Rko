using Data.GameManagement;
using Data;
using Enums;
using Inventory;
using System.Collections.Generic;
using System.Linq;
using System;
using Tools;
using Managers;
using MyBox;
using Game.Loaders;
using Data.DataStructures.PowerEffects;
using Game.GameManagers.ArenaModules;


namespace Save.Data.Progression.Structs
{
    [Serializable]
    public struct SCurrentArenaCloudData
    {
        #region Members

        public EArenaType ArenaType;
        public SArenaDifficulty SArenaDifficulty;

        public int              Level;
        public int              Stage;
        public SBuildData       BuildData;
        public int              MaxLifes;
        public int              Losses;
        public float            Erosion;
        public string[]         PowerUps;
        public List<EArenaMod>  ArenaMods;
        public int              CurrentEnemyLifesLost;
        public int              RewardPower;
        public ERarety          RewardRarety;
        public int              RefreshTokens;

        public Dictionary<string, string> MetaData;

        private bool            m_IsOver;

        public readonly EArenaDifficulty GetArenaDifficulty()   => SArenaDifficulty.Difficulty;
        public readonly int GetExtraDifficulty()                => SArenaDifficulty.Level;
        public string[] GetPowerUps()                           => PowerUps ?? (new string[4]);
        public readonly bool InProgress()                       => ArenaType != EArenaType.None;
        public readonly int GetMaxLosses()                      => ArenaMods.Contains(EArenaMod.NoDeath) ? 1 : ArenaData.MAX_LOSSES;
        public readonly bool IsOver()                           => m_IsOver || Losses >= GetMaxLosses() || Level > AssetLoader.LoadArenaData(ArenaType, SArenaDifficulty).MaxLevel;
        public readonly bool HasBuildData()                     => ! BuildData.Character.IsNullOrEmpty();
        public bool IsBoss()
        {
            SArenaLevelData? arenaLevelData = ArenaLevelData();
            if (arenaLevelData == null)
                return false;

            return Stage == arenaLevelData.Value.StageData.Count - 1;
        }

        public bool IsLastBoss()
        {
            return IsMaxArenaLevel() && IsMaxStage();
        }

        public SArenaLevelData? ArenaLevelData()
        {
            var arenaData = LoadArenaData();

            if (Level > arenaData.MaxLevel + 1)
            {
                ErrorHandler.Error($"CurrentArena.Level ({Level}) > max arena level + 1 ({arenaData.MaxLevel + 1}) for arena {ArenaType} at difficulty {SArenaDifficulty} - setting to max arena level");
                Level = arenaData.MaxLevel + 1;
            }

            if (Level >= arenaData.MaxLevel + 1)
                return null;

            if (Level < 0)
            {
                ErrorHandler.Error($"CurrentArena.Level ({Level}) < 0 for arena {ArenaType} at difficulty {SArenaDifficulty} at arena level {Level} - setting to max arena level");
                Level = 0;
            }

            return arenaData.ArenaLevelData[Level];
        }
        public bool IsArenaCompleted() => Level >= LoadArenaData().MaxLevel + 1;
        public bool IsMaxArenaLevel() => Level >= LoadArenaData().MaxLevel;
        public bool IsMaxStage()
        {
            if (!CheckCurrentStage())
                return false;

            var arenaLevelData = ArenaLevelData();
            if (arenaLevelData == null)
            {
                ErrorHandler.Warning("Unable to check if is max stage - arena level data is null");
                return true;
            }

            return Stage == arenaLevelData.Value.StageData.Count - 1;
        }

        #endregion


        #region Constructor

        public SCurrentArenaCloudData(
            EArenaType arenaType, 
            SArenaDifficulty arenaDifficulty    = default, 
            int level                           = 0, 
            int stage                           = 0, 
            SBuildData buildData                = default, 
            List<EArenaMod> arenaMods           = null, 
            int maxLifes                        = 3, 
            int losses                          = 0, 
            float erosion                       = 0f, 
            string[] powerUps                   = default, 
            int currentEnemyLifesLost           = 1, 
            int rewardPower                     = 0,
            ERarety rewardRarety                = 0, 
            int refreshTokens                   = 0, 
            bool isOver                         = false, 
            Dictionary<string, string> metaData = null
        )
        {
            if (losses < 0)
            {
                ErrorHandler.Error("Bad current losses value : " + losses);
                losses = 0;
            }

            ArenaType               = arenaType;
            SArenaDifficulty        = arenaDifficulty;
            Level                   = level;
            Stage                   = stage;
            BuildData               = buildData;
            MaxLifes                = maxLifes;
            Losses                  = losses;
            Erosion                 = erosion;
            PowerUps                = powerUps;
            ArenaMods               = arenaMods;
            CurrentEnemyLifesLost   = currentEnemyLifesLost;
            RewardPower             = rewardPower;
            RewardRarety            = rewardRarety;
            RefreshTokens           = refreshTokens;
            m_IsOver                = isOver;

            MetaData = metaData ?? new Dictionary<string, string>();
        }

        #endregion


        #region Rewards

        public SPowerOrb GetPowerOrb()
        {
            if (InProgress())
                return new SPowerOrb(RewardPower, RewardRarety);

            return new SPowerOrb(0, GetStartPowerOrbRarety());
        }

        public ERarety GetStartPowerOrbRarety()
        {
            return HasMod(EArenaMod.Random) && RewardRarety < ERarety.Legendary ? RewardRarety + 1 : RewardRarety;
        }

        public float GetBonusPowerOrb()
        {
            float bonus = 1f;
            foreach (var mod in GetArenaMods())
            {
                bonus += AssetLoader.LoadArenaMod(mod).BonusPower;
            }

            foreach (var powerUpName in GetPowerUps())
            {
                if (powerUpName.IsNullOrEmpty())
                    continue;

                var powerUp = SpellLoader.GetPowerUp(powerUpName);
                if (powerUp is SPowerUp sPowerUp)
                    bonus += sPowerUp.BonusPowerOrb;
            }

            return bonus;
        }

        #endregion


        #region Mods

        public List<EArenaMod> GetArenaMods() 
        {
            if (InProgress())
                return ArenaMods ?? new() { };

            EArenaType arenaType = PlayerPrefsHandler.GetArenaType();
            return PlayerPrefsHandler.GetArenaMods(arenaType, PlayerPrefsHandler.GetArenaDifficulty(arenaType));
        }

        public bool HasMod(EArenaMod arenaMod)
        {
            var arenaMods = GetArenaMods();
            return arenaMods != null && arenaMods.Contains(arenaMod);
        }

        #endregion


        #region Lifes

        public void RemoveEnemyLife(int nLifes) 
        {
            CurrentEnemyLifesLost += nLifes;
        }

        #endregion


        #region Data

        public readonly ArenaData LoadArenaData()
        {
            return AssetLoader.LoadArenaData(ArenaType, SArenaDifficulty);
        }

        public void SetIsOver(bool isOver)
        {
            m_IsOver = isOver;
        }

        public void SetBuildData(SBuildData buildData)
        {
            BuildData = buildData;
        }

        public void SetBuildValue(Enum collectable, int level, int index)
        {
            if (collectable is ESpell spell)
            {
                BuildData.Spells[index] = spell;
                if (level > 0)
                    BuildData.SpellLevels[index] = level;
            }
            
            else if (collectable is ERune rune)
            {
                BuildData.Runes[index] = rune;
                if (level > 0)
                    BuildData.RuneLevels[index] = level;
            }
            
            else if (collectable is ECharacter character)
            {
                BuildData.Character = character.ToString();
                if (level > 0)
                    BuildData.CharacterLevel = level;
            }
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

        #endregion


        #region MetaData

        public void RefreshMetaData()
        {
            MetaData = new();
        }

        public void SetMetaData(string key, string value)
        {
            if (MetaData == null)
                MetaData = new();

            CheckMetadata(key, ref value);

            if (! MetaData.ContainsKey(key))
                MetaData.Add(key, value);
            else
                MetaData[key] = value;
        }

        public T GetMetaData<T>(string key)
        {
            if (MetaData == null || !MetaData.ContainsKey(key))
                return default;

            string raw = MetaData[key];

            try
            {
                if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(raw, out int iValue))
                        return (T)(object)iValue;
                    ErrorHandler.Error($"Unable to parse '{raw}' as int");
                    return default;
                }

                if (typeof(T) == typeof(float))
                {
                    if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fValue))
                        return (T)(object)fValue;
                    ErrorHandler.Error($"Unable to parse '{raw}' as float");
                    return default;
                }

                if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(raw, out bool bValue))
                        return (T)(object)bValue;
                    ErrorHandler.Error($"Unable to parse '{raw}' as bool");
                    return default;
                }

                if (typeof(T).IsEnum)
                {
                    if (Enum.TryParse(typeof(T), raw, out object eValue))
                        return (T)eValue;
                    ErrorHandler.Error($"Unable to parse '{raw}' as enum {typeof(T).Name}");
                    return default;
                }

                // fallback : string
                if (typeof(T) == typeof(string))
                    return (T)(object)raw;

                ErrorHandler.Warning($"GetMetaData: Unsupported type {typeof(T).Name}, returning default.");
                return default;
            }
            catch (Exception ex)
            {
                ErrorHandler.Error($"Exception parsing MetaData[{key}]='{raw}' as {typeof(T).Name}: {ex.Message}");
                return default;
            }
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check that values are consistent - otherwise make the changes
        /// </summary>
        public bool Check()
        {
            bool test = true;

            test = test && CheckArenaData();
            test = test && CheckPowerUps();
            test &= BuildData.Check();

            return test;
        }

        public bool CheckArenaData()
        {
            if (ArenaType == EArenaType.None)
            {
                ErrorHandler.Error("Bad arena type provided : " + ArenaType);
                return false;
            }

            CheckCurrentStage();

            return true;
        }

        public bool CheckCurrentStage()
        {
            var arenaLevelData = ArenaLevelData();

            if (Stage < 0)
            {
                ErrorHandler.Error($"CurrentArena.Stage ({Stage}) < 0 for arena {ArenaType} at difficulty {SArenaDifficulty} at arena level {Level} - setting to max arena level");
                Stage = 0;
                return false;
            }

            if (arenaLevelData == null)
            {
                ErrorHandler.Warning("Unable to check stage - arena data is null");
                return true;
            }

            if (Stage > arenaLevelData.Value.StageData.Count)
            {
                ErrorHandler.Error($"CurrentArena.Stage ({Stage}) > max arena stage ({arenaLevelData.Value.StageData.Count}) for arena {ArenaType} at difficulty {SArenaDifficulty} at arena level {Level} - setting to max arena level");
                Stage = arenaLevelData.Value.StageData.Count - 1;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if mods are correct
        /// </summary>
        /// <returns></returns>
        public bool CheckMods()
        {
            return true;
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
            for (int i = 0; i < PowerUps.Length; i++)
            {
                string powerUpName = PowerUps[i];

                if (PowerUps[i] == "")
                    continue;

                // if no longer exists : reset value
                if (!SRunePower.TrySplitPowerUpName(powerUpName, out string _, out ERuneActivation _))
                {
                    PowerUps[i] = "";
                    test = false;
                }
            }

            return test;
        }

        public bool CheckMetadata(string key, ref string value)
        {
            if (key == EArenaMetadataKeys.CorruptionStacks.ToString())
            {
                if (! int.TryParse(value, out int iValue))
                {
                    ErrorHandler.Error($"Unable to parse value {value} of {key} into a string");
                    value = "0";
                    return false;
                }

                if (iValue < 0 || iValue > 100)
                {
                    ErrorHandler.Error($"Wrong value {value} provided for {key} : must be between 0 and 100");
                    value = Math.Clamp(iValue, 0, 100).ToString();
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}