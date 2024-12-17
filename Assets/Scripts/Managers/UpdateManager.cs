using Analytics.Events;
using Data;
using Data.GameManagement;
using Enums;
using Managers.Friends;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Services.Authentication;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public static class UpdateManager
    {
        public static Version LastVersion => new Version(PlayerPrefs.GetString("LastVersion", "0.0.0"));
        public static Version CurrentVersion => new Version(Application.version);


        #region Updates Manipulators
        
        /// <summary>
        /// Contains all the necessary update moves for a new player
        /// </summary>
        /// <returns></returns>
        static void InitNewPlayer()
        {
            if (LastVersion.CompareTo(new Version("0.0.0")) == 0)
                return;

            FriendsHandler.SendFriendRequestToAll();
            SetVersion(Application.version);
        }

        /// <summary>
        /// Check if Player requires an Update
        /// </summary>
        public static void CheckUpdates()
        {
            if (LastVersion.CompareTo(new Version("0.0.0")) == 0)
            {
                InitNewPlayer();
                return;
            }

            while (LastVersion != CurrentVersion)
            {
                if (! UpdateVersion())
                {
                    ErrorHandler.Error($"Unable to udpate version {LastVersion} to {Application.version}");
                    return;
                }
            }
        }

        /// <summary>
        /// Update to next Version
        /// </summary>
        /// <returns></returns>
        public static bool UpdateVersion()
        {
            var test = true;
            if (LastVersion.CompareTo(CurrentVersion) == 0)
                return test;

            // reset PlayerPrefs settings on every new versions
            Settings.Reload();

            if (LastVersion.CompareTo(new Version("0.1.5")) == -1)
                test = UpdateVersion_0_1_5();

            if (LastVersion.CompareTo(new Version("0.1.6")) == -1)
                test = UpdateVersion_0_1_6();

            if (LastVersion.CompareTo(new Version("0.1.7")) == -1)
                test = UpdateVersion_0_1_7();

            if (LastVersion.CompareTo(new Version("0.1.8")) == -1)
                test = UpdateVersion_0_1_8();

            //if (LastVersion.CompareTo(new Version("0.1.11")) == -1)
            //    test = UpdateVersion_0_1_11();

            //if (LastVersion.CompareTo(new Version("0.1.12")) == -1)
            //    test = UpdateVersion_0_1_12();

            if (LastVersion.CompareTo(new Version("0.1.13")) == -1)
                test = UpdateVersion_0_1_13();

            if (LastVersion.CompareTo(new Version("0.2.0")) == 0)
                test = UpdateVersion_0_2_0();

            // if does not trigger any version until now, update to current version
            if (LastVersion.CompareTo(CurrentVersion) == -1)
                SetVersion(Application.version);

            return test;
        }

        /// <summary>
        /// Set updated version in settings
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        static bool SetVersion(string version)
        {
            if (LastVersion.CompareTo(new Version(version)) >= 0)
            {
                ErrorHandler.Error($"Trying to set new version {version} wich is <= current version {LastVersion}");
                return false;
            }

            Debug.Log($"Version Updated from {LastVersion} to {version}");
            PlayerPrefs.SetString("LastVersion", version);
            return true;
        }

        #endregion


        #region v0.1.5

        static bool UpdateVersion_0_1_5()
        {
            var test = true;

            // register GamerTag and Token
            MAnalytics.SendEvent(new PlayerDataEvent(ProfileCloudData.GamerTag, ProfileCloudData.Token, ProfileCloudData.Region));

            // save version
            if (!SetVersion("0.1.5"))
                test = false;

            return test;
        }

        #endregion


        #region v0.1.6

        static bool UpdateVersion_0_1_6()
        {
            var test = true;

            // update 
            UpdateAuthPlayerName();

            // send a friend request to every player
            FriendsHandler.SendFriendRequestToAll();

            // save version
            if (!SetVersion("0.1.6"))
                test = false;

            return test;
        }

        static async void UpdateAuthPlayerName()
        {
            // if Auth PlayerName already match Cloud data PlayerName, no need to do anything
            if (AuthenticationService.Instance.PlayerName == ProfileCloudData.PlayerName)
                return;

            // check if GamerTag still respect rules (skip availability check since it is the player's own pseudo)
            (bool isValid, string reason) = await ProfileCloudData.IsGamerTagValid(ProfileCloudData.GamerTag, checkAvailable: false);
            if (!isValid)
            {
                // reset gamer's tag before PopUp
                ProfileCloudData.ResetGamerTag();

                // store the change of the display PseudoPopUp for when the user will reach the MainMenu
                Main.AddStoredEvent(EAppState.MainMenu, () => Main.SetPopUp(EPopUpState.PseudoPopUp, "Your Pseudo no longer matches our rules for pseudos.\nPlease enter a new one"));

                return;
            }

            // set Auth PlayerName and save Tag
            string playerName = await AuthenticationService.Instance.UpdatePlayerNameAsync(ProfileCloudData.GamerTag);
            ProfileCloudData.SetTag("#"+playerName.Split("#")[1]);
        }



        #endregion


        #region v0.1.7

        static bool UpdateVersion_0_1_7()
        {
            var test = true;

            // update 
            UpdateAuthPlayerName();

            // send a friend request to every player
            FriendsHandler.SendFriendRequestToAll();

            // save version
            if (!SetVersion("0.1.7"))
                test = false;

            return test;
        }

        #endregion


        #region v0.1.8

        static bool UpdateVersion_0_1_8()
        {
            var test = true;

            // set ErrorHandler to false for everyone by default (too consuming)
            PlayerPrefsHandler.SetDebug(EDebugOption.ErrorHandler, false);
            ErrorHandler.IsActivated = false;

            // save version
            if (!SetVersion("0.1.8"))
                test = false;

            return test;
        }

        #endregion


        #region v0.1.11

        static bool UpdateVersion_0_1_11()
        {
            var test = true;

            // set IsTutoDone to true
            ProfileCloudData.Instance.SetData(ProfileCloudData.KEY_TUTO_DONE, true);

            // save version
            if (!SetVersion("0.1.11"))
                test = false;

            return test;
        }

        #endregion


        #region v0.1.12

        static bool UpdateVersion_0_1_12()
        {
            var test = true;

            // set IsTutoDone to true
            Main.CloudSaveManager.ResetAll();

            // save version
            if (! SetVersion("0.1.12"))
                test = false;

            return test;
        }

        #endregion


        #region v0.1.13

        static bool UpdateVersion_0_1_13()
        {
            var test = true;

            // DO NOT reset (for some testers)
            if (LastVersion.CompareTo(new Version("0.1.12")) == -1)
                Main.CloudSaveManager.ResetAll();

            var alphaTesterTitle = new SAchievementReward();
            alphaTesterTitle.Set(ETitle.Alpha_Tester);

            NotificationCloudData.AddMessage(new SMessage(
                title: "Alpha Tester Rewards",
                content: "Congratulation on having played the Alpha version of the game !\n\nAll data have been reseted, but here is a reward pack to help you start this new version !\nAlso, you gained the title \"AlphaTester\" that will be still be available on your account when the game launches.\n\nThank you for your contribution so far and i hope you'll enjoy this new patch !",
                rewardsData: new SRewardsData(
                    chests: new List<EChest>() {
                        EChest.Rare,
                        EChest.Rare
                    },
                    currencyRewards: new List<SCurrencyReward>() {
                        new SCurrencyReward(ECurrency.Xp, 250),
                        new SCurrencyReward(ECurrency.Golds, 5000),
                        new SCurrencyReward(ECurrency.Gems, 150)
                    },
                    achievementRewards: new List<SAchievementReward> {
                        alphaTesterTitle
                    }
                )
             ));

            // save version
            if (!SetVersion("0.1.13"))
                test = false;

            return test;
        }

        #endregion


        #region v0.2.0

        static bool UpdateVersion_0_2_0()
        {
            var test = true;

            // DO NOT reset (for some testers)
            if (LastVersion.CompareTo(new Version("0.1.12")) == -1)
                Main.CloudSaveManager.ResetAll();

            var alphaTesterTitle = new SAchievementReward();
            alphaTesterTitle.Set(ETitle.Alpha_Tester);

            NotificationCloudData.AddMessage(new SMessage(
                title: "Alpha Tester Rewards",
                content: "Congratulation on having played the Alpha version of the game !\n\nAll data have been reseted, but here is a reward pack to help you start this new version !\nAlso, you gained the title \"AlphaTester\" that will be still be available on your account when the game launches.\n\nThank you for your contribution so far and i hope you'll enjoy this new patch !",
                rewardsData: new SRewardsData(
                    chests: new List<EChest>() {
                        EChest.Rare,
                        EChest.Rare
                    },
                    currencyRewards: new List<SCurrencyReward>() {
                        new SCurrencyReward(ECurrency.Xp, 250),
                        new SCurrencyReward(ECurrency.Golds, 5000),
                        new SCurrencyReward(ECurrency.Gems, 150)
                    },
                    achievementRewards: new List<SAchievementReward> {
                        alphaTesterTitle
                    }
                )
             ));

            // save version
            if (! SetVersion("0.2.0"))
                test = false;

            return test;
        }

        #endregion


        #region Beta Launch



        #endregion
    }
}