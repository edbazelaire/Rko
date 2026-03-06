using Assets.Scripts.Managers;
using Assets.Scripts.Network;
using Enums;
using Game.Loaders;
using Managers.Friends;
using Menu.MainMenu;
using Menu.PopUps;
using Network;
using Save;
using System.Collections.Generic;
using System;
using System.Net;
using System.Threading.Tasks;
using TMPro;
using Tools;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;
using Data;

namespace Assets.Scripts.UI
{
    public class AccountTabContent : TabContent
    {
        #region Members

        // LEFT SIDE
        ProfileDisplayUI    m_ProfileDisplay;
        Button              m_DeleteButton;
        Button              m_LoginButton;
        Button              m_LogoutButton;

        // RIGHT SIDE
        TMP_Text            m_PseudoText;
        TMP_Text            m_PlayerIdText; 
        TMP_Text            m_DiscordIdText;
        TMP_Text            m_VersionText;
        TMP_Dropdown        m_RegionDropdown; 
        TMP_InputField      m_DiscordIdInputField;
        Button              m_DiscordIdValidateButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            EnsureDiscordIdInfoUI();

            // LEFT SIDE
            m_ProfileDisplay    = Finder.FindComponent<ProfileDisplayUI>(gameObject);
            m_DeleteButton      = Finder.FindComponent<Button>(gameObject, "DeleteButton");
            m_LoginButton       = Finder.FindComponent<Button>(gameObject, "LoginButton");
            m_LogoutButton      = Finder.FindComponent<Button>(gameObject, "LogoutButton");

