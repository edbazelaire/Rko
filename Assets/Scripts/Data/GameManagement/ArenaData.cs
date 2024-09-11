using Data.DataStructures;
using Enums;
using Managers;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using static Unity.Collections.Unicode;

namespace Data.GameManagement
{
    [Serializable]
    public struct SStageData
    {
        public ECharacter Character;
        public int CharacterLevel;
    }

    [Serializable]
    public struct SArenaLevelData
    {
        public List<ESpell>                 Spells;
        public List<STriggerEffect>         TriggerEffects;
        public List<SCharacterStatScaling>  BonusStats;
        public SRewardsData                 RewardsData;
        public List<SStageData>             StageData;
    }


    [CreateAssetMenu(fileName = "ArenaData", menuName = "Game/Management/ArenaData")]
    public class ArenaData : ScriptableObject
    {
        #region Members

        public static Action<EArenaType, int> ArenaLevelCompletedEvent;

        [Header("AI stats")]
        [SerializeField] int m_BotPerfLevelCeiling                  = 10;
        [SerializeField] (float Min, float Max) m_DecisionRefresh   = (0.05f, 0.75f);
        [SerializeField] (float Min, float Max) m_Randomness        = (0f, 1f);

        [Header("Arena Data")]
        [SerializeField] List<SArenaLevelData> m_ArenaLevelData;

        public int CurrentLevel => ProgressionCloudData.SoloArenas[ArenaType].CurrentLevel;
        public int CurrentStage => ProgressionCloudData.SoloArenas[ArenaType].CurrentStage;
        public float CurrentRewardMultiplicator => (1 + (int)ArenaDifficulty * 10) * (1 + CurrentStage * 0.5f + CurrentLevel * 0.05f);

        public EArenaType               ArenaType               => Enum.TryParse(name.Split("_")[0], out EArenaType arenaType) ? arenaType : EArenaType.FireArena;
        public EArenaDifficulty         ArenaDifficulty         => Enum.TryParse(name.Split("_")[1], out EArenaDifficulty arenaDifficulty) ? arenaDifficulty : EArenaDifficulty.Normal;
        public List<SArenaLevelData>    ArenaLevelData          => m_ArenaLevelData;
        public SArenaLevelData          CurrentArenaLevelData   => GetArenaLevelData(CurrentLevel);
        public SStageData               CurrentStageData        => GetStageData(CurrentLevel, CurrentStage);
        public int                      MaxLevel                => m_ArenaLevelData.Count;

        #endregion


        #region Accessors

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

            return m_ArenaLevelData[arenaLevel];
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

            return stageDataList[stage];
        }

        public void UpdateStageValue(bool up)
        {
            ProgressionCloudData.UpdateStageValue(ArenaType, up);
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

            ERune[] runes;
            switch (ArenaType)
            {
                case EArenaType.FireArena:
                    runes = new ERune[] { ERune.FireRune, ERune.None, ERune.None };
                    break;

                default:
                    runes = new ERune[] { ERune.None, ERune.None, ERune.None };
                    break;
            }

            // set rune levels equal to character level
            int[] runeLevels = new int[runes.Length];
            for (int i = 0; i < runes.Length; i++)
            {
                // make sure that character level is not > to max spell level
                runeLevels[i] = Math.Min(CurrentStageData.CharacterLevel, maxLevel);
            }

            // set spell levels equal to character level
            int[] spellLevels = new int[CurrentArenaLevelData.Spells.Count];
            for (int i = 0; i < CurrentArenaLevelData.Spells.Count; i++)
            {
                // make sure that character level is not > to max spell level
                spellLevels[i] = Math.Min(CurrentStageData.CharacterLevel, maxLevel);
            }

            // create & return PlayerData
            return new SPlayerData(
                playerName:     CurrentStageData.Character.ToString(),
                characterLevel: CurrentStageData.CharacterLevel,
                character:      CurrentStageData.Character,
                runes:          runes,
                runeLevels:     runeLevels,      
                spells:         CurrentArenaLevelData.Spells.ToArray(),
                spellLevels:    spellLevels,
                profileData:    CreateProfileData(),
                isPlayer:       false,

                triggerEffects: CurrentArenaLevelData.TriggerEffects.ToArray(),
                bonusStats:     CurrentArenaLevelData.BonusStats.ToArray(),
                botData :       new SBotData(
                    GetDecisionRefresh(CurrentStageData.CharacterLevel), 
                    GetRandomness(CurrentStageData.CharacterLevel)
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
                gamerTag: CurrentStageData.Character.ToString(),
                avatar: CurrentStageData.Character.ToString(),
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
            if (CurrentLevel == 0)
                return EBorder.None;

            if (CurrentLevel == 1)
                return EBorder.LeagueBronze;

            if (CurrentLevel == 2)
                return EBorder.LeagueSilver;

            if (CurrentLevel == 3)
                return EBorder.LeagueGold;

            return EBorder.Rank1;
        }

        #endregion
    }
}