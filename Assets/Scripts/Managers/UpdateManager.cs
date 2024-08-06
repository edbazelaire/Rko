using Analytics.Events;
using Data.GameManagement;
using Save;
using System.Threading.Tasks;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public static class UpdateManager
    {
        public static string LastVersion => PlayerPrefs.GetString("LastVersion", "0.0.0");

        public static async Task CheckUpdates()
        {
            while (LastVersion != Application.version)
            {
                if (! await UpdateVersion())
                {
                    ErrorHandler.Error($"Unable to udpate version {LastVersion} to {Application.version}");
                    return;
                }
            }
        }

        public static async Task<bool> UpdateVersion()
        {
            var test = true;
            if (LastVersion.CompareTo(Application.version) == 0)
                return test;

            // reset PlayerPrefs settings on every new versions
            Settings.Reload();

            if (LastVersion.CompareTo("0.1.5") == -1)
            {
                // register GamerTag and Token
                MAnalytics.SendEvent(new PlayerDataEvent(ProfileCloudData.GamerTag, ProfileCloudData.Token, ProfileCloudData.Region));

                // save version
                if (!SetVersion("0.1.5"))
                    test = false;
                return test;
            }

            // if does not trigger any version until now, update to current version
            SetVersion(Application.version);
            return test;
        }

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
    }
}