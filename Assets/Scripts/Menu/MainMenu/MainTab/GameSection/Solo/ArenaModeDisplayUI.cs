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
        GameObject          m_ArenaSection;
        ArenaButton         m_ArenaButton;
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
            m_ArenaSection              = Finder.Find(gameObject, "ArenaSection");
            m_StageSectionUI            = Finder.FindComponent<ArenaStageSectionUI>(gameObject, "StageSection");
            m_ButtonsSection            = Finder.Find(gameObject, "ButtonsSection");
            m_SelectButton              = Finder.FindComponent<Button>(m_ButtonsSection, "SelectButton");
            m_CollectButton             = Finder.FindComponent<Button>(m_ButtonsSection, "CollectButton");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetUpArenaDropDown();

            if (ProgressionCloudData.CurrentArena.ArenaType != EArenaType.None)
            {
                // display current arenna type with current difficulty
                m_ArenaType = ProgressionCloudData.CurrentArena.ArenaType;
                m_ArenaDifficulty = ProgressionCloudData.CurrentArena.SArenaDifficulty;
            }
            else
            {
                // display max difficulty for last selected arena type
                m_ArenaType = PlayerPrefsHandler.GetArenaType();
                m_ArenaDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(m_ArenaType);
            }

            // load data
            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            // HANDLE NO DATA
            if (m_ArenaData == null)
            {
                if (ProgressionCloudData.HasArenaInProgress)
                    ProgressionCloudData.ResetCurrentArena();
                else
                    PlayerPrefsHandler.SetArenaType(EArenaType.None);

                m_ArenaType = PlayerPrefsHandler.GetArenaType();
                m_ArenaDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(m_ArenaType);

                m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);
            }

            // display UI with current data
            RefreshUI();

            // Display only difficulties for this arena level
            RefreshDifficultyDropdown();

            // set value to last selected value
            m_ArenaTypeDropdown.SetValueWithoutNotify(Enum.GetNames(typeof(EArenaType)).ToList().IndexOf(m_ArenaType.ToString()));
            m_ArenaDifficultyDropdown.SetValueWithoutNotify(m_ArenaDifficultyDropdown.options.FindIndex(option => option.text == m_ArenaDifficulty.ToString()));
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            UIHelper.CleanContent(m_ArenaSection);

            // setup arena button style
            m_ArenaButton = Instantiate(AssetLoader.LoadArenaButton(m_ArenaType), m_ArenaSection.transform).GetComponent<ArenaButton>();
            m_ArenaButton.Initialize(m_ArenaType, m_ArenaDifficulty);

            // NO ARENA selected
            if (! ProgressionCloudData.HasArenaInProgress)
            {
                m_ArenaTypeDropdown.interactable = true;
                m_ArenaDifficultyDropdown.interactable = true;
                m_StageSectionUI.transform.parent.gameObject.SetActive(false);
                m_ButtonsSection.gameObject.SetActive(true);
                m_SelectButton.gameObject.SetActive(true);
                m_CollectButton.gameObject.SetActive(false);
            }
            
            // ARENA IS OVER
            else if (ProgressionCloudData.CurrentArena.IsOver())
            {
                // if is Over but has not reward - Refresh CloudData + UI
                if (ProgressionCloudData.CurrentArena.Level == 0)
                {
                    ProgressionCloudData.ResetCurrentArena();
                    RefreshUI();
                    return;
                }

                m_ArenaTypeDropdown.interactable = false;
                m_ArenaDifficultyDropdown.interactable = false;
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
                m_StageSectionUI.transform.parent.gameObject.SetActive(true);
                m_ButtonsSection.gameObject.SetActive(false);
                m_StageSectionUI.Initialize(m_ArenaData.CurrentLevel, m_ArenaData);
            }
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
                for (int i = 0; i < ArenaManagementData.NDifficultyLevels; i++)
                {
                    var arenaDifficulty = new SArenaDifficulty(difficulty, i);

                    if (arenaDifficulty > ProgressionCloudData.UnlockedArenas[m_ArenaType])
                        break;

                    values.Add(arenaDifficulty.ToString());
                }
            }

            // add values to dropdown
            m_ArenaDifficultyDropdown.AddOptions(values);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            ProgressionCloudData.CurrentArenaDataChangedEvent += OnCurrentArenaDataChanged;
            m_ArenaTypeDropdown.onValueChanged.AddListener(OnArenaTypeValueChanged);
            m_ArenaDifficultyDropdown.onValueChanged.AddListener(OnArenaDifficultyValueChanged);
            m_SelectButton.onClick.AddListener(OnSelectButtonClicked);
            m_CollectButton.onClick.AddListener(OnCollectButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProgressionCloudData.CurrentArenaDataChangedEvent -= OnCurrentArenaDataChanged;
            m_ArenaTypeDropdown.onValueChanged.RemoveListener(OnArenaTypeValueChanged);
            m_ArenaDifficultyDropdown.onValueChanged.RemoveListener(OnArenaDifficultyValueChanged);
            m_SelectButton.onClick.RemoveListener(OnSelectButtonClicked);
            m_CollectButton.onClick.RemoveListener(OnCollectButtonClicked);
        }

        void SetArenaType(EArenaType arenaType)
        {
            m_ArenaType = arenaType;

            // check set arena allows the current level of difficulty
            var maxAllowedDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(arenaType);
            if (m_ArenaDifficulty > maxAllowedDifficulty)
            {
                m_ArenaDifficulty = maxAllowedDifficulty;
                m_ArenaDifficultyDropdown.SetValueWithoutNotify(Enum.GetNames(typeof(EArenaDifficulty)).ToList().IndexOf(m_ArenaDifficulty.ToString()));
            }

            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            // set value to last selected value
            m_ArenaTypeDropdown.value = Enum.GetNames(typeof(EArenaType)).ToList().IndexOf(m_ArenaType.ToString());

            // Display only difficulties for this arena level
            RefreshDifficultyDropdown();

            RefreshUI();
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

            SetArenaType(arenaType);
        }

        void OnArenaDifficultyValueChanged(int index)
        {
            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

            m_ArenaDifficulty = (new SArenaDifficulty()).FromString(m_ArenaDifficultyDropdown.options[index].text);
            m_ArenaData = AssetLoader.LoadArenaData(m_ArenaType, m_ArenaDifficulty);

            RefreshUI();
        }

        public void OnSelectButtonClicked()
        {
            ProgressionCloudData.CreateNewCurrentArena(m_ArenaData.ArenaType, m_ArenaDifficulty);
        }

        void OnCollectButtonClicked()
        {
            Main.SetPopUp(EPopUpState.RewardsScreen, m_ArenaData.GetCurrentRewards(), "Arena");
            ProgressionCloudData.ResetCurrentArena();
        }

        #endregion
    }
}