            // RIGHT SIDE
            m_PseudoText        = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "PseudoInfo"),     "Value");
            m_PlayerIdText      = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "PlayerIdInfo"),   "Value");
            m_DiscordIdText     = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "DiscordIdInfo"),  "Value", false);
            m_VersionText       = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "VersionInfo"),    "Value");
            m_RegionDropdown    = Finder.FindComponent<TMP_Dropdown>(Finder.Find(gameObject, "RegionInfo"), "Dropdown");
            m_DiscordIdInputField = Finder.FindComponent<TMP_InputField>(Finder.Find(gameObject, "DiscordIdInfo"), "InputField", false);
            m_DiscordIdValidateButton = Finder.FindComponent<Button>(Finder.Find(gameObject, "DiscordIdInfo"), "ValidateButton", false);
        }

        protected override async void SetUpUI()
        {
            base.SetUpUI();

            m_ProfileDisplay.Initialize(ProfileCloudData.CurrentProfileData);
            m_LoginButton.gameObject.SetActive(! AuthManager.Instance.IsLoggedIn);
            m_DeleteButton.gameObject.SetActive(AuthManager.Instance.IsLoggedIn);
            m_LogoutButton.gameObject.SetActive(AuthManager.Instance.IsLoggedIn);

            m_PseudoText.text       = ProfileCloudData.GamerTag;
            m_PlayerIdText.text     = AuthenticationService.Instance.PlayerId;
            m_VersionText.text      = UpdateManager.GameVersion.ToString();
            RefreshDiscordIdUI();

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

        void RefreshDiscordIdUI()
        {
            bool hasDiscordId = !string.IsNullOrWhiteSpace(ProfileCloudData.DiscordId);

            if (m_DiscordIdText != null)
            {
                m_DiscordIdText.gameObject.SetActive(hasDiscordId);
                if (hasDiscordId)
                    m_DiscordIdText.text = ProfileCloudData.DiscordId;
            }

            if (m_DiscordIdInputField != null)
            {
                m_DiscordIdInputField.gameObject.SetActive(!hasDiscordId);
                if (!hasDiscordId)
                    m_DiscordIdInputField.text = "";
            }

            if (m_DiscordIdValidateButton != null)
                m_DiscordIdValidateButton.gameObject.SetActive(!hasDiscordId);
        }

        void EnsureDiscordIdInfoUI()
        {
            var rightSide = Finder.Find(gameObject, "RightSide", false);
            if (rightSide == null)
                return;

            if (Finder.Find(rightSide, "DiscordIdInfo", false) != null)
                return;

            var pseudoInfo = Finder.Find(rightSide, "PseudoInfo", false);
            if (pseudoInfo == null)
                return;

            var discordInfo = Instantiate(pseudoInfo, rightSide.transform);
            discordInfo.name = "DiscordIdInfo";

            var label = Finder.FindComponent<TMP_Text>(Finder.Find(discordInfo, "Name", false), throwError: false);
            var value = Finder.FindComponent<TMP_Text>(Finder.Find(discordInfo, "Value", false), throwError: false);
            if (label != null) label.text = "Discord ID";
            if (value != null) value.text = "";

            if (value != null)
                value.gameObject.SetActive(false);

            var valueRect = value != null ? value.GetComponent<RectTransform>() : null;
            var defaultText = value != null ? value.text : "enter discord id";
            var defaultFont = value != null ? value.font : null;
            var defaultFontSize = value != null ? value.fontSize : 48f;
            var defaultColor = value != null ? value.color : Color.white;

            var inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(discordInfo.transform, false);
            var inputRect = inputGo.GetComponent<RectTransform>();
            if (valueRect != null)
            {
                inputRect.anchorMin = valueRect.anchorMin;
                inputRect.anchorMax = valueRect.anchorMax;
                inputRect.pivot = valueRect.pivot;
                inputRect.sizeDelta = valueRect.sizeDelta;
                inputRect.anchoredPosition = valueRect.anchoredPosition;
            }

            var inputBg = inputGo.GetComponent<Image>();
            inputBg.color = new Color(0f, 0f, 0f, 0.35f);

            var inputField = inputGo.GetComponent<TMP_InputField>();
            inputField.characterLimit = 64;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            var textGo = new GameObject("Text Area", typeof(RectTransform));
            textGo.transform.SetParent(inputGo.transform, false);
            var textAreaRect = textGo.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(16f, 8f);
            textAreaRect.offsetMax = new Vector2(-16f, -8f);

            var textComponentGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMP_Text));
            textComponentGo.transform.SetParent(textGo.transform, false);
            var textRect = textComponentGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComponent = textComponentGo.GetComponent<TMP_Text>();
            textComponent.text = "";
            textComponent.font = defaultFont;
            textComponent.fontSize = defaultFontSize;
            textComponent.color = defaultColor;
            textComponent.enableAutoSizing = true;
            textComponent.alignment = TextAlignmentOptions.Left;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMP_Text));
            placeholderGo.transform.SetParent(textGo.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholderText = placeholderGo.GetComponent<TMP_Text>();
            placeholderText.text = "enter discord id";
            placeholderText.font = defaultFont;
            placeholderText.fontSize = defaultFontSize;
            placeholderText.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.5f);
            placeholderText.enableAutoSizing = true;
            placeholderText.alignment = TextAlignmentOptions.Left;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;
            inputField.targetGraphic = inputBg;

            var validateGo = new GameObject("ValidateButton", typeof(RectTransform), typeof(Image), typeof(Button));
            validateGo.transform.SetParent(discordInfo.transform, false);
            var validateRect = validateGo.GetComponent<RectTransform>();
            validateRect.anchorMin = new Vector2(1f, 0.5f);
            validateRect.anchorMax = new Vector2(1f, 0.5f);
            validateRect.pivot = new Vector2(1f, 0.5f);
            validateRect.sizeDelta = new Vector2(180f, 90f);
            validateRect.anchoredPosition = new Vector2(0f, 0f);

            var validateBg = validateGo.GetComponent<Image>();
            validateBg.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);

            var validateTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMP_Text));
            validateTextGo.transform.SetParent(validateGo.transform, false);
            var validateTextRect = validateTextGo.GetComponent<RectTransform>();
            validateTextRect.anchorMin = Vector2.zero;
            validateTextRect.anchorMax = Vector2.one;
            validateTextRect.offsetMin = Vector2.zero;
            validateTextRect.offsetMax = Vector2.zero;
            var validateText = validateTextGo.GetComponent<TMP_Text>();
            validateText.text = "Validate";
            validateText.font = defaultFont;
            validateText.fontSize = defaultFontSize * 0.85f;
            validateText.color = Color.white;
            validateText.enableAutoSizing = true;
            validateText.alignment = TextAlignmentOptions.Center;

            if (valueRect != null)
            {
                inputRect.anchorMin = valueRect.anchorMin;
                inputRect.anchorMax = valueRect.anchorMax;
                inputRect.pivot = valueRect.pivot;
                inputRect.sizeDelta = new Vector2(valueRect.sizeDelta.x - 220f, valueRect.sizeDelta.y);
                inputRect.anchoredPosition = valueRect.anchoredPosition + new Vector2(-110f, 0f);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_DeleteButton.onClick.AddListener(OnDeleteButtonClicked);
            m_LoginButton.onClick.AddListener(OnLoginClicked);
            m_LogoutButton.onClick.AddListener(OnLogoutClicked);
            m_DiscordIdValidateButton?.onClick.AddListener(OnDiscordIdValidateClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_DeleteButton.onClick.RemoveAllListeners();
            m_LoginButton.onClick.RemoveAllListeners();
            m_LogoutButton.onClick.RemoveAllListeners();
            m_DiscordIdValidateButton?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// When a region is manually changed
        /// </summary>
        void OnRegionChanged(string regionValue)
        {
            // change the current region in cloud data
            ProfileCloudData.SetRegion(CleanDropdownRegionValue(regionValue));
        }

        void OnDeleteButtonClicked()
        {
            Main.ConfirmPopUp(
                "Are you sure you want to delete your account ?\nThis action can not be reverted, your data will be lost permanantly.",
                title: "Delete your Account",
                onValidate: AuthManager.Instance.DeleteAccountAndUnlink
            );
        }

        void OnLoginClicked()
        {
            AuthManager.Instance.Login();
        }

        void OnLogoutClicked()
        {
            Main.ConfirmPopUp(
                "Do you want to logout ?",
                title: "Logout",
                onValidate: AuthManager.Instance.Logout
            );
        }

        void OnDiscordIdValidateClicked()
        {
            if (m_DiscordIdInputField == null)
                return;

            string discordId = m_DiscordIdInputField.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(discordId))
            {
                ScreenManager.QuickMessage("Please enter a discord id.");
                return;
            }

            ProfileCloudData.SetDiscordId(discordId);
            _ = BackfillArenaAchievementsForDiscordIdAsync();
            RefreshDiscordIdUI();
        }

        async Task BackfillArenaAchievementsForDiscordIdAsync()
        {
            int sentEvents = 0;

            try
            {
                var arenaAchievements = AchievementLoader.Get<ArenaAchievementData>(ECharacter.None);
                foreach (var arenaAchievement in arenaAchievements)
                {
                    var achievementInfo = ProfileCloudData.GetAchievementInfo(arenaAchievement.ID);
                    int completedCount = Mathf.Max(0, achievementInfo.Index);
                    if (completedCount <= 0)
                        continue;

                    for (int i = 0; i < completedCount && i < arenaAchievement.AchievementSubData.Count; i++)
                    {
                        var step = arenaAchievement.AchievementSubData[i];

                        // In this project, arena progression starts at Brutal and index reflects completed tiers.
                        await InGameEventsApiClient.SendArenaAchievementUnlockedEventAsync(
                            step.ArenaType,
                            step.ArenaDifficulty,
                            step.ArenaMods
                        );
                        sentEvents++;
                    }
                }

                ScreenManager.QuickMessage("Discord ID saved. Synced " + sentEvents + " arena achievement event(s).");
            }
            catch (Exception ex)
            {
                ErrorHandler.Error("Failed to sync arena achievements for Discord ID: " + ex.Message);
                ScreenManager.QuickMessage("Discord ID saved, but achievement sync failed.");
            }
        }

        #endregion

    }
}