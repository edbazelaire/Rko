using Assets;
using Enums;
using Save;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "AchievementData", menuName = "Game/Achievements/Achievement")]
    public class AchievementData : ScriptableObject
    {
        #region Members

        [SerializeField]
        string m_Description;

        [Tooltip("List of each sub-achiemevents linked to their rewards")]
        public List<SAchievementSubData> AchievementSubData;

        // ===========================================================================================
        // Dependent values
        public virtual string   ID                  => Name;
        public string           Name                => name;
        public float            RequestedValue      => Current == null ? 0 : Current.Value.MaxValue;
        public virtual bool     IsUnlockable        => Current != null && GetCount() >= RequestedValue;
        public int              CurrentIndex        => ProfileCloudData.GetAchievementThresholdIndex(ID);
        public float            TresholdValue       => Current.HasValue ? Current.Value.MaxValue : 0f;

        public virtual float GetCount() => ProfileCloudData.GetAchievementInfo(ID).Count;

        public SAchievementSubData? Current
        {
            get
            {
                if (CurrentIndex >= AchievementSubData.Count)
                    return null;

                return AchievementSubData[CurrentIndex];
            }
        }


        #endregion


        #region Count Management

        public virtual void Increase(float count = 1)
        {
            UpdateCount(GetCount() + count);
        }

        public virtual void UpdateCount(float count)
        {
            ProfileCloudData.UpdateAchievementCount(ID, count);
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
                    ProfileCloudData.AddAchievementReward(data.AchievementReward, data.Value, false);
                }

                Main.DisplayAchievementRewards(achievementData.AchivementRewardData);
                ProfileCloudData.Instance.SaveValue(ProfileCloudData.KEY_ACHIEVEMENT_REWARDS);
            }

            // MULTIPLE REWARDS
            else if (!achievementData.Rewards.IsEmpty)
                Main.DisplayRewards(achievementData.Rewards, ERewardContext.Achievements.ToString());

            // save that the achievement was completed
            ProfileCloudData.CompleteAchievement(ID);
        }

        #endregion


        #region Description

        public virtual string GetDescription()
        {
            return CleanDescription(m_Description);
        }

        public virtual string CleanDescription(string baseDescription)
        {
            return baseDescription.Replace("[TresholdValue]", TextHandler.FormatNumericalString((int)TresholdValue));
        }

        #endregion
    }
}