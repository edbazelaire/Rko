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

    [CreateAssetMenu(fileName = "ArenaManagementData", menuName = "Game/Management/ArenaManagementData")]
    public class ArenaManagementData : ScriptableObject
    {
        #region Members

        static ArenaManagementData s_Instance;

        [SerializeField] private int m_NDifficultyLevels = 2;
        [SerializeField] private List<SPowerUpDropRate> m_PowerUpDropRates;

        public static List<SPowerUpDropRate> PowerUpDropRates => Instance.m_PowerUpDropRates;
        public static int NDifficultyLevels => Instance.m_NDifficultyLevels;

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