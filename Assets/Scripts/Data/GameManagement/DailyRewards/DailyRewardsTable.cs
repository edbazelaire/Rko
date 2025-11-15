using Enums;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.ComponentModel;
using Tools;
using Inventory;
using Save.Data;
namespace Data.GameManagement
{
    [CreateAssetMenu(fileName = "DailyRewardsTable", menuName = "Game/Daily Rewards Table")]
    public class DailyRewardsTable : ScriptableObject
    {
        #region Members

        [Header("Weekly Rewards")]
        public int NDays = 5;
        public List<SRewardsData> m_WeeklyRewards = new();

        static DailyRewardsTable s_Instance;

        public static DailyRewardsTable Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = AssetLoader.Load<DailyRewardsTable>("DailyRewardsTable", AssetLoader.c_ManagementDataPath);
                return s_Instance;
            }
        }

        #endregion


        #region Rewards Generation

        public static List<SRewardsData> GenerateWeeklyReward(int streak)
        {
            List<SRewardsData> dailyRewards = new();
            for (int i = 0; i < Instance.NDays - 1; i++)
            {
                dailyRewards.Add(GenerateDailyReward(i));
            }

            // add final weekly reward
            dailyRewards.Add(GenerateFinalReward(streak));
            return dailyRewards;
        }

        public static SRewardsData GenerateDailyReward(int index)
        {
            SRewardsData data = new SRewardsData();
            data.SetDefaultData();

            if (index == 0)
                data.Currencies.Add(new SCurrencyReward(ECurrency.Keys, 1));
            else if (index == 1)
                data.Currencies.Add(new SCurrencyReward(ECurrency.Gold, 1000));
            else if (index == 2)
                data.Currencies.Add(new SCurrencyReward(ECurrency.Gems, 15));
            else
                data.Currencies.Add(new SCurrencyReward(ECurrency.Keys, 3));

            return data;
        }

        private static SRewardsData GenerateFinalReward(int streak)
        {
            return Instance.m_WeeklyRewards[Math.Min(streak, Instance.m_WeeklyRewards.Count - 1)];
        }

        #endregion

    }
}