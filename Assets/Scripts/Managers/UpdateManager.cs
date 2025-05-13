using Analytics.Events;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Google.Apis.Sheets.v4.Data;
using Inventory;
using Managers.Friends;
using Menu.Common.Buttons;
using MyBox;
using NUnit.Framework.Internal;
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
        /// <summary> player's current installed version </summary>
        public static Version CurrentVersion => new Version(PlayerPrefs.GetString("LastVersion", "0.0.0"));
        /// <summary> game's expected version </summary>
        public static Version GameVersion => new Version(Application.version);


        #region Updates Manipulators
        
        /// <summary>
        /// Contains all the necessary update moves for a new player
        /// </summary>
        /// <returns></returns>
        static void InitNewPlayer()
        {
            SetVersion(GameVersion.ToString());
        }

        /// <summary>
        /// Check if Player requires an Update
        /// </summary>
        public static void CheckUpdates()
        {
            if (CurrentVersion.CompareTo(new Version("0.0.0")) == 0)
            {
                InitNewPlayer();
                return;
            }

            while (CurrentVersion != GameVersion)
            {
                if (! UpdateVersion())
                {
                    ErrorHandler.Error($"Unable to udpate version {CurrentVersion} to {Application.version}");
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
            if (CurrentVersion.CompareTo(GameVersion) == 0)
                return test;

            // reset PlayerPrefs settings on every new versions
            Settings.Reload();

            if (CurrentVersion.CompareTo(new Version("0.1.5")) == -1)
                test = UpdateVersion_0_1_5();

            if (CurrentVersion.CompareTo(new Version("0.1.6")) == -1)
                test = UpdateVersion_0_1_6();

            if (CurrentVersion.CompareTo(new Version("0.1.7")) == -1)
                test = UpdateVersion_0_1_7();

            if (CurrentVersion.CompareTo(new Version("0.1.8")) == -1)
                test = UpdateVersion_0_1_8();

            if (CurrentVersion.CompareTo(new Version("0.2.0")) == -1)
                test = UpdateVersion_0_2_0();

            if (CurrentVersion.CompareTo(new Version("0.2.5")) == -1)
                test = UpdateVersion_0_2_5();

            if (CurrentVersion.CompareTo(new Version("0.3.0")) == -1)
                test = UpdateVersion_0_3_0();

            if (CurrentVersion.CompareTo(new Version("0.3.1")) == -1)
                test = UpdateVersion_0_3_1();

            // if does not trigger any version until now, update to current version
            if (CurrentVersion.CompareTo(GameVersion) == -1)
                SetVersion(Application.version);

            // on updates - check if there were changes in achievements that needs to be provided to the player
            CheckAchievements();

            return test;
        }

        /// <summary>
        /// Set updated version in settings
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        static bool SetVersion(string version)
        {
            if (CurrentVersion.CompareTo(new Version(version)) >= 0)
            {
                ErrorHandler.Error($"Trying to set new version {version} wich is <= current version {CurrentVersion}");
                return false;
            }

            Debug.Log($"Version Updated from {CurrentVersion} to {version}");
            PlayerPrefs.SetString("LastVersion", version);
            return true;
        }

        /// <summary>
        /// Check if there were rewards in Achivements that has been changed during last update
        /// </summary>
        static void CheckAchievements()
        {
            SRewardsData missingRewards = new SRewardsData();
            missingRewards.SetDefaultData();

            foreach (AchievementData achievementData in AchievementLoader.Achievements)
            {
                int currentIndex = ProfileCloudData.GetAchievementIndex(achievementData.Name);

                // check all achievements so far to see if any reward is missing
                for (int i = 0; i < currentIndex; i++)
                {
                    var rewards = achievementData.AchievementSubData[i].Rewards;

                    // only check AchievementRewards data
                    if (rewards.AchievementRewards.IsNullOrEmpty())
                        continue;

                    foreach (SAchievementReward achievementReward in rewards.AchievementRewards)
                    {
                        // already unlocked - skip
                        if (ProfileCloudData.HasAchievementReward(achievementReward.AchievementReward, achievementReward.Value))
                            continue;

                        missingRewards.Add(achievementReward);
                    }
                }
            }

            if (missingRewards.IsEmpty)
                return;

            NotificationCloudData.AddMessage(new SMessage()
            {
                Title = "Achivement Rewards",
                Content = "We've made some exciting updates to the Achievement rewards!\nTake a look at the new rewards you've earned so far.",
                RewardsData = missingRewards
            });
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
            if (! isValid)
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
            CloudSaveManager.Instance.ResetAll();

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
            if (CurrentVersion.CompareTo(new Version("0.1.12")) == -1)
                CloudSaveManager.Instance.ResetAll();

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
                        new SCurrencyReward(ECurrency.Gold, 5000),
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
            if (CurrentVersion.CompareTo(new Version("0.1.12")) == -1)
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
                        new SCurrencyReward(ECurrency.Xp,       250),
                        new SCurrencyReward(ECurrency.Gold,    5000),
                        new SCurrencyReward(ECurrency.Gems,     150)
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

        static bool UpdateVersion_0_2_5()
        {
            var test = true;

            // save version
            if (!SetVersion("0.2.5"))
                return false;

            // clear current messagerie
            NotificationCloudData.ClearMessages();

            if (ProfileCloudData.AccountLevel < 3)
                return test;

            int missingXp = 0;
            if (ProfileCloudData.AccountLevel >= 3)
                missingXp += 500;
            if (ProfileCloudData.AccountLevel >= 4)
                missingXp += 1000;
            if (ProfileCloudData.AccountLevel >= 5)
                missingXp += 4000;
            if (ProfileCloudData.AccountLevel >= 6)
                missingXp += 10000;
            if (ProfileCloudData.AccountLevel >= 7)
                missingXp += 22500;
            if (ProfileCloudData.AccountLevel >= 8)
                missingXp += 40000;
            if (ProfileCloudData.AccountLevel >= 9)
                missingXp += 85000;

            // send reward of missing xp to player
            NotificationCloudData.AddMessage(new SMessage(
                title: "Account level XP",
                content: "Greatings Player !\n\nSince the account level curve was re-adjusted, here is the xp that was consumed by the previous system.",
                rewardsData: new SRewardsData(
                    currencyRewards: new List<SCurrencyReward>() {
                        new SCurrencyReward(ECurrency.Xp, missingXp),
                    }
                )
            ));

            if ( ProfileCloudData.PlayerName == "Subrogue")
            {

                var avatar = new SAchievementReward();
                avatar.Set(EAvatar.Assassin);
                var border = new SAchievementReward();
                border.Set(EBorder.Assassin);
                var title = new SAchievementReward();
                title.Set(ETitle.The_Shadow);
                var badge = new SAchievementReward();
                badge.Set(EBadge.DemonLord);

                // send reward of missing xp to player
                NotificationCloudData.AddMessage(new SMessage(
                    title: "Uniques Rewards",
                    content: "",
                    rewardsData: new SRewardsData(
                        achievementRewards: new List<SAchievementReward>() {
                            avatar,
                            border,
                            title,
                            badge,
                        }
                    )
               ));
            }

            return test;
        }

        #endregion


        #region v0.3.0

        static bool UpdateVersion_0_3_0()
        {
            RenameGoldKey();
            return true;
        }

        static async void RenameGoldKey()
        {
            int oldValue = await InventoryCloudData.Instance.TryGet<int>("Golds");
            if (oldValue == 0)
                return;

            InventoryCloudData.Instance.SetCurrency(ECurrency.Gold, oldValue);
            InventoryCloudData.Instance.DeleteKey("Golds");

            Debug.Log($"[UpdateManager] Successfully updated 'Golds' → '{ECurrency.Gold}' with value {oldValue}.");
        }


        #endregion


        #region v0.3.1

        static bool UpdateVersion_0_3_1()
        {
            // check if the version should be updated
            if (GameVersion.CompareTo(new Version("0.3.1")) == -1)
                return true;

            AddMissingRankedRewards();
            SetVersion("0.3.1");
            return true;
        }

        static void AddMissingRankedRewards()
        {
            SRewardsData rewards = new SRewardsData();
            foreach (SLeagueData leagueData in Main.LeagueDataConfig.LeagueDataList)
            {
                if (leagueData.League >= ProgressionCloudData.CurrentLeague)
                    break;
                rewards.Add(leagueData.LevelData[^1].Rewards);
            }

            if (rewards.Count == 0)
                return;

            // send reward of missing xp to player
            NotificationCloudData.AddMessage(new SMessage(
                title: "Missing Ranked Rewards",
                content: "In the last patch, there was an issue affecting some rewards in Ranked mode. The problem has now been resolved.\n\nHere are the rewards you should have received.",
                rewardsData: rewards
            ));
        }

        #endregion


        #region Beta Launch



        #endregion
    }
}