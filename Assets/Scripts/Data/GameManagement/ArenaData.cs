using Data.DataStructures;
using Enums;
using Inventory;
using Managers;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using UnityEngine;

namespace Data.GameManagement
{
    [Serializable]
    public struct SStageData
    {
        [SerializeField] EBoss                          m_Boss;
        [SerializeField] int                            m_BonusLevel;
        [SerializeField] List<ESpell>                   m_Spells;
        [SerializeField] List<STriggerEffect>           m_TriggerEffects;
        [SerializeField] List<SCharacterStatScaling>    m_BonusStats;

        int m_BaseLevel;

        public readonly EBoss                   Boss            => m_Boss;
        public readonly int                     Level           => m_BonusLevel + m_BaseLevel;
        public readonly List<ESpell>            Spells          => m_Spells;
        public readonly List<STriggerEffect>    TriggerEffects
        {
            get
            {
                var triggerEffects = new List<STriggerEffect>();
                foreach (var effect in m_TriggerEffects)
                {
                    var duplicateEffect = effect;
                    duplicateEffect.Level = Level;
                    triggerEffects.Add(duplicateEffect);
                }

                return triggerEffects;
            }
        }

        public readonly List<SCharacterStatScaling>  BonusStats      => m_BonusStats;

        public SStageData SetBaseLevel(int baseLevel)
        {
            m_BaseLevel = baseLevel;
            return this;
        }
    }

    [Serializable]
    public struct SArenaLevelData
    {
        public SRewardsData                 RewardsData;
        public List<STriggerEffect>         TriggerEffects;
        [SerializeField] List<string>       m_PowerUps;
        public List<SStageData>             StageData;

        public List<string> PowerUps => m_PowerUps;
    }


    [CreateAssetMenu(fileName = "ArenaData", menuName = "Game/Management/ArenaData")]
    public class ArenaData : ScriptableObject
    {
        #region Members

        // ===============================================================================================
        // ACTIONS 
        public static Action<EArenaType, int> ArenaLevelCompletedEvent;

        // ===============================================================================================
        // CONSTANTS
        public const int MAX_LOSSES = 3;

        // ===============================================================================================
        // DATA
        [Header("AI stats")]
        [SerializeField] int m_BotPerfLevelCeiling                  = 10;
        [SerializeField] (float Min, float Max) m_DecisionRefresh   = (0.05f, 0.05f);
        [SerializeField] (float Min, float Max) m_Randomness        = (0f, 1f);

        [Header("Arena Data")]
        [SerializeField] List<SArenaLevelData> m_ArenaLevelData;

        protected int m_ArenaDifficultyLevel;

        public int CurrentLevel                 => ProgressionCloudData.CurrentArena.Level;
        public int CurrentStage                 => ProgressionCloudData.CurrentArena.Stage;
        public int CurrentBaseCharacterLevel    => (int)ArenaDifficulty * 2 + m_ArenaDifficultyLevel;
        public float CurrentRewardMultiplicator => 1 + (int)ArenaDifficulty * 0.5f + m_ArenaDifficultyLevel * 0.15f;

        public EArenaType               ArenaType               => Enum.TryParse(name.Split("_")[0], out EArenaType arenaType) ? arenaType : EArenaType.FrostArena;
        public SArenaDifficulty         SArenaDifficulty        => new SArenaDifficulty(ArenaDifficulty, ArenaDifficultyLevel);
        public EArenaDifficulty         ArenaDifficulty         => Enum.TryParse(name.Split("_")[1], out EArenaDifficulty arenaDifficulty) ? arenaDifficulty : EArenaDifficulty.Normal;
        public int                      ArenaDifficultyLevel    => m_ArenaDifficultyLevel;
        public List<SArenaLevelData>    ArenaLevelData          => m_ArenaLevelData;
        public SArenaLevelData          CurrentArenaLevelData   => GetArenaLevelData(CurrentLevel);
        public SStageData               CurrentStageData        => GetStageData(CurrentLevel, CurrentStage);
        public int                      MaxLevel                => m_ArenaLevelData.Count - 1;

        #endregion


        #region Accessors

        public void SetDifficultyLevel(int level)
        {
            m_ArenaDifficultyLevel = level;
        }

