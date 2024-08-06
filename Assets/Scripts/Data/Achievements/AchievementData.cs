using Assets;
using Data.GameManagement;
using Enums;
using Menu.Common.Buttons;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;

namespace Data
{
    [Serializable]
    public struct SAchievementReward
    {
        public EAchievementReward AchievementReward;

        [ConditionalField("AchievementReward", false, EAchievementReward.Title)]
        public ETitle   Title;
        [ConditionalField("AchievementReward", false, EAchievementReward.Avatar)]
        public EAvatar  Avatar;
        [ConditionalField("AchievementReward", false, EAchievementReward.Border)]
        public EBorder  Border;
        [ConditionalField("AchievementReward", false, EAchievementReward.Badge)]
        public EBadge   Badge;
        [ConditionalField("AchievementReward", false, EAchievementReward.Badge)]
        public ELeague  League;

        public void Set(Enum value)
        {
            // try to extract type from provided value
            if (! ProfileCloudData.TryGetType(value, out AchievementReward))
                return;

            // try to set value from string
            Value = value.ToString();
        }

        public Enum EnumValue
        {
            get
            {
                switch (AchievementReward)
                {
                    case EAchievementReward.Title:
                        return Title;
                    case EAchievementReward.Avatar:
                        return Avatar;
                    case EAchievementReward.Border:
                        return Border;
                    case EAchievementReward.Badge:
                        return Badge;

                    default:
                        ErrorHandler.Error("Unahandled case : " + AchievementReward);
                        return default;
                }
            }
        }

        public string Value
        {
            get
            {
                if (AchievementReward == EAchievementReward.Badge)
                    return ProfileCloudData.BadgeToString(Badge, League);

                if (EnumValue == null)
                {
                    ErrorHandler.Error("EnumValue is null - return");
                    return "";
                }

                return EnumValue.ToString();
            }

            set
            {
                switch (AchievementReward)
                {
                    case EAchievementReward.Title:
                        if (!Enum.TryParse(value, out Title))
                            break;
                        return;

                    case EAchievementReward.Avatar:
                        if (!Enum.TryParse(value, out Avatar))
                            break;
                        return;

                    case EAchievementReward.Border:
                        if (!Enum.TryParse(value, out Border))
                             break;
                        return;

                    case EAchievementReward.Badge:
                        if (!ProfileCloudData.TryGetBadgeFromString(value, out Badge, out League))
                            break;
                        return;

                    default:
                        ErrorHandler.Error("Unahandled case : " + AchievementReward);
                        return;
                }

                ErrorHandler.Error("Unable to set value " + value + " for achievement of type " + AchievementReward);
            }
        }
    }

    [Serializable]
    public struct SAchievementSubData
    {
        [Description("Treshold value that provides the reward")]
        public float                            MaxValue;

        //[Description("Rewards specific to achivements")]
        //public List<SAchievementReward>     AchivementRewardData;

        [Description("Extra rewards (golds, xp, chests, ...)")]
        public SRewardsData                     Rewards;

        public List<SAchievementReward> AchivementRewardData => Rewards.AchievementRewards;
    }

    [Serializable]
    public struct SAnalyticsFilter
    {
        public EAnalyticsParam      AnalyticParam;
        public string               Value;
        public EComparator          Comparator;
        
        public SAnalyticsFilter(EAnalyticsParam analyticParam, string value, EComparator comparator = EComparator.Equal)
        {
            AnalyticParam   = analyticParam;
            Value           = value;
            Comparator      = comparator;
        }

        public bool Check(object value, bool validateOnNull = false)
        {
            // CHECK : null value provided
            if (value == null)
                return validateOnNull;

            // CHECK :  equals first
            if (Comparator == EComparator.Equal)
                return Value.Equals(value.ToString());

            // CHECK : is numerical value
            if (! float.TryParse(value.ToString(), out float valueToCheck))
            {
                ErrorHandler.Error($"Using Comparator {Comparator} on a non numerical provided value {value}");
                return false;
            }

            if (! float.TryParse(Value, out float filterValue))
            {
                ErrorHandler.Error($"Using Comparator {Comparator} on a non numerical filter value {Value}");
                return false;
            }

            // switch case comparator
            switch (Comparator)
            {
                case EComparator.Inf:
                    return valueToCheck < filterValue;

                case EComparator.InfEq:
                    return valueToCheck <= filterValue;

                case EComparator.SupEq:
                    return valueToCheck > filterValue;

                case EComparator.Sup:
                    return valueToCheck >= filterValue;

                default:
                    ErrorHandler.Error("Unhandled case : " + Comparator);
                    return false;
            }
        }
    }

    [CreateAssetMenu(fileName = "Achievement", menuName = "Game/Achievements/Achievement")]
    public class AchievementData : ScriptableObject
    {
        #region Members

        [Description("Description informations of the Achievement")]
        public string Description;

        [Description("Stat to record")]
        public EAnalytics Analytics;

        [Description("Stat to record")]
        public List<SAnalyticsFilter> AnalyticsFilters;

        [Description("List of each sub-achiemevents linked to their rewards")]
        public List<SAchievementSubData> AchievementSubData;

        // ===========================================================================================
        // Dependent values
        public string Name => name;
        public float Count              => StatCloudData.GetAnalyticsCount(Analytics, AnalyticsFilters);
        public float RequestedValue     => Current == null ? 0 : Current.Value.MaxValue;
        public bool IsUnlockable        => Current != null && Count >= RequestedValue;

        public SAchievementSubData? Current
        {
            get
            {
                int index = ProfileCloudData.GetAchievementIndex(Name);
                if (index >= AchievementSubData.Count)
                    return null;

                return AchievementSubData[index];
            }
        }


        #endregion


        #region Unlocking

        public void Unlock()
        {
            if (! Current.HasValue)
            {
                ErrorHandler.Error("Current has no value");
                return;
            }    

            SAchievementSubData achievementData = Current.Value;

            // ACHIEVEMENT REWARDS (only)
            if (achievementData.AchivementRewardData.Count == achievementData.Rewards.Count) 
            {
                foreach (SAchievementReward data in achievementData.AchivementRewardData)
                {
                    ErrorHandler.Log("Unlocking Achievement Reward : " + data.AchievementReward + " - " + data.Value, ELogTag.Achievements);
                    ErrorHandler.Log("- data : ", ELogTag.Achievements);
                    ErrorHandler.Log("     + AchievementRewardType : " + data.AchievementReward, ELogTag.Achievements);
                    ErrorHandler.Log("     + Value : " + data.Value, ELogTag.Achievements);
                    ProfileCloudData.AddAchievementReward(data.AchievementReward, data.Value, false);
                }

                Main.DisplayAchievementRewards(achievementData.AchivementRewardData);
                ProfileCloudData.Instance.SaveValue(ProfileCloudData.KEY_ACHIEVEMENT_REWARDS);
            }

            // MULTIPLE REWARDS
            else if (!achievementData.Rewards.IsEmpty)
                Main.DisplayRewards(achievementData.Rewards, ERewardContext.Achievements.ToString());

            // save that the achievement was completed
            ProfileCloudData.CompleteAchievement(Name);
        }

        #endregion

    }
}