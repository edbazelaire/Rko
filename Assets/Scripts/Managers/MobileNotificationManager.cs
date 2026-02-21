using Assets;
using Enums;
using Inventory;
using Save;
using System;
using Tools;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Schedules a single local notification for the next chest that becomes ready.
    /// </summary>
    public static class MobileNotificationManager
    {
        const int c_ChestReadyNotificationId = 1901;
        const string c_AndroidChannelId = "chest_ready_channel";

        static bool s_Initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void RuntimeInitialize()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (s_Initialized)
                return;

            s_Initialized = true;

#if UNITY_ANDROID
            var channel = new AndroidNotificationChannel
            {
                Id = c_AndroidChannelId,
                Name = "Chest notifications",
                Importance = Importance.Default,
                Description = "Notifications when a chest is ready to collect."
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif

            Main.InitializationCompletedEvent += OnInitializationCompleted;
            TimeCloudData.BoostChangedEvent += OnBoostChanged;
        }

        static void OnInitializationCompleted()
        {
            RefreshChestReadyNotification();
        }

        static void OnBoostChanged(string boostName, bool activate)
        {
            if (boostName != EBoost.ChestSpeedBoost.ToString())
                return;

            RefreshChestReadyNotification();
        }

        public static void RefreshChestReadyNotification()
        {
            if (!ShouldSendChestNotifications())
            {
                CancelChestReadyNotification();
                return;
            }

            long unlockTimestamp = GetNextChestUnlockTimestamp();
            if (unlockTimestamp <= 0)
            {
                CancelChestReadyNotification();
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long secondsUntilReady = Math.Max(1, unlockTimestamp - now);
            DateTime fireTime = DateTime.UtcNow.AddSeconds(secondsUntilReady);

            ScheduleChestReadyNotification(fireTime);
        }

        static bool ShouldSendChestNotifications()
        {
            if (!PlayerPrefsHandler.AreChestReadyNotificationsEnabled())
                return false;

            if (!CloudSaveManager.LoadingCompleted)
                return false;

            return true;
        }

        static long GetNextChestUnlockTimestamp()
        {
            var chests = InventoryManager.Chests;
            if (chests == null || chests.Length == 0)
                return -1;

            long minTimestamp = long.MaxValue;
            bool found = false;
            for (int i = 0; i < chests.Length; i++)
            {
                var chestData = chests[i];
                if (chestData == null || chestData.GetState() != EChestLockState.Unlocking)
                    continue;

                long unlockedAt = chestData.GetUnlockedTime();
                if (unlockedAt < minTimestamp)
                    minTimestamp = unlockedAt;
                found = true;
            }

            return found ? minTimestamp : -1;
        }

        static void ScheduleChestReadyNotification(DateTime fireTimeUtc)
        {
            CancelChestReadyNotification();

#if UNITY_ANDROID
            if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
            {
                var _ = new PermissionRequest();
            }

            var notification = new AndroidNotification
            {
                Title = "Chest ready!",
                Text = "Your chest is ready to be collected.",
                FireTime = fireTimeUtc
            };

            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, c_AndroidChannelId, c_ChestReadyNotificationId);
#endif

#if UNITY_IOS
            if (iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus != AuthorizationStatus.Authorized)
            {
                var _ = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true);
            }

            TimeSpan interval = fireTimeUtc - DateTime.UtcNow;
            if (interval.TotalSeconds < 1)
                interval = TimeSpan.FromSeconds(1);

            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = interval,
                Repeats = false
            };

            var notification = new iOSNotification
            {
                Identifier = c_ChestReadyNotificationId.ToString(),
                Title = "Chest ready!",
                Body = "Your chest is ready to be collected.",
                ShowInForeground = false,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                Trigger = trigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
#endif
        }

        static void CancelChestReadyNotification()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelScheduledNotification(c_ChestReadyNotificationId);
#endif

#if UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(c_ChestReadyNotificationId.ToString());
#endif
        }
    }
}