        /// <summary>
        /// Get data of the requested level
        /// </summary>
        /// <param name="arenaLevel"></param>
        /// <returns></returns>
        public SArenaLevelData GetArenaLevelData(int arenaLevel)
        {
            if (arenaLevel < 0 || arenaLevel >= m_ArenaLevelData.Count)
            {
                ErrorHandler.Error("Bad arena level : " + arenaLevel);
                arenaLevel = 0;
            }

            var arenaLevelData = m_ArenaLevelData[arenaLevel];

            var triggerEffects = new List<STriggerEffect>();
            foreach (var effect in arenaLevelData.TriggerEffects)
            {
                var duplicateEffect = effect;
                duplicateEffect.Level = CurrentBaseCharacterLevel;
                triggerEffects.Add(duplicateEffect);
            }
            arenaLevelData.TriggerEffects = triggerEffects;

            for (int i = 0; i < m_ArenaLevelData[arenaLevel].StageData.Count; i++)
            {
                arenaLevelData.StageData[i].SetBaseLevel(CurrentBaseCharacterLevel);
            }

            return arenaLevelData;
        }

        /// <summary>
        /// Get data of the requested stage in the requested level
        /// </summary>
        /// <param name="arenaLevel"></param>
        /// <param name="stage"></param>
        /// <returns></returns>
        public SStageData GetStageData(int arenaLevel, int stage)
        {
            var stageDataList = GetArenaLevelData(arenaLevel).StageData;
            if (stage < 0 || stage >= stageDataList.Count)
            {
                ErrorHandler.Error("Bad arena level : " + arenaLevel);
                stage = 0;
            }

            return stageDataList[stage].SetBaseLevel(CurrentBaseCharacterLevel);
        }

        public EBoss GetBoss(int arenaLevel)
        {
            if (arenaLevel > m_ArenaLevelData.Count)
            {
                ErrorHandler.Error("Trying to get boss for arena level " + arenaLevel + " with arena max level beeing " + m_ArenaLevelData.Count);
                return EBoss.None;
            }
            return m_ArenaLevelData[arenaLevel].StageData.LastOrDefault().Boss;
        }

        public SRewardsData GetCurrentRewards()
        {
            if (ProgressionCloudData.CurrentArena.GetPowerOrb().Power == 0)
                return default;

            // init rewards
            var rewards = new SRewardsData();
            rewards.SetDefaultData();

            // add current orb as reward
            rewards.Add(ProgressionCloudData.CurrentArena.GetPowerOrb());

            // current difficulty inferior to already unlocked difficulty -> return rewards
            if (SArenaDifficulty < ProgressionCloudData.GetUnlockedArenaReward(ArenaType).ArenaDifficulty)
                return rewards;  
            
            for (int arenaLevel = 0; arenaLevel < ProgressionCloudData.CurrentArena.Level; arenaLevel++)
            {
                // check if this arena level has already been collected
                if (ProgressionCloudData.IsArenaRewardCollected(ArenaType, SArenaDifficulty, arenaLevel))
                    continue;

                rewards.Add(m_ArenaLevelData[arenaLevel].RewardsData);
            }

            return rewards;
        }

        /// <summary>
        /// Calculate the total OrbPower that can be collected during this Arena
        /// </summary>
        /// <returns></returns>
        public int CalculateMaxOrbPower()
        {
            int maxPower = 0;

            for (int arenaLevel = 0; arenaLevel < m_ArenaLevelData.Count; arenaLevel++)
            {
                // add power of each mobs of arena level
                maxPower += CalculateOrbPowerReward(arenaLevel, 0) * (m_ArenaLevelData[arenaLevel].StageData.Count - 1);
                // add boss power
                maxPower += CalculateOrbPowerReward(arenaLevel, m_ArenaLevelData[arenaLevel].StageData.Count - 1);
            }

            return maxPower;
        }

        /// <summary>
        /// Calculate the Power to add to the current OrbPower from finishing a stage
        /// </summary>
        /// <param name="arenaLevel"></param>
        /// <param name="arenaStage"></param>
        /// <returns></returns>
        public int CalculateOrbPowerReward(int arenaLevel, int arenaStage)
        {
            // TODO : constants
            int baseMobPower                = 5;
            int baseBossPower               = 50;
            float mobPowerIncreasePerLevel  = 0.2f;
            float bossPowerIncreasePerLevel = 0.5f;
            float bonusArenaDifficulty      = 0.3f;
            float bonusArenaDifficultyLevel = 0.15f;
            // TODO : constants

            float basePowerIncreasePerLevel;
            float basePower;
            // MOB REWARD
            if (arenaStage < m_ArenaLevelData[arenaLevel].StageData.Count - 1)
            {
                basePower = baseMobPower;
                basePowerIncreasePerLevel = mobPowerIncreasePerLevel;
            }
            else
            {
                basePower = baseBossPower;
                basePowerIncreasePerLevel = bossPowerIncreasePerLevel;
            }

            return (int)Math.Round(
                basePower * (1 + arenaLevel * basePowerIncreasePerLevel)        // base power level from current arena level
                * Math.Pow(1 + bonusArenaDifficulty, (int)ArenaDifficulty)      // power level increase from arena difficulty (normal, hard, brutal, ...)
                * (1 + bonusArenaDifficultyLevel * ArenaDifficultyLevel)        // power level increase from arena difficulty bonus level (+, ++, ... etc)
            );
        }

