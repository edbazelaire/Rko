using Enums;
using Save;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "AnalyticsAchievementData", menuName = "Game/Achievements/Analytics_Achievement")]
    public class AnalyticsAchievementData : AchievementData
    {
        #region Members

        [Description("Stat to record")]
        public EAnalytics Analytics;

        [Description("Stat to record")]
        public List<SAnalyticsFilter> AnalyticsFilters;

        // ===========================================================================================
        // Dependent values
        public override float GetCount() => StatCloudData.GetAnalyticsCount(Analytics, AnalyticsFilters);

        #endregion


        #region Unlocking

        #endregion

    }
}