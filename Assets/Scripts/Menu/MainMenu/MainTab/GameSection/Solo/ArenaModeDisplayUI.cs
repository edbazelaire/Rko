using Data.GameManagement;
using Enums;
using System.Collections.Generic;
using System;
using TMPro;
using Tools;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Managers.Sound;
using Save;
using UnityEngine.UI;
using Assets;
using Tools.Animations;
using System.Collections;
using Assets.Scripts.Managers;
using Save.Data.Progression.Structs;
using Managers;
using Inventory;
using Assets.Scripts.Data.GameManagement;

namespace Menu.MainMenu.MainTab
{
    public class ArenaModeDisplayUI : MObject
    {
        #region Members

        EArenaType          m_ArenaType;
        SArenaDifficulty    m_ArenaDifficulty;
        ArenaData           m_ArenaData;

        TMP_Dropdown        m_ArenaTypeDropdown;
        TMP_Dropdown        m_ArenaDifficultyDropdown;
        ArenaOptionsUI      m_ArenaOptionsUI;
        GameObject          m_ArenaDifficultyAnimation;
        GameObject          m_ArenaSection;
        GameObject          m_ArenaButtonContainer;
        ArenaButton         m_ArenaButton;
        Button              m_BuildButton;
        LifesSectionUI      m_LifesSection;
        ArenaStageSectionUI m_StageSectionUI;
        GameObject          m_ButtonsSection;
        Button              m_SelectButton;
        Button              m_CollectButton;
        Image               m_KeyIcon;
        Image               m_LockIcon;

        bool m_RequiresKey => ProgressionCloudData.IsCompleted(m_ArenaType, m_ArenaDifficulty.Difficulty);

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ArenaTypeDropdown         = Finder.FindComponent<TMP_Dropdown>(gameObject, "DropdownButton");
            m_ArenaDifficultyDropdown   = Finder.FindComponent<TMP_Dropdown>(gameObject, "ArenaDifficultyDropdown");
            m_ArenaDifficultyAnimation  = Finder.Find(m_ArenaDifficultyDropdown.gameObject, "Animation");
            m_ArenaSection              = Finder.Find(gameObject, "ArenaSection");
            m_ArenaButtonContainer      = Finder.Find(gameObject, "ArenaButtonContainer");
            m_ArenaOptionsUI            = Finder.FindComponent<ArenaOptionsUI>(m_ArenaSection, "ArenaOptionsUI");
            m_BuildButton               = Finder.FindComponent<Button>(m_ArenaSection, "BuildButton");
            m_LifesSection              = Finder.FindComponent<LifesSectionUI>(gameObject, "LifesSection");
            m_StageSectionUI            = Finder.FindComponent<ArenaStageSectionUI>(gameObject, "StageSection");
            m_ButtonsSection            = Finder.Find(gameObject, "ButtonsSection");
            m_SelectButton              = Finder.FindComponent<Button>(m_ButtonsSection, "SelectButton");
            m_CollectButton             = Finder.FindComponent<Button>(m_ButtonsSection, "CollectButton");
            m_LockIcon                  = Finder.FindComponent<Image>(gameObject, "LockIcon");
            m_KeyIcon                   = Finder.FindComponent<Image>(m_SelectButton.gameObject, "Icon");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_LifesSection.Initialize(0, 0);
            SetUpArenaDropDown();

            if (ProgressionCloudData.HasArenaInProgress)
            {
                // display current arenna type with current difficulty
                m_ArenaType = ProgressionCloudData.CurrentArena.ArenaType;
                m_ArenaDifficulty = ProgressionCloudData.CurrentArena.SArenaDifficulty;
            }
            else
            {
                // display max difficulty for last selected arena type
                m_ArenaType = PlayerPrefsHandler.GetArenaType();
                var arenaDifficulty = PlayerPrefsHandler.GetArenaDifficulty(m_ArenaType);
                m_ArenaDifficulty = new SArenaDifficulty(arenaDifficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, arenaDifficulty));
            }

            if (m_ArenaType == EArenaType.None)
            {
                return;
            }

