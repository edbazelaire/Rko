using Data.DataStructures;
using Enums;
using Managers;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Data.GameManagement
{
    [Serializable]
    public struct SStageData
    {
        [SerializeField] EBoss m_Boss;
        [SerializeField] int m_Level;
        [SerializeField] List<ESpell> m_Spells;
        [SerializeField] List<STriggerEffect> m_TriggerEffects;
        [SerializeField] List<SCharacterStatScaling> m_BonusStats;

        int m_ArenaDifficultyLevel;

        public readonly EBoss                   Boss            => m_Boss;
        public readonly int                     Level           => m_Level + (m_ArenaDifficultyLevel - 1);
        public readonly List<ESpell>            Spells          => m_Spells;
        public readonly List<STriggerEffect>    TriggerEffects
        {
            get
            {
                var triggerEffects = new List<STriggerEffect>();
                foreach (var effect in m_TriggerEffects)
                {
                    var duplicateEffect = effect;
                    duplicateEffect.Level += 2 * (m_ArenaDifficultyLevel - 1);
                    triggerEffects.Add(duplicateEffect);
                }

                return triggerEffects;
            }
        }

        public readonly List<SCharacterStatScaling>  BonusStats      => m_BonusStats;

        public SStageData SetArenaDifficultyLevel(int arenaDifficultyLevel)
        {
            m_ArenaDifficultyLevel = arenaDifficultyLevel;
            return this;
        }
    }

    [Serializable]
    public struct SArenaLevelData
    {
        public SRewardsData                 RewardsData;
        public List<STriggerEffect>         TriggerEffects;
        public List<SStageData>             StageData;
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

        public int CurrentLevel => ProgressionCloudData.CurrentArena.Level;
        public int CurrentStage => ProgressionCloudData.CurrentArena.Stage;
        public float CurrentRewardMultiplicator => (1 + (int)ArenaDifficulty * 10) * (1 + CurrentStage * 0.5f + CurrentLevel * 0.05f);

        public EArenaType               ArenaType               => Enum.TryParse(name.Split("_")[0], out EArenaType arenaType) ? arenaType : EArenaType.FireArena;
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
                duplicateEffect.Level += 2 * (m_ArenaDifficultyLevel - 1);
                triggerEffects.Add(duplicateEffect);
            }
            arenaLevelData.TriggerEffects = triggerEffects;

            for (int i = 0; i < m_ArenaLevelData[arenaLevel].StageData.Count; i++)
            {
                arenaLevelData.StageData[i].SetArenaDifficultyLevel(m_ArenaDifficultyLevel);
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

            return stageDataList[stage].SetArenaDifficultyLevel(m_ArenaDifficultyLevel);
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
            if (ProgressionCloudData.CurrentArena.Level == 0)
                return new SRewardsData();

            return GetArenaLevelData(ProgressionCloudData.CurrentArena.Level - 1).RewardsData;
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
                bonusStats:     CurrentStageData.BonusStats.ToArray(),
                botData :       new SBotData(
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
                gamerTag: CurrentStageData.Boss.ToString(),
                avatar: CurrentStageData.Boss.ToString(),
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

            //if (ArenaDifficulty == EArenaDifficulty.HardCore)
            //    return EBorder.LeagueBronze;

            //if (ArenaDifficulty == EArenaDifficulty.Normal)
            //    return EBorder.LeagueSilver;

            //if (ArenaDifficulty == EArenaDifficulty.Normal)
            //    return EBorder.LeagueGold;

            if (ArenaDifficulty == EArenaDifficulty.HardCore)
                return EBorder.Frost;

            return EBorder.Rank1;
        }

        #endregion
    }
}