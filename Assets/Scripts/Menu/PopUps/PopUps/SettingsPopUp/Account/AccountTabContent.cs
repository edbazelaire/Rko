using Assets.Scripts.Managers;
using Assets.Scripts.Network;
using Enums;
using Managers.Friends;
using Menu.MainMenu;
using Network;
using Save;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TMPro;
using Tools;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class AccountTabContent : TabContent
    {
        #region Members

        // LEFT SIDE
        ProfileDisplayUI    m_ProfileDisplay;
        Button              m_LoginButton;
        Button              m_LogoutButton;

        // RIGHT SIDE
        TMP_Text            m_PseudoText;
        TMP_Text            m_PlayerIdText; 
        TMP_Text            m_VersionText;
        TMP_Dropdown        m_RegionDropdown; 

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // LEFT SIDE
            m_ProfileDisplay    = Finder.FindComponent<ProfileDisplayUI>(gameObject);
            m_LoginButton       = Finder.FindComponent<Button>(gameObject, "LoginButton");
            m_LogoutButton      = Finder.FindComponent<Button>(gameObject, "LogoutButton");

            // RIGHT SIDE
            m_PseudoText        = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "PseudoInfo"),     "Value");
            m_PlayerIdText      = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "PlayerIdInfo"),   "Value");
            m_VersionText       = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "VersionInfo"),    "Value");
            m_RegionDropdown    = Finder.FindComponent<TMP_Dropdown>(Finder.Find(gameObject, "RegionInfo"), "Dropdown");
        }

        protected override async void SetUpUI()
        {
            base.SetUpUI();

            m_ProfileDisplay.Initialize(ProfileCloudData.CurrentProfileData);
            m_LoginButton.gameObject.SetActive(! AuthManager.Instance.IsLoggedIn);
            m_LogoutButton.gameObject.SetActive(AuthManager.Instance.IsLoggedIn);

            m_PseudoText.text       = ProfileCloudData.GamerTag;
            m_PlayerIdText.text     = AuthenticationService.Instance.PlayerId;
            m_VersionText.text      = UpdateManager.GameVersion.ToString();

            await SetUpDropdown();
        }

        #endregion


        #region GUI Manipulators



        async Task SetUpDropdown()
        {
            // get all values of the dropdown from Relay available servers
            List<string> values = new();
            var allRelayRegions = await RelayService.Instance.ListRegionsAsync();
            string defaultValue = ProfileCloudData.Region;
            foreach (var relayRegion in allRelayRegions)
            {
                values.Add($"{relayRegion.Id} ({relayRegion.Description})");

                // handle default value
                if (relayRegion.Id == ProfileCloudData.Region)
                    defaultValue = $"{relayRegion.Id} ({relayRegion.Description})";
            }

            // check default value is in the list of values, otherwise add it (it might be not currently available but we do not want to override the value in database)
            if (string.IsNullOrEmpty(defaultValue) || ! values.Contains(defaultValue))
            {
                ErrorHandler.Error("Unable to find region (" + defaultValue + ") in list of available regions - adding manually");
                defaultValue = ProfileCloudData.Region;
                values.Add(defaultValue);
            }

            // sort before sending to dropdown
            values.Sort();

            // setup dropdown with initialized values
            UIHelper.SetUpDropdown(
                dropdown: m_RegionDropdown, 
                values: values, 
                defaultValue: defaultValue, 
                onDropDownValueChanged: OnRegionChanged
            );
        }

        /// <summary>
        /// Clean the name of the region coming from the DropDown
        /// ex : "europe-west2 (Paris)"  ->  "europe-west2"
        /// </summary>
        string CleanDropdownRegionValue(string regionValue)
        {
            return regionValue.Split(" ")[0];
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_LoginButton.onClick.AddListener(OnLoginClicked);
            m_LogoutButton.onClick.AddListener(OnLogoutClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_LoginButton.onClick.RemoveAllListeners();
            m_LogoutButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// When a region is manually changed
        /// </summary>
        void OnRegionChanged(string regionValue)
        {
            // change the current region in cloud data
            ProfileCloudData.SetRegion(CleanDropdownRegionValue(regionValue));
        }

        void OnLoginClicked()
        {
            AuthManager.Instance.Login();
        }

        void OnLogoutClicked()
        {
            AuthManager.Instance.Logout();
        }

        #endregion

    }
}