            // load data
            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            // HANDLE NO DATA
            if (m_ArenaData == null)
            {
                if (ProgressionCloudData.HasArenaInProgress)
                    ProgressionCloudData.ResetCurrentArena();
                else
                    PlayerPrefsHandler.SetArenaType(PlayerPrefsHandler.DEFAULT_ARENA_TYPE);

                m_ArenaType = PlayerPrefsHandler.GetArenaType();
                var arenaDifficulty = PlayerPrefsHandler.GetArenaDifficulty(m_ArenaType);
                m_ArenaDifficulty = new SArenaDifficulty(arenaDifficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, arenaDifficulty));
                m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);
            }

            // display UI with current data
            RefreshUI();

            // Display only difficulties for this arena level
            RefreshDifficultyDropdown();

            // set value to last selected value
            m_ArenaTypeDropdown.SetValueWithoutNotify(Enum.GetNames(typeof(EArenaType)).ToList().IndexOf(m_ArenaType.ToString()));
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            UIHelper.CleanContent(m_ArenaButtonContainer);

            // setup arena button style
            m_ArenaButton = Instantiate(AssetLoader.LoadArenaButton(m_ArenaType), m_ArenaButtonContainer.transform).GetComponent<ArenaButton>();
            m_ArenaButton.Initialize(m_ArenaType, m_ArenaDifficulty);

            // refresh ArenaOptions
            m_ArenaOptionsUI.Initialize(m_ArenaType, m_ArenaDifficulty.Difficulty);

            // by default, remove lock icon
            m_LockIcon.gameObject.SetActive(false);

            // NO ARENA selected
            if (! ProgressionCloudData.HasArenaInProgress)
            {
                m_ArenaTypeDropdown.interactable = true;
                m_ArenaDifficultyDropdown.interactable = true;
                m_BuildButton.gameObject.SetActive(false);
                m_LifesSection.Activate(false);
                m_StageSectionUI.transform.parent.gameObject.SetActive(false);
                m_ButtonsSection.gameObject.SetActive(true);
                m_SelectButton.gameObject.SetActive(true);
                m_KeyIcon.gameObject.SetActive(m_RequiresKey);
                m_CollectButton.gameObject.SetActive(false);

                // check if has notification of unlocked arena
                if (NotificationCloudData.HasUnlockedArena(m_ArenaType))
                {
                    // set the arena difficulty data
                    EArenaDifficulty arenaDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(m_ArenaType, clamp: true);
                    SetArenaDifficulty(arenaDifficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, arenaDifficulty));

                    // set this new unlocked value as current selected and add 
                    ScreenManager.StoreEvent(EPopUpState.MainMenuScreen, () => StartCoroutine(NewArenaDifficultyAnim()));

                    // tell notifications that the value has been seen
                    NotificationCloudData.CollectUnlockedArena(m_ArenaType);
                } 
                
                // arena not unlocked yet
                if (! ArenaManagementData.IsArenaUnlocked(m_ArenaType))
                {
                    m_LockIcon.gameObject.SetActive(true);
                } 
            }
            
            // ARENA IS OVER
            else if (ProgressionCloudData.CurrentArena.IsOver())
            {
                // if is Over but has not reward - Refresh CloudData + UI
                if (ProgressionCloudData.CurrentArena.Level == 0 && ProgressionCloudData.CurrentArena.Stage == 0)
                {
                    ProgressionCloudData.ResetCurrentArena();
                    RefreshUI();
                    return;
                }

                m_ArenaTypeDropdown.interactable = false;
                m_ArenaDifficultyDropdown.interactable = false;
                m_BuildButton.gameObject.SetActive(false);
                m_LifesSection.Activate(false);
                m_StageSectionUI.transform.parent.gameObject.SetActive(false);
                m_ButtonsSection.gameObject.SetActive(true);
                m_SelectButton.gameObject.SetActive(false);
                m_CollectButton.gameObject.SetActive(true);
            }

            // ARENA IS IN PROGRESS
            else
            {
                m_ArenaTypeDropdown.interactable = false;
                m_ArenaDifficultyDropdown.interactable = false;
                m_BuildButton.gameObject.SetActive(ProgressionCloudData.CurrentArena.HasBuildData());
                m_ButtonsSection.gameObject.SetActive(false);
                m_StageSectionUI.transform.parent.gameObject.SetActive(true);
                m_StageSectionUI.Initialize(m_ArenaData.CurrentLevel, m_ArenaData);

                // check if activates LIFES SECTION
                if (ProgressionCloudData.CurrentArena.HasMod(EArenaMod.NoDeath))
                    m_LifesSection.gameObject.SetActive(false);
                else
                {
                    m_LifesSection.Activate(true);
                    m_LifesSection.RefreshUI(ProgressionCloudData.CalculateArenaMaxLifes() - ProgressionCloudData.CurrentArena.Losses, ProgressionCloudData.CalculateArenaMaxLifes()); m_StageSectionUI.transform.parent.gameObject.SetActive(true);
                }
            }
        }

        void SetArenaDifficulty(EArenaDifficulty arenaDifficulty, int extraDifficulty)
        {
            m_ArenaDifficulty = new SArenaDifficulty(arenaDifficulty, extraDifficulty);
            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);
            m_ArenaButton.UpdateArenaDifficulty(m_ArenaDifficulty);
        }

        void SetUpArenaDropDown()
        {
            // ARENA TYPE
            List<string> values = Enum.GetNames(typeof(EArenaType)).ToList();
            values.Remove(EArenaType.None.ToString());

            // add values to dropdown
            m_ArenaTypeDropdown.AddOptions(values);
        }

        void RefreshDifficultyDropdown()
        {
            // clear options before adding
            m_ArenaDifficultyDropdown.ClearOptions();

            // ARENA DIFFICULTY
            var values = new List<string>();
            foreach (EArenaDifficulty difficulty in Enum.GetValues(typeof(EArenaDifficulty)))
            {
                if (difficulty > ProgressionCloudData.UnlockedArenas[m_ArenaType])
                    break;

                values.Add(difficulty.ToString());
            }

            // add values to dropdown
            m_ArenaDifficultyDropdown.AddOptions(values);

            // set current difficulty as selected difficulty
            m_ArenaDifficultyDropdown.SetValueWithoutNotify(m_ArenaDifficultyDropdown.options.FindIndex(option => option.text == m_ArenaDifficulty.Difficulty.ToString()));
        }

        #endregion


        #region Animation

        IEnumerator NewArenaDifficultyAnim()
        {
            // play an animation
            m_ArenaDifficultyAnimation.SetActive(true);
            var fade = m_ArenaDifficultyAnimation.AddComponent<Fade>();
            fade.Initialize(duration: 1f, startOpacity: 0f, endOpacity: 1f);

            yield return new WaitForSeconds(1f);

            // change value in the dropdown
            m_ArenaDifficultyDropdown.value = m_ArenaDifficultyDropdown.options.FindIndex(option => option.text == m_ArenaDifficulty.Difficulty.ToString());

            yield return new WaitForSeconds(1.5f);

            fade = m_ArenaDifficultyAnimation.AddComponent<Fade>();
            fade.Initialize(duration: 1f, startOpacity: 1f, endOpacity: 0f);
            m_ArenaDifficultyAnimation.SetActive(false);
        }

        #endregion


        #region Select Arena

        void SelectArena()
        {
            ProgressionCloudData.CreateNewCurrentArena(
                m_ArenaData.ArenaType,
                m_ArenaDifficulty,
                PlayerPrefsHandler.GetArenaMods(m_ArenaData.ArenaType, m_ArenaDifficulty.Difficulty),
                CharacterBuildsCloudData.SelectedCharacter,
                new SBuildData(0, "")
            );

            // CHECK : Random mod
            if (ProgressionCloudData.CurrentArena.ArenaMods.Contains(EArenaMod.Random))
            {
                ScreenManager.SetPopUpFadeIn(EPopUpState.ArenaBuildConstructorScreen);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            ProgressionCloudData.CurrentArenaDataChangedEvent += OnCurrentArenaDataChanged;
            PlayerPrefsHandler.ArenaModsChangedEvent += OnArenaModsChanged;
            m_ArenaTypeDropdown.onValueChanged.AddListener(OnArenaTypeValueChanged);
            m_ArenaDifficultyDropdown.onValueChanged.AddListener(OnArenaDifficultyValueChanged);
            m_SelectButton.onClick.AddListener(OnSelectButtonClicked);
            m_CollectButton.onClick.AddListener(OnCollectButtonClicked);
            m_BuildButton.onClick.AddListener(OnBuildButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProgressionCloudData.CurrentArenaDataChangedEvent -= OnCurrentArenaDataChanged;
            PlayerPrefsHandler.ArenaModsChangedEvent -= OnArenaModsChanged;
            m_ArenaTypeDropdown.onValueChanged.RemoveAllListeners();
            m_ArenaDifficultyDropdown.onValueChanged.RemoveAllListeners();
            m_SelectButton.onClick.RemoveAllListeners();
            m_CollectButton.onClick.RemoveAllListeners();
            m_BuildButton.onClick.RemoveAllListeners();
        }

        void SetArenaType(EArenaType arenaType)
        {
            m_ArenaType = arenaType;

            // check set arena allows the current level of difficulty
            var maxAllowedDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(arenaType, clamp: true);
            if (m_ArenaDifficulty.Difficulty > maxAllowedDifficulty)
            {
                m_ArenaDifficulty.Difficulty = maxAllowedDifficulty;
                m_ArenaDifficultyDropdown.SetValueWithoutNotify(Enum.GetNames(typeof(EArenaDifficulty)).ToList().IndexOf(m_ArenaDifficulty.ToString()));
            }

            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            // set value to last selected value
            m_ArenaTypeDropdown.value = Enum.GetNames(typeof(EArenaType)).ToList().IndexOf(m_ArenaType.ToString());

            // Display only difficulties for this arena level
            RefreshDifficultyDropdown();

            RefreshUI();
        }

        void OnArenaModsChanged() 
        {
            var difficulty = PlayerPrefsHandler.GetArenaDifficulty(m_ArenaType);
            m_ArenaDifficulty = new SArenaDifficulty(difficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, difficulty));
            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            m_ArenaButton.RefreshDifficulty(m_ArenaDifficulty);
        }

        void OnCurrentArenaDataChanged()
        {
            RefreshUI();
        } 

        void OnArenaTypeValueChanged(int index)
        {
            if (!Enum.TryParse(m_ArenaTypeDropdown.options[index].text, out EArenaType arenaType))
            {
                ErrorHandler.Error("Unable to convert " + m_ArenaTypeDropdown.options[index].text + " as game mode");
                return;
            }

            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

            PlayerPrefsHandler.SetArenaType(arenaType);
            SetArenaType(arenaType);
        }

        void OnArenaDifficultyValueChanged(int index)
        {
            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

            if (! Enum.TryParse(m_ArenaDifficultyDropdown.options[index].text, out EArenaDifficulty difficulty))
            {
                ErrorHandler.Error("Unable to parse " + m_ArenaDifficultyDropdown.options[index].text + " as arena difficulty");
                difficulty = EArenaDifficulty.Easy;
            }

            PlayerPrefsHandler.SetArenaDifficulty(m_ArenaType, difficulty);
            m_ArenaDifficulty = new SArenaDifficulty(difficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, difficulty));
            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            RefreshUI();
        }

        public void OnSelectButtonClicked()
        {
            if (!ArenaManagementData.IsArenaUnlocked(m_ArenaType))
            {
                ScreenManager.QuickMessage($"This arena is unlocked at level {ArenaManagementData.GetArenaSpecialConfig(m_ArenaType).UnlockedLevel}");
                return;
            }

            if (m_RequiresKey)
            {
                if (InventoryCloudData.Instance.GetCurrency(ECurrency.Keys) <= 0)
                {
                    ScreenManager.QuickMessage(
                       message: "You do not have any key " + TextHandler.FormatIcon("Keys") + " left to play this game mode.\nYou can earn more or buy some in the shop"
                    );
                    return;
                }

                InventoryManager.Spend(new SPriceData(1, ECurrency.Keys), $"{m_ArenaType}_{m_ArenaDifficulty.Difficulty}");
            }

            SelectArena();
        }

        void OnCollectButtonClicked()
        {
            var rewards = m_ArenaData.GetCurrentRewards();
            ProgressionCloudData.UpdateArenaUnlockedRewards();

            ScreenManager.DisplayRewards(rewards, "Arena");
            ProgressionCloudData.ResetCurrentArena();
        }

        void OnBuildButtonClicked()
        {
            ScreenManager.SetPopUp(EPopUpState.ArenaBuildScreen);
        }

        #endregion
    }
}