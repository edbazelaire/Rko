using Assets;
using Data;
using Enums;
using MyBox;
using Save.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Services.CloudSave.Models;

namespace Save
{
    public class StatCloudData : CloudData
    {
        #region Members

        public new static StatCloudData Instance => CloudSaveManager.Instance.GetCloudData(typeof(StatCloudData)) as StatCloudData;

        // ===============================================================================================
        // CONSTANTS
        public const string KEY_ANALYTICS = "Analytics";

        // ===============================================================================================
        // ACTIONS
        public static Action<EAnalytics> AnalyticsDataChanged;

        // ===============================================================================================
        // DATA
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { EAnalytics.GameEnded.ToString(),              new List<SGameEndedCloudData>()         },
            { EAnalytics.CurrencyChanged.ToString(),        new List<SCurrencyEventCloudData>()     },
            { EAnalytics.InGame.ToString(),                 new List<SInGameEventCloudData>()       },
        };

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        public static List<SGameEndedCloudData>     GameEndedData   => Instance.m_Data[EAnalytics.GameEnded.ToString()] as List<SGameEndedCloudData>;
        public static List<SCurrencyEventCloudData> CurrencyData    => Instance.m_Data[EAnalytics.CurrencyChanged.ToString()] as List<SCurrencyEventCloudData>;
        public static List<SInGameEventCloudData>   InGameData      => Instance.m_Data[EAnalytics.InGame.ToString()] as List<SInGameEventCloudData>;

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert SCharacterBuildsCloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <param name="charsBuildsList"></param>
        /// <returns></returns>
        protected override object BaseConversion(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(List<SGameEndedCloudData>))
                return item.Value.GetAs<List<SGameEndedCloudData>>();

            if (m_Data[item.Key].GetType() == typeof(List<SCurrencyEventCloudData>))
                return item.Value.GetAs<List<SCurrencyEventCloudData>>();

            if (m_Data[item.Key].GetType() == typeof(List<SInGameEventCloudData>))
                return item.Value.GetAs<List<SInGameEventCloudData>>();

            return base.BaseConversion(item);
        }

        #endregion


        #region Accessors

        public static int GetAnalyticsCount(EAnalytics analytics, List<SAnalyticsFilter> filters = default)
        {
            int count = 0;

            foreach (SAnalyticsData data in GetAnalytics(analytics))
            {
                if (data.CheckFilters(filters))
                    count += data.Count;
            }

            ErrorHandler.Log(() => $"Count of {analytics} : " + count, ELogTag.StatCloudData);
            return count;
        }

        public static List<SAnalyticsData> GetAnalytics(EAnalytics analytics)
        {
            if (!Instance.m_Data.ContainsKey(analytics.ToString()))
            {
                ErrorHandler.Warning("Unable to find analytics " + analytics + " in cloud data");
                return new List<SAnalyticsData>(); // Return an empty list if key is not found
            }

            var data = Instance.m_Data[analytics.ToString()];
            // Convert the list items to SAnalyticsData using LINQ
            if (data is IEnumerable<SAnalyticsData> dataList)
            {
                // Select and convert each item to SAnalyticsData
                List<SAnalyticsData> convertedList = dataList.Cast<SAnalyticsData>().ToList();
                return convertedList;
            }

            ErrorHandler.Warning("Analytics data of " + analytics + " is not of expected type");
            return new List<SAnalyticsData>(); // Return an empty list if type is unexpected
        }

        public static void AddAnalytics(EAnalytics analytics, SAnalyticsData data) 
        {
            var analyticsData = GetAnalytics(analytics);

            // find matching data in the list of data
            int index = analyticsData.FirstIndex(d => d.Equals(data));
            if (index < 0)
            {
                // No match found, add new data
                analyticsData.Add(data);
            }
            else
            {
                // Match found, increase count
                analyticsData[index].Count += data.Count;
            }

            // save and fire event
            Instance.m_Data[analytics.ToString()] = analyticsData;
            Instance.SaveValue(analytics.ToString());
            AnalyticsDataChanged?.Invoke(analytics);
        }

        #endregion


        #region Default Data

        public override void Reset(string key, bool save = true)
        {
            base.Reset(key, save);

            Instance.m_Data[key] = new List<SCurrencyEventCloudData>();

            if (save)
                Instance.SaveValue(key);
        }

        #endregion


        #region Checkers


        #endregion


        #region Listeners

        protected override void OnCloudDataKeyLoaded(string key)
        {
            base.OnCloudDataKeyLoaded(key);

            // default check
            if (Instance.m_Data[key] == null)
            {
                Reset(key, false);
            }
        }

        #endregion
    }
}