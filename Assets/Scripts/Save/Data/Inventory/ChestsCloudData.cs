using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Services.CloudSave.Models;

namespace Save
{
    [Serializable]
    public class ChestData
    {
        /// <summary> type of chest </summary>
        public EChest ChestType;
        /// <summary> timestamp (in seconds) when the chest will be available </summary>
        public long UnlockedAt;

        public ChestData(EChest chestType, long unlockedAt = 0)
        {
            ChestType = chestType;
            UnlockedAt = unlockedAt;
        }

        public long GetUnlockedTime()
        {
            var time = UnlockedAt;

            // (Special Case) CHEST SPEED BOOST : Divides the "unlock time" by 2
            if (TimeCloudData.HasBoost(EBoost.ChestSpeedBoost))
                time = UnlockedAt - (ItemLoader.GetChestRewardData(ChestType).UnlockTime / 2);

            return time;
        }

        public void SetUnlockTime()
        {
            UnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ItemLoader.GetChestRewardData(ChestType).UnlockTime;
        }

        public EChestLockState GetState()
        {
            if (UnlockedAt == 0)
                return EChestLockState.Locked;

            if (GetUnlockedTime() > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return EChestLockState.Unlocking;

            return EChestLockState.Ready;
        }
    }

    /// <summary>
    /// Accessor of chests data
    /// </summary>
    public class ChestsCloudData : CloudData
    {
        #region Members

        public new static ChestsCloudData Instance => CloudSaveManager.Instance.GetCloudData(typeof(ChestsCloudData)) as ChestsCloudData;

        public const string KEY_CHESTS = "Chests";

        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>()
        {
            { KEY_CHESTS, new ChestData[4] { null, null, null, null }  }
        };

        #endregion


        #region Inherited Manipulators

        protected override object Convert(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(ChestData[]))
                return item.Value.GetAs<ChestData[]>();

            return base.Convert(item);
        }

        #endregion


        #region Public Accessor

        public static bool IsChestWaitingUnlock()
        {
            foreach (ChestData chestData in (Instance.m_Data[KEY_CHESTS] as ChestData[]))
            {
                if (chestData != null && chestData.GetState() == EChestLockState.Unlocking)
                    return true;
            }

            return false;
        }

        #endregion


        #region Reset & Unlock

        public override void Reset(string key, bool save = true)
        {
            base.Reset(key, save);

            switch (key)
            {
                case KEY_CHESTS:
                    m_Data[KEY_CHESTS] = new ChestData[4] { null, null, null, null };
                    break;

                default:
                    ErrorHandler.Warning("Unhandled key : " + key);
                    return;
            }

            if (save)
                Instance.SaveValue(key);
        }

        #endregion

    }
}