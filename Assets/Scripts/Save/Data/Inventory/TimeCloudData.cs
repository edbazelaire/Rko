using Assets;
using Data;
using Data.GameManagement;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Services.CloudSave.Models;

namespace Save
{
    [Serializable]
    public struct STimeData
    {
        /// <summary> name or identifier of the item </summary>
        public string Name;
        /// <summary> number of allowed collection lefts on this item </summary>
        public int NCollectionLeft;
        /// <summary> timestamp (in sec) when this value resets </summary>
        public int ResetAt;
        /// <summary> to store extra data that might be necessary </summary>
        public string MetaData;

        public STimeData(string name, int nCollectionLeft, int resetAt = 0, string metaData = "")
        {
            Name                = name;
            NCollectionLeft     = nCollectionLeft;
            ResetAt             = resetAt;
            MetaData            = metaData;
        }

        public bool IsExpired()
        {
            return ResetIn() <= 0;
        }

        public int ResetIn()
        {
            // Get the current time in Unix epoch seconds
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, ResetAt - currentTimestamp);
        }

        public bool IsCollectable()
        {
            return NCollectionLeft > 0 && ! IsExpired();
        }
    }

    public class TimeCloudData : CloudData
    {
        #region Members

        public new static TimeCloudData Instance => Main.CloudSaveManager.GetCloudData(typeof(TimeCloudData)) as TimeCloudData;

        // ===============================================================================================
        // CONSTANTS
        public const string KEY_TIME_DATA       = "TimeData";

        public const string DAILY_SHOP_ID       = "DailyShopOffer_";
        public const string SPECIAL_OFFER_ID    = "SpecialOffer_";

        // ===============================================================================================
        // ACTIONS
        public static Action<string> TimeDataChangedEvent;

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_TIME_DATA,            new List<STimeData>() },
        };

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        public static List<STimeData> TimeData     => Instance.m_Data[KEY_TIME_DATA] as List<STimeData>;

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert SCharacterBuildsCloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <param name="charsBuildsList"></param>
        /// <returns></returns>
        protected override object Convert(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(List<STimeData>))
                return item.Value.GetAs<List<STimeData>>();

            return base.Convert(item);
        }

        #endregion


        #region General Accessors

        public static STimeData? GetTimeData(string name)
        {
            int index = GetTimeDataIndex(name);
            if (index == -1)
                return null;

            return TimeData[index];
        }

        public static int GetTimeDataIndex(string name)
        {
            for (int index = 0; index < TimeData.Count; index++)
            {
                if (TimeData[index].Name == name)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// Add or Update time data in the database
        /// </summary>
        /// <param name="timeData"></param>
        /// <param name="save"></param>
        public static void UpdateTimeData(STimeData timeData, bool save = true)
        {
            int index = GetTimeDataIndex(timeData.Name);
            if (index == -1)
                TimeData.Add(timeData);
            else
                TimeData[index] = timeData;

            TimeDataChangedEvent?.Invoke(timeData.Name);

            if (save)
                Instance.SaveValue(KEY_TIME_DATA);
        }

        /// <summary>
        /// Collect N data from a timeData
        /// </summary>
        /// <param name="name"></param>
        /// <param name="nCollections"></param>
        /// <returns></returns>
        public static bool CollectTimeData(string name, int nCollections = 1)
        {
            STimeData? timeData = GetTimeData(name);
            if (timeData == null)
            {
                ErrorHandler.Error("Unable to find timedata with name " + name);
                return false;
            }

            if (timeData.Value.NCollectionLeft < nCollections)
            {
                ErrorHandler.Error("Trying to collect " + nCollections + " on " + name + " but it has only " + timeData.Value.NCollectionLeft + " left");
                return false;
            }

            var newTimeData = timeData.Value;
            newTimeData.NCollectionLeft -= nCollections;
            UpdateTimeData(newTimeData);

            return true;
        }

        #endregion


        #region Daily Shop Offers

        public static STimeData? GetDailyShopOffer(int index)
        {
            return GetTimeData(GetDailyShopId(index));
        }

        public static STimeData GenerateNewDailyShopOffer(int index, ref List<ESpell> usedSpells)
        {
            if (index < 0 || index > ShopManagementData.DailyOffersRareties.Count)
            {
                ErrorHandler.Error("Bad index provided : " + index);
                return default;
            }

            ERarety rarety = index == 0 ? Instance.GetRandomRarety() : ShopManagementData.DailyOffersRareties[index - 1];

            return new STimeData(
                name:               GetDailyShopId(index), 
                nCollectionLeft:    1, 
                resetAt:            Instance.GetNextDayTimestamp(),
                metaData:           SSpellDistributionData.GenerateRandomSpell(rarety, ref usedSpells).ToString()
            );
        }

        #endregion


        #region Special Offers

        public static STimeData? GetSpecialShopOffer(string name)
        {
            return GetTimeData(GetSpecialShopOfferId(name));
        }

        public static STimeData GenerateSpecialShopOffer(SShopData data)
        {
            return new STimeData(
                name: GetSpecialShopOfferId(data.Name),
                nCollectionLeft: data.MaxCollection,
                resetAt: Instance.GetNextDayTimestamp(),
                metaData: ""
            );
        }

        public static STimeData GenerateSpecialShopOffer(string name)
        {
            foreach (var data in ShopManagementData.SpecialOffers)
            {
                if (! data.Name.Equals(name))
                    continue;

                // no max collection, no need to create a TimeCloudData
                if (data.MaxCollection <= 0)
                    return default;

                GenerateSpecialShopOffer(data);
            }

            ErrorHandler.Error("Unable to find ShopOffer");
            return default;
        }

        #endregion


        #region Time Management

        public int GetNextDayTimestamp()
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            // Calculate the start of the next day
            DateTime startOfNextDay = now.Date.AddDays(1);

            // Convert to Unix timestamp (seconds since the epoch)
            return (int)((DateTimeOffset)startOfNextDay).ToUnixTimeSeconds();
        }

        public int GetTimestampIn(int nSeconds)
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            // Calculate the start of the next day
            DateTime startOfNextDay = now.AddSeconds(nSeconds);

            // Convert to Unix timestamp (seconds since the epoch)
            return (int)((DateTimeOffset)startOfNextDay).ToUnixTimeSeconds();
        }

        #endregion


        #region Helpers

        public static string GetDailyShopId(int index)
        {
            return DAILY_SHOP_ID + index;
        }

        public static string GetSpecialShopOfferId(string name)
        {
            return SPECIAL_OFFER_ID + name;
        }

        public static int GetIndexDailyShop(string dailyShopId)
        {
            // Check if the string starts with DAILY_SHOP_ID
            if (! dailyShopId.StartsWith(DAILY_SHOP_ID))
            {
                ErrorHandler.Error("Trying to extract daily shop id from (" + dailyShopId + ") that does not starts with " + DAILY_SHOP_ID);
                return -1;
            }

            // Extract the part of the string after DAILY_SHOP_ID
            string indexString = dailyShopId.Substring(DAILY_SHOP_ID.Length);

            // Parse the extracted string to an integer
            if (! int.TryParse(indexString, out int index))
            {
                ErrorHandler.Error("Unable to extract index from " + dailyShopId);
                return -1;
            }

            return index;
        }

        ERarety GetRandomRarety()
        {
            var r = UnityEngine.Random.Range(0f, 1f);
            if (r <= 0.5)
                return ERarety.Common;

            if (r >= 0.9)
                return ERarety.Epic;

            return ERarety.Rare;
        }

        #endregion


        #region Default Data

        public override void Reset(string key, bool save = true)
        {
            base.Reset(key, save);

            switch (key)
            {
                case KEY_TIME_DATA:
                    m_Data[key] = new List<STimeData>();
                    CheckTimeData();
                    break;
            }

            if (save)
                Instance.SaveValue(key);
        }

        #endregion


        #region Checkers

        public bool CheckTimeData()
        {
            var test = CheckDailyShopOffers()
                || CheckSpecialShopOffers();
            
            if (test)
                Save();

            return test;
        }

        bool CheckDailyShopOffers()
        {
            bool updated = false;
            List<ESpell> usedSpells = new List<ESpell>();
            for (int dailyOfferIndex = 0; dailyOfferIndex < ShopManagementData.DailyOffersRareties.Count + 1; dailyOfferIndex++)
            {
                STimeData? data = GetTimeData(GetDailyShopId(dailyOfferIndex));
                if (data == null || data.Value.IsExpired())
                {
                    UpdateTimeData(GenerateNewDailyShopOffer(dailyOfferIndex, ref usedSpells), false);
                    updated = true;
                }
            }

            return updated;
        }

        bool CheckSpecialShopOffers()
        {
            // TODO =====================================================================
            // check current data (+ delete non existant)
            // TODO =====================================================================

            bool updated = false;

            // add missing data
            foreach (SShopData shopData in ShopManagementData.SpecialOffers)
            {
                // no max collection, no need to create a TimeCloudData
                if (shopData.MaxCollection <= 0)
                    continue;

                STimeData? timeData = GetSpecialShopOffer(shopData.Name);
                if (timeData == null || timeData.Value.IsExpired())
                {
                    UpdateTimeData(GenerateSpecialShopOffer(shopData), false);
                    updated = true;
                }
            }

            return updated;
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
                case KEY_TIME_DATA:
                    CheckTimeData();
                    break;
            }
        }

        #endregion
    }
}