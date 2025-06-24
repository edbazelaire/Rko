using Data.GameManagement;
using Data;
using Enums;
using Inventory;
using System.Collections.Generic;
using System.Linq;
using System;
using Tools;
using Managers;


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
        public int              CurrentEnemyLifes;
        public int              RewardPower;
        public ERarety          RewardRarety;
        public int              RefreshTokens;

        private bool            m_IsOver;

        public readonly EArenaDifficulty GetArenaDifficulty()   => SArenaDifficulty.Difficulty;
        public string[] GetPowerUps()                           => PowerUps ?? (new string[4]);
        public readonly bool InProgress()                       => ArenaType != EArenaType.None;
        public readonly bool IsOver()                           => m_IsOver || Losses >= ArenaData.MAX_LOSSES || Level > AssetLoader.LoadArenaData(ArenaType, SArenaDifficulty).MaxLevel;
        public bool IsBoss()
        {
            SArenaLevelData? arenaLevelData = ArenaLevelData();
            if (arenaLevelData == null)
                return false;

            return Stage == arenaLevelData.Value.StageData.Count - 1;
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

        public SCurrentArenaCloudData(EArenaType arenaType, SArenaDifficulty arenaDifficulty = default, int level = 0, int stage = 0, SBuildData buildData = default, List<EArenaMod> arenaMods = null, int maxLifes = 3, int losses = 0, float erosion = 0f, string[] powerUps = default, int currentEnemyLifes = 1, int rewardPower = 0, ERarety rewardRarety = 0, int refreshTokens = 0, bool isOver = false)
        {
            if (losses < 0)
            {
                ErrorHandler.Error("Bad current losses value : " + losses);
                losses = 0;
            }

            ArenaType           = arenaType;
            SArenaDifficulty    = arenaDifficulty;
            Level               = level;
            Stage               = stage;
            BuildData           = buildData;
            MaxLifes            = maxLifes;
            Losses              = losses;
            Erosion             = erosion;
            PowerUps            = powerUps;
            ArenaMods           = arenaMods;
            CurrentEnemyLifes   = currentEnemyLifes;
            RewardPower         = rewardPower;
            RewardRarety        = rewardRarety;
            RefreshTokens       = refreshTokens;
            m_IsOver            = isOver;
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

        #endregion
    }
}