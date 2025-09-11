using Assets;
using Data;
using Data.GameManagement;
using Enums;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Data
{
    public interface IAchievement
    {
        // ===========================================================================================
        // Dependent values
        #region Common
        public string GetID();
        public string GetName();
        public float RequestedValue => Current == null ? 0 : Current.MaxValue;
        public virtual bool IsUnlockable => Current != null && GetCount() >= RequestedValue && IsMasteryUnlocked();
        public int CurrentIndex => ProfileCloudData.GetAchievementThresholdIndex(GetID());
        public float TresholdValue => GetCurrent() != null ? Current.MaxValue : 0f;
        #endregion

        #region Mastery
        public bool IsMastery => IsCharacterMastery;
        public bool IsCharacterMastery => Character != ECharacter.None;
        public int CurrentMastery => InventoryCloudData.Instance.GetCollectable(Character).Mastery;
        #endregion

        public virtual float GetCount() => ProfileCloudData.GetAchievementInfo(GetID()).Count;

        public ECharacter Character => GetCharacter();
        public ECharacter GetCharacter();

        public SAchievementSubData Current => GetCurrent();
        public SAchievementSubData GetCurrent();

        public List<SAchievementSubData> AchievementSubData => GetAchievementSubData();
        public List<SAchievementSubData> GetAchievementSubData();


        #region Check

        public virtual void Check() { }

        #endregion


        #region Count Management

        public virtual void Increase(float count = 1) {
            UpdateCount(GetCount() + count);
        }

        public virtual void UpdateCount(float count)
        {
            ProfileCloudData.UpdateAchievementCount(GetID(), count);
        }

        #endregion


        #region Unlocking

        public void Unlock() { }

        /// <summary>
        /// Check if character's mastery is unlocked for this level of achievement
        /// </summary>
        /// <returns></returns>
        public bool IsMasteryUnlocked() => true;

        #endregion


        #region Description

        public virtual string GetDescription() => "";

        public virtual string CleanDescription(string baseDescription)
        {
            return baseDescription.Replace("[TresholdValue]", TextHandler.FormatNumericalString((int)TresholdValue));
        }

        public SRewardsData GetAllRewardsAtMastery(int mastery) => new SRewardsData();

        #endregion
    }
}

    public class AchievementData<T> : ScriptableObject, IAchievement where T : SAchievementSubData
    {
        #region Members

        [Header("Info")]
        [SerializeField] string m_Description;
        [SerializeField] bool m_ResetCount = false;

        [Header("Mastery")]
        [SerializeField]
        protected ECharacter m_Character = ECharacter.None;
        [SerializeField, Tooltip("Tresholds of each index allowed by Nth Mastery")]
        protected List<int> m_MasteryThresholds = new List<int>() { 3, 6 };

        [Header("Rewards")]
        [Tooltip("List of each sub-achiemevents linked to their rewards")]
        public List<T> AchievementSubData;

        // ===========================================================================================
        // Dependent values
        #region Common
        public virtual string ID            => IsCharacterMastery ? m_Character.ToString() + "_" + Name : Name;
        public string Name                  => name;
        public float RequestedValue         => Current == null ? 0 : Current.MaxValue;
        public virtual bool IsUnlockable    => Current != null && GetCount() >= RequestedValue && IsMasteryUnlocked();
        public int CurrentIndex             => ProfileCloudData.GetAchievementThresholdIndex(ID);
        public float TresholdValue          => Current != null ? Current.MaxValue : 0f;
        #endregion

        #region Mastery
        public bool IsMastery               => IsCharacterMastery;
        public bool IsCharacterMastery      => m_Character != ECharacter.None;
        public ECharacter Character         => m_Character;
        public int CurrentMastery           => InventoryCloudData.Instance.GetCollectable(m_Character).Mastery;
        #endregion

        public virtual float GetCount() => ProfileCloudData.GetAchievementInfo(ID).Count;
        public virtual float GetCountAtIndex(int index) => ProfileCloudData.GetAchievementInfo(ID).GetCountAtIndex(index);

        public T Current
        {
            get
            {
                if (CurrentIndex >= AchievementSubData.Count)
                    return null;

                return AchievementSubData[CurrentIndex];
            }
        }


        #endregion


        #region Check

        public virtual void Check() { }

        #endregion


        #region Count Management

        public virtual void Increase(float count = 1)
        {
            UpdateCount(GetCount() + count);
        }

        public virtual void IncreaseAtIndex(int index, float count = 1)
        {
            UpdateCountAtIndex(index, GetCountAtIndex(index) + count);
        }

        public virtual void UpdateCount(float count)
        {
            ProfileCloudData.UpdateAchievementCount(ID, count);
        }

        public virtual void UpdateCountAtIndex(int index, float count)
        {
            ProfileCloudData.UpdateAchievementCount(ID, count, index);
        }

        #endregion


        #region Unlocking

        public void Unlock()
        {
            if (Current == null)
            {
                ErrorHandler.Error("Current has no value");
                return;
            }

            SAchievementSubData achievementData = Current;

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
            ProfileCloudData.CompleteAchievement(ID, m_ResetCount);
        }

        /// <summary>
        /// Check if character's mastery is unlocked for this level of achievement
        /// </summary>
        /// <returns></returns>
        public bool IsMasteryUnlocked()
        {
            // NO MASTERY : return true
            if (!IsMastery)
                return true;

            // get mastery from cloud data
            var charCloudData = InventoryCloudData.Instance.GetCollectable(m_Character);

            // no thresholds OR current mastery > max threshold - means no restrictions
            if (m_MasteryThresholds.IsNullOrEmpty() || charCloudData.Mastery > m_MasteryThresholds.Count)
                return true;

            // no mastery - exit
            if (charCloudData.Mastery <= 0)
                return false;

            // current index must be below the threshold for next mastery
            return CurrentIndex < m_MasteryThresholds[charCloudData.Mastery - 1];
        }

        #endregion


        #region Description

        public virtual string GetDescription()
        {
            string description = CleanDescription(m_Description);
            if (!IsMasteryUnlocked())
                description += "\n<color=\"red\">Requires Mastery " + (CurrentMastery + 1).ToString() + " to be unlockable</color>";
            return description;
        }

        public virtual string CleanDescription(string baseDescription)
        {
            return baseDescription.Replace("[TresholdValue]", TextHandler.FormatNumericalString((int)TresholdValue));
        }

        public SRewardsData GetAllRewardsAtMastery(int mastery)
        {
            var rewards = new SRewardsData();

            // CHECK : is actually mastery
            if (!IsMastery)
            {
                ErrorHandler.Warning("Trying to get mastery rewards on a non-mastery achivement : " + ID);
                return rewards;
            }

            // CHECK : > 0
            if (mastery <= 0)
            {
                ErrorHandler.Error("Bad mastery provided, must be > 0 - " + mastery);
                return rewards;
            }

            // CHECK : Has sub data
            if (AchievementSubData.IsNullOrEmpty())
            {
                ErrorHandler.Error("Achievement " + Name + " has no sub data");
                return rewards;
            }

            // CHECK : not too big
            if (mastery > m_MasteryThresholds.Count + 1)
            {
                ErrorHandler.Warning("mastery requested (" + mastery + ") is > number of mastery thresholds + 1" + (m_MasteryThresholds.Count + 1));
            }

            // calculate min/max indexes of rewards for this mastery
            int minIndex = 0;
            int maxIndex = AchievementSubData.Count;
            if (mastery >= 2)
            {
                minIndex = m_MasteryThresholds[mastery - 2];
            }
            if (mastery <= m_MasteryThresholds.Count)
            {
                maxIndex = m_MasteryThresholds[mastery - 1];
            }
            // make sure maxIndex does not go ever N subData
            maxIndex = Math.Min(maxIndex, AchievementSubData.Count);

            // add rewards between min/max index
            for (int i = minIndex; i < maxIndex; i++)
            {
                rewards.Add(AchievementSubData[i].Rewards);
            }

            return rewards;
        }

    public string GetID()
    {
        return IsCharacterMastery ? m_Character.ToString() + "_" + GetName() : GetName();
    }

    public string GetName()
    {
        return name;
    }

    public ECharacter GetCharacter()
    {
        return m_Character;
    }

    public SAchievementSubData GetCurrent()
    {
        if (CurrentIndex >= AchievementSubData.Count)
            return null;

        return AchievementSubData[CurrentIndex];
    }

    public List<SAchievementSubData> GetAchievementSubData()
    {
        return AchievementSubData as List<SAchievementSubData>;
    }

    #endregion
}