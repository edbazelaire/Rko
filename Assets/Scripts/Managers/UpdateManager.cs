using Analytics.Events;
using Data.GameManagement;
using Enums;
using Managers.Friends;
using MyBox;
using NUnit.Framework.Internal;
using Save;
using System.Collections.Generic;
using Tools;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public static class UpdateManager
    {
        public static bool IsNewPlayer => LastVersion == "0.0.0" && ! ProfileCloudData.PseudoChanged;
        public static string LastVersion => PlayerPrefs.GetString("LastVersion", "0.0.0");


        #region Updates Manipulators
        
        /// <summary>
        /// Contains all the necessary update moves for a new player
        /// </summary>
        /// <returns></returns>
        static void InitNewPlayer()
        {
            FriendsHandler.SendFriendRequestToAll();
            SetVersion(Application.version);
        }

        /// <summary>
        /// Check if Player requires an Update
        /// </summary>
        public static void CheckUpdates()
        {
            if (IsNewPlayer)
            {
                InitNewPlayer();
                return;
            }

            while (LastVersion != Application.version)
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
            if (LastVersion.CompareTo(Application.version) == 0)
                return test;

            // reset PlayerPrefs settings on every new versions
            Settings.Reload();

            if (LastVersion.CompareTo("0.1.5") == -1)
                test = UpdateVersion_0_1_5();

            if (LastVersion.CompareTo("0.1.6") == -1)
                test = UpdateVersion_0_1_6();

            if (LastVersion.CompareTo("0.1.7") == -1)
                test = UpdateVersion_0_1_7();

            if (LastVersion.CompareTo("0.1.8") == -1)
                test = UpdateVersion_0_1_8();

            // if does not trigger any version until now, update to current version
            if (LastVersion.CompareTo(Application.version) == -1)
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
            if (LastVersion.CompareTo(version) >= 0)
            {
                ErrorHandler.Error($"Trying to set new version {version} wich is <= current version {LastVersion}");
                return false;
            }

            Debug.Log($"Version Updated from {LastVersion} to {version}");
            PlayerPrefs.SetString("LastVersion", Application.version);
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
    }
}