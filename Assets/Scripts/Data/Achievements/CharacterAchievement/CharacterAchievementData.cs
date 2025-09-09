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
    [CreateAssetMenu(fileName = "CharacterAchievementData", menuName = "Game/Achievements/CharacterAchievements/Default")]
    public class CharacterAchievementData : AchievementData
    {
        #region Members

        [SerializeField]
        protected ECharacter m_Character;
        [SerializeField]
        protected List<int> m_MasteryThresholds = new List<int>() { 3, 6 };

        public override string ID => m_Character.ToString() + base.ID;
        public ECharacter Character => m_Character;
        public int CurrentMastery => InventoryCloudData.Instance.GetCollectable(m_Character).Mastery;

        #endregion


        #region Unlock

        /// <summary>
        /// Check if achievement can be unlocked
        /// </summary>
        public override bool IsUnlockable
        {
            get
            {
                if (! IsMasteryUnlocked())
                    return false;

                return base.IsUnlockable;
            }
        }

        /// <summary>
        /// Check if character's mastery is unlocked for this level of achievement
        /// </summary>
        /// <returns></returns>
        public bool IsMasteryUnlocked()
        {
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


        #region Infos

        public override string GetDescription()
        {
            string description = base.GetDescription();
            if (!IsMasteryUnlocked())
                description += "\n<color=\"red\">Requires Mastery " + (CurrentMastery + 1).ToString() + " to be unlockable</color>";
            return description;
        }

        public SRewardsData GetAllRewardsAtMastery(int mastery)
        {
            var rewards = new SRewardsData();

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

        #endregion
    }
}