        #endregion


        #region Player Data Manipulators

        /// <summary>
        /// Create PlayerData from AI ArenaData
        /// </summary>
        /// <returns></returns>
        public SPlayerData CreatePlayerData()
        {
            int maxLevel = CollectablesManagementData.GetMaxLevel(ESpell.AxeThrow);

            ERune[] runes = new ERune[] { ERune.None, ERune.None, ERune.None };

            // set rune levels equal to character level
            int[] runeLevels = new int[runes.Length];
            for (int i = 0; i < runes.Length; i++)
            {
                // make sure that character level is not > to max spell level
                runeLevels[i] = Math.Min(CurrentStageData.Level, maxLevel);
            }

            // set spell levels equal to character level
            int[] spellLevels = new int[CurrentStageData.Spells.Count];
            for (int i = 0; i < CurrentStageData.Spells.Count; i++)
            {
                // make sure that character level is not > to max spell level
                spellLevels[i] = Math.Min(CurrentStageData.Level, maxLevel);
            }

            // create & return PlayerData
            var triggerEffects = CurrentArenaLevelData.TriggerEffects;
            triggerEffects.AddRange(CurrentStageData.TriggerEffects);
            return new SPlayerData(
                playerName:     CurrentStageData.Boss.ToString(),
                characterLevel: CurrentStageData.Level,
                character:      CurrentStageData.Boss.ToString(),
                runes:          runes,
                runeLevels:     runeLevels,      
                spells:         CurrentStageData.Spells.ToArray(),
                spellLevels:    spellLevels,
                profileData:    CreateProfileData(),
                isPlayer:       false,

                triggerEffects: triggerEffects.ToArray(),
                powerUps:       CurrentArenaLevelData.PowerUps.Select(str => new FixedString64Bytes(str)).ToArray(),
                bonusStats:     CurrentStageData.BonusStats.ToArray(),
                botData :       new SBotData(
                    ArenaDifficulty,
                    GetDecisionRefresh(CurrentStageData.Level), 
                    GetRandomness(CurrentStageData.Level)
                )
            );
        }

        /// <summary>
        /// Create ProfileData for the AI depending on character & progression
        /// </summary>
        /// <returns></returns>
        public SProfileDataNetwork CreateProfileData()
        {
            return new SProfileDataNetwork(
                accountLevel: CurrentStageData.Level,
                gamerTag: CurrentStageData.Boss.ToString(),
                avatar: EAvatar.None.ToString(),
                border: GetBorder().ToString(),
                title: ETitle.None.ToString()
            );
        }

        float GetDecisionRefresh(int characterLevel)
        {
            return m_DecisionRefresh.Min + Mathf.Max(0, (1 - characterLevel / m_BotPerfLevelCeiling) * (m_DecisionRefresh.Max - m_DecisionRefresh.Min));
        }

        float GetRandomness(int characterLevel)
        {
            return m_Randomness.Min + Mathf.Max(0, (1 - characterLevel / m_BotPerfLevelCeiling) * (m_Randomness.Max - m_Randomness.Min));
        }

        /// <summary>
        /// Get Border of the "profile" depending on current level
        /// </summary>
        /// <returns></returns>
        public EBorder GetBorder()
        {
            if (ArenaDifficulty == EArenaDifficulty.Normal)
                return EBorder.None;

            if (ArenaDifficulty == EArenaDifficulty.Hard)
                return EBorder.LeagueBronze;

            if (ArenaDifficulty == EArenaDifficulty.Painful)
                return EBorder.LeagueSilver;

            //if (ArenaDifficulty == EArenaDifficulty.Normal)
            //    return EBorder.LeagueGold;

            return EBorder.Frost;
        }

        #endregion
    }
}