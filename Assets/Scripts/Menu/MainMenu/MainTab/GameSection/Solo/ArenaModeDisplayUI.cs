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

namespace Menu.MainMenu.MainTab
{
    public class ArenaModeDisplayUI : MObject
    {
        #region Members

        EArenaType          m_ArenaType;
        ArenaData           m_ArenaData;

        TMP_Dropdown        m_DropdownButton;
        GameObject          m_ArenaSection;
        TMP_Text            m_DifficultyMode;
        ArenaButton         m_ArenaButton;
        ArenaStageSectionUI m_StageSectionUI;
        GameObject          m_MessageSection;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_DropdownButton    = Finder.FindComponent<TMP_Dropdown>(gameObject, "DropdownButton");
            m_DifficultyMode    = Finder.FindComponent<TMP_Text>(gameObject, "DifficultyMode");
            m_ArenaSection      = Finder.Find(gameObject, "ArenaSection");
            m_StageSectionUI    = Finder.FindComponent<ArenaStageSectionUI>(gameObject, "StageSection");
            m_MessageSection    = Finder.Find(gameObject, "MessageSection");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetUpDropDownButton();
            OnArenaTypeChanged(PlayerPrefsHandler.GetArenaType());
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            UIHelper.CleanContent(m_ArenaSection);

            // set the difficulty mode of the current arena
            m_DifficultyMode.text = TextHandler.SplitCamelCase(ProgressionCloudData.GetArenaDifficulty(m_ArenaType).ToString());

            // setup arena button style
            m_ArenaButton = Instantiate(AssetLoader.LoadArenaButton(m_ArenaType), m_ArenaSection.transform).GetComponent<ArenaButton>();
            m_ArenaButton.Initialize(m_ArenaType);

            // ARENA COMPLETED
            if (ProgressionCloudData.IsArenaCompleted(m_ArenaType))
            {
                m_StageSectionUI.transform.parent.gameObject.SetActive(true);   // keep the parent (layout management)
                m_StageSectionUI.gameObject.SetActive(false);                   // deactivate content
                m_MessageSection.gameObject.SetActive(false);                   // deactivate message
            } 

            // ARENA DIFFICULTY COMPLETED
            else if (ProgressionCloudData.IsArenaDifficultyCompleted(m_ArenaType))
            {
                // check that all rewards have been collected
                if (NotificationCloudData.HasRewardsForArenaType(m_ArenaType))
                {
                    m_StageSectionUI.transform.parent.gameObject.SetActive(false);
                    m_MessageSection.gameObject.SetActive(true);
                } 

                // ERROR CONTROL : if no more rewards to collect but for some reason the difficulty has not been updated -> do it
                else
                {
                    ErrorHandler.Error($"No rewards to claim on arena {m_ArenaType} at difficulty {m_ArenaData.ArenaDifficulty} - upgrading difficulty");
                    ProgressionCloudData.UpgradeArenaDifficulty(m_ArenaType);
                }
            }

            // NORMAL DISPLAY
            else
            {
                m_StageSectionUI.transform.parent.gameObject.SetActive(true);
                m_StageSectionUI.gameObject.SetActive(true);
                m_MessageSection.gameObject.SetActive(false);
                m_StageSectionUI.Initialize(m_ArenaData.CurrentLevel, m_ArenaData.CurrentLevel, m_ArenaData.CurrentStage, m_ArenaData.CurrentArenaLevelData.StageData.Count);
            }
        }

        void SetUpDropDownButton()
        {
            List<string> modes = Enum.GetNames(typeof(EArenaType)).ToList();

            // add values to dropdown
            m_DropdownButton.AddOptions(modes);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            PlayerPrefsHandler.ArenaTypeChangedEvent += OnArenaTypeChanged;
            ProgressionCloudData.ArenaDataChangedEvent += OnArenaDataChanged;
            m_DropdownButton.onValueChanged.AddListener(OnDropDownValueChanged);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            PlayerPrefsHandler.ArenaTypeChangedEvent -= OnArenaTypeChanged;
            m_DropdownButton.onValueChanged.RemoveListener(OnDropDownValueChanged);
        }


        void OnArenaTypeChanged(EArenaType arenaType)
        {
            m_ArenaType = arenaType;
            m_ArenaData = AssetLoader.LoadArenaData(arenaType);

            // set value to last selected value
            m_DropdownButton.value = Enum.GetNames(typeof(EArenaType)).ToList().IndexOf(m_ArenaType.ToString());
            
            RefreshUI();
        }

        void OnArenaDataChanged(EArenaType arenaType)
        {
            // CHECK : is same arena
            if (m_ArenaType != arenaType)
                return;

            // CHECK : is new difficulty
            if (m_ArenaData.ArenaDifficulty == ProgressionCloudData.GetArenaDifficulty(arenaType))
                return;

            m_ArenaData = AssetLoader.LoadArenaData(arenaType);
            RefreshUI();
        } 

        void OnDropDownValueChanged(int index)
        {
            if (!Enum.TryParse(m_DropdownButton.options[index].text, out EArenaType arenaType))
            {
                ErrorHandler.Error("Unable to convert " + m_DropdownButton.options[index].text + " as game mode");
                return;
            }

            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);
            PlayerPrefsHandler.SetArenaType(arenaType);
        }

        #endregion
    }
}