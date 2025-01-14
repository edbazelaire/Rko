using Enums;
using System;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Data.GameManagement
{
    [Serializable]
    public struct SRaretyDistribution
    {
        public ERarety Rarety;
        public SRaretyPercData[] RaretyPercData;
    }

    [Serializable]
    public struct SSubRewardsTypePerc
    {
        public ESubRewardType   SubRewardType;
        public float            Percentage;
    }

    [CreateAssetMenu(fileName = "LootManagementData", menuName = "Game/LootManagementData")]
    public class LootManagementData : ScriptableObject
    {
        protected static LootManagementData s_Instance;

        public SRaretyDistribution[] OrbRaretyDistribution;
        public SSubRewardsTypePerc[] OrbSubRewardsTypePercs;

        public static SRaretyPercData[] FindOrbRaretyPercData(ERarety rarety)
        {
            foreach (var raretyDistribution in Instance.OrbRaretyDistribution)
            {
                if (raretyDistribution.Rarety == rarety)
                    return raretyDistribution.RaretyPercData;
            }

            ErrorHandler.Error("Unable to find SRaretyDistribution matching rarety " + rarety + " for orbs");
            return default;
        }

        public static LootManagementData Instance
        {
            get
            {
                if (s_Instance == null)
                    Load();

                return s_Instance;
            }
        }

        static void Load()
        {
            s_Instance = AssetLoader.Load<LootManagementData>("LootManagementData", AssetLoader.c_ManagementDataPath);
        }

    }
}