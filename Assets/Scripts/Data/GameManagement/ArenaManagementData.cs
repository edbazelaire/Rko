using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.GameManagement
{
    [Serializable]
    public struct SPowerUpDropRate
    {
        public float Minor;
        public float Major;
        public float Primal;

        public Dictionary<ERuneActivation, float> Get() => new Dictionary<ERuneActivation, float>()
        {
            { ERuneActivation.Minor, Minor },
            { ERuneActivation.Major, Major },
            { ERuneActivation.Primal, Primal },
        };

        public ERuneActivation SelectRandomRarety()
        {
            var rareties = Get();
            var currentPercentage = 0f;
            var rand = UnityEngine.Random.Range(0f, 1f);
            foreach (var item in rareties) 
            { 
                currentPercentage += item.Value;
                if (currentPercentage >= rand)
                    return item.Key;
            }

            ErrorHandler.Error("Unable to find random rarety, percentages have been set wrong");
            return ERuneActivation.Major;
        }
    }

    [Serializable]
    public class SArenaSpecialConfig
    {
        public EArenaType Arena;
        [Tooltip("Base level for this Arena")]
        public int BaseLevel = 1;

        [Header("Orb Power Reward")]
        [SerializeField, Tooltip("Quantity of power dropped by a MOB depending on level")]
        SScalingStat m_MobPowerDrop = new SScalingStat(25f, 0.2f);
        [SerializeField, Tooltip("Quantity of power dropped by a BOSS depending on level")]
        SScalingStat m_BossPowerDrop = new SScalingStat(350f, 0.35f);
        [SerializeField, Tooltip("Quantity of power dropped at the END of the Arena depending on level")]
        SScalingStat m_CompletionDrop = new SScalingStat(25f, 0.2f);
        [SerializeField, Tooltip("Quantity of power increased at each Arena Difficulty")]
        float m_BonusArenaDifficulty = 0.35f;
        [SerializeField, Tooltip("Quantity of power increased for each extra arena difficulty level (+, ++, ...)")]
        float m_BonusArenaDifficultyLevel = 0f;

        public SScalingStat MobPowerDrop        => m_MobPowerDrop;
        public SScalingStat BossPowerDrop       => m_BossPowerDrop;
        public SScalingStat CompletionPowerDrop => m_CompletionDrop;

    }

    [CreateAssetMenu(fileName = "ArenaManagementData", menuName = "Game/Management/ArenaManagementData")]
    public class ArenaManagementData : ScriptableObject
    {
        #region Members

        static ArenaManagementData s_Instance;

        [Header("Arena Management")]
        [SerializeField] private int m_NDifficultyLevels    = 5;
        [SerializeField] private int m_MaxLevels            = 4;
        [SerializeField] private List<SPowerUpDropRate> m_PowerUpDropRates;
        [SerializeField] private List<Vector2> m_EternalMenagerieUpgradeCosts;
        [SerializeField] private int m_NStartRefreshes              = 4;
        [SerializeField] private int m_NStartRefreshes_BonusRandom  = 3;
        [SerializeField] private int m_NRefreshesOnBoss             = 1;
        [SerializeField] private int m_NCorruptionStacksLossOnDeath = 20;
        [SerializeField] private int m_NCorruptionStacksLossOnWin   = 10;

        [Header("Special Arena Config")]
        [SerializeField] private List<SArenaSpecialConfig> m_SpecialConfigs;

        [Header("Orb Power Reward")]
        [SerializeField, Tooltip("Quantity of power dropped by a MOB depending on level")]
        SScalingStat m_MobPowerDrop = new SScalingStat(25f, 0.2f);
        [SerializeField, Tooltip("Quantity of power dropped by a BOSS depending on level")]
        SScalingStat m_BossPowerDrop = new SScalingStat(350f, 0.35f);
        [SerializeField, Tooltip("Quantity of power dropped at the END of the Arena depending on level")]
        SScalingStat m_CompletionDrop = new SScalingStat(25f, 0.2f);
        [SerializeField, Tooltip("Quantity of power increased at each Arena Difficulty")]
        float m_BonusArenaDifficulty = 0.35f;
        [SerializeField, Tooltip("Quantity of power increased for each extra arena difficulty level (+, ++, ...)")]
        float m_BonusArenaDifficultyLevel = 0f;

        // ================================================================================================
        // Public Accessors
        public static List<SPowerUpDropRate> PowerUpDropRates   => Instance.m_PowerUpDropRates;
        public static List<Vector2> EternalMenagerieUpgradeCosts => Instance.m_EternalMenagerieUpgradeCosts;
        public static int NStartRefreshes                       => Instance.m_NStartRefreshes;
        public static int NStartRefreshes_BonusRandom           => Instance.m_NStartRefreshes_BonusRandom;
        public static int NRefreshesOnBoss                      => Instance.m_NRefreshesOnBoss;
        public static int NCorruptionStacksLossOnDeath          => Instance.m_NCorruptionStacksLossOnDeath;
        public static int NCorruptionStacksLossOnWin            => Instance.m_NCorruptionStacksLossOnWin;
        public static int NDifficultyLevels                     => Instance.m_NDifficultyLevels;
        public static int MaxLevels                             => Instance.m_MaxLevels;
        public static SScalingStat MobPowerDrop                 => Instance.m_MobPowerDrop;
        public static SScalingStat BossPowerDrop                => Instance.m_BossPowerDrop;
        public static SScalingStat CompletionPowerDrop          => Instance.m_CompletionDrop;
        public static float BonusArenaDifficulty                => Instance.m_BonusArenaDifficulty;
        public static float BonusArenaDifficultyLevel           => Instance.m_BonusArenaDifficultyLevel;

        #endregion


        #region Special Arena Configs

        public static SArenaSpecialConfig GetArenaSpecialConfig(EArenaType arena)
        {
            if (!Instance.m_SpecialConfigs.Any(t => t.Arena == arena))
                ErrorHandler.Warning("No special config was found for Arena : " + arena);

            return Instance.m_SpecialConfigs.Where(t => t.Arena == arena).FirstOrDefault();
        }

        #endregion


        #region PowerUp DropRates

        public static ERuneActivation SelectRandomActivation(int level)
        {
            if (level < 0)
            {
                ErrorHandler.Error("Bad Arena level (" + level + ") provided");
                level = 0;
            }

            if (Instance.m_PowerUpDropRates.Count <= level)
            {
                ErrorHandler.Error("Arena level (" + level + ")" + " >= number of PowerUpDropRates (" + Instance.m_PowerUpDropRates.Count + ")");
                return Instance.m_PowerUpDropRates.LastOrDefault().SelectRandomRarety();
            }

            return Instance.m_PowerUpDropRates[level].SelectRandomRarety();
        }

        #endregion


        #region Instance 

        public static ArenaManagementData Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    Load();
                }

                return s_Instance;
            }
        }


        static void Load()
        {
            s_Instance = AssetLoader.Load<ArenaManagementData>(AssetLoader.c_ManagementDataPath);
        }

        #endregion
    }
}