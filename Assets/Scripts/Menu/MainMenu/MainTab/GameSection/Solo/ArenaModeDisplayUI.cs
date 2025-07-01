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
                m_ArenaDifficulty = new SArenaDifficulty(PlayerPrefsHandler.GetArenaDifficulty(m_ArenaType), PlayerPrefs.GetInt(EPlayerPref.ArenaExtraDifficulty.ToString()));
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
                    PlayerPrefsHandler.SetArenaType(EArenaType.FrostArena);

                m_ArenaType = PlayerPrefsHandler.GetArenaType();
                m_ArenaDifficulty = new SArenaDifficulty(PlayerPrefsHandler.GetArenaDifficulty(m_ArenaType), PlayerPrefs.GetInt(EPlayerPref.ArenaExtraDifficulty.ToString()));
                m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);
            }

            // display UI with current data
            RefreshUI();

            // Display only difficulties for this arena level
            RefreshDifficultyDropdown();

            // set value to last selected value
            m_ArenaTypeDropdown.SetValueWithoutNotify(Enum.GetNames(typeof(EArenaType)).ToList().IndexOf(m_ArenaType.ToString()));
            m_ArenaDifficultyDropdown.SetValueWithoutNotify(m_ArenaDifficultyDropdown.options.FindIndex(option => option.text == m_ArenaDifficulty.Difficulty.ToString()));
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
                m_CollectButton.gameObject.SetActive(false);

                // check if has notification of unlocked arena
                if (NotificationCloudData.HasUnlockedArena(m_ArenaType))
                {
                    // set the arena difficulty data
                    EArenaDifficulty arenaDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(m_ArenaType);
                    SetArenaDifficulty(arenaDifficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, arenaDifficulty));

                    // set this new unlocked value as current selected and add 
                    ScreenManager.StoreEvent(EPopUpState.MainMenuScreen, () => StartCoroutine(NewArenaDifficultyAnim()));

                    // tell notifications that the value has been seen
                    NotificationCloudData.CollectUnlockedArena(m_ArenaType);
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
                m_BuildButton.gameObject.SetActive(true);
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
                m_BuildButton.gameObject.SetActive(true);
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
            m_ArenaDifficultyDropdown.SetValueWithoutNotify(m_ArenaDifficultyDropdown.options.FindIndex(option => option.text == m_ArenaDifficulty.ToString()));

            yield return new WaitForSeconds(1.5f);

            fade = m_ArenaDifficultyAnimation.AddComponent<Fade>();
            fade.Initialize(duration: 1f, startOpacity: 1f, endOpacity: 0f);
            m_ArenaDifficultyAnimation.SetActive(false);
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
            PlayerPrefsHandler.ArenaExtraDifficultyChanged -= OnArenaExtraDifficultyChanged;
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
            var maxAllowedDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(arenaType);
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

        void OnArenaExtraDifficultyChanged()
        {
            // update the arena difficulty data
            SetArenaDifficulty(m_ArenaDifficulty.Difficulty, PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, m_ArenaDifficulty.Difficulty));
        }

        void OnArenaModsChanged() { }

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
            ProgressionCloudData.CreateNewCurrentArena(m_ArenaData.ArenaType, m_ArenaDifficulty, PlayerPrefsHandler.GetArenaMods(m_ArenaData.ArenaType, m_ArenaDifficulty.Difficulty), CharacterBuildsCloudData.CurrentBuild);

            // CHECK : Random mod
            if (ProgressionCloudData.CurrentArena.ArenaMods.Contains(EArenaMod.Random))
            {
                ScreenManager.SetPopUp(EPopUpState.ArenaBuildConstructorScreen);
            }
        }

        void OnCollectButtonClicked()
        {
            var rewards = m_ArenaData.GetCurrentRewards();
            ProgressionCloudData.UpdateArenaUnlockedRewards();

            Main.DisplayRewards(rewards, "Arena");
            ProgressionCloudData.ResetCurrentArena();
        }

        void OnBuildButtonClicked()
        {
            ScreenManager.SetPopUp(EPopUpState.ArenaBuildScreen);
        }

        #endregion
    }
}