using System;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using Menu.Common.Rewards;
using Assets.Scripts.Data.GameManagement;
using Save;
using Enums;
using Menu.MainMenu.MainTab;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Xml;

namespace Menu.PopUps
{
    /// <summary>
    /// Pop-up used for selecting Arena difficulty and Arena mods.
    /// Displays selected mods, their effects, and reward bonuses.
    /// </summary>
    public class ArenaOptionsPopUp : PopUp
    {
        #region Members

        [SerializeField] ArenaModButtonUI m_TemplateArenaModButton;

        // ===========================================================================================
        // GameObjects & Components
        private ArenaModInfoUI              m_InfoSection;
        private CurrentArenaModsDisplayer   m_CurrentArenaModsDisplayer;
        private ArenaExtraDifficultyUI      m_ArenaExtraDifficultyUI;
        private Transform                   m_ModsContainer;
        private TMP_Text                    m_ModTitle;
        private Image                       m_SelectedModIcon;
        private PowerOrbContainer           m_PowerOrbContainer;
        private TMP_Text                    m_BonusRewardText;

        // Data
        List<ArenaModButtonUI> m_ArenaModButtons;

        #endregion


        #region Init & End

        /// <summary>
        /// Finds all UI components using the Finder tool.
        /// </summary>
        protected override void FindComponents()
        {
            base.FindComponents();

            m_CurrentArenaModsDisplayer = Finder.FindComponent<CurrentArenaModsDisplayer>(gameObject);
            m_ArenaExtraDifficultyUI    = Finder.FindComponent<ArenaExtraDifficultyUI>(gameObject);
            m_ModTitle                  = Finder.FindComponent<TMP_Text>(gameObject, "ModTitle");
            m_SelectedModIcon           = Finder.FindComponent<Image>(gameObject, "SelectedModIcon");
            m_ModsContainer             = Finder.Find(gameObject, "ModsContainer").transform;
            m_InfoSection               = Finder.FindComponent<ArenaModInfoUI>(gameObject);
            m_PowerOrbContainer         = Finder.FindComponent<PowerOrbContainer>(gameObject);
            m_BonusRewardText           = Finder.FindComponent<TMP_Text>(gameObject, "BonusReward");
        }

        /// <summary>
        /// Initializes the pop-up, sets up all mod buttons and default state.
        /// </summary>
        protected override void OnPrefabLoaded()
        {
            InitializeMods();
            RefreshRewards();
            m_InfoSection.Initialize();
            m_CurrentArenaModsDisplayer.Initialize();
            m_ArenaExtraDifficultyUI.Initialize(PlayerPrefsHandler.CurrentArenaType, PlayerPrefsHandler.CurrentArenaDifficulty);

            // initialize UI with a selected mod
            SelectMod(PlayerPrefsHandler.CurrentArenaMods.Count > 0 ? PlayerPrefsHandler.CurrentArenaMods[0] : EArenaMod.Random);
        }

        /// <summary>
        /// Prepares each mod button with its callback and populates its data.
        /// Selects the first mod by default to show its details.
        /// </summary>
        private void InitializeMods()
        {
            // reset buttons
            m_ArenaModButtons = new();

            // clean and populate arena mods
            UIHelper.CleanContent(m_ModsContainer);
            foreach (EArenaMod arenaMod in Enum.GetValues(typeof(EArenaMod)))
            {
                if (arenaMod == EArenaMod.None)
                    continue;

                ArenaModButtonUI arenaModButton = Instantiate(m_TemplateArenaModButton, m_ModsContainer);
                arenaModButton.Initialize(arenaMod);
                arenaModButton.Button.onClick.AddListener(() => SelectMod(arenaMod));

                m_ArenaModButtons.Add(arenaModButton);
            }
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Updates the bottom section showing the total bonus rewards.
        /// </summary>
        void RefreshRewards()
        {
            m_PowerOrbContainer.Initialize(ProgressionCloudData.CurrentArena.GetPowerOrb(), activateIdle: false);

            // calculate current bonus value
            int bonus = (int)Mathf.Round((ProgressionCloudData.CurrentArena.GetBonusPowerOrb() + ArenaManagementData.BonusArenaDifficultyLevel * PlayerPrefsHandler.CurrentArenaExtraDifficulty - 1) * 100);

            // Display the bonus reward percentage
            m_BonusRewardText.text = $"+ {bonus}%";
        }

        /// <summary>
        /// Called when a mod button is clicked.
        /// </summary>
        /// <param name="modData">The ArenaModData of the selected mod</param>
        private void SelectMod(EArenaMod arenaMod)
        {
            // Update current mod and refresh the right panel
            m_ModTitle.text = TextHandler.Clean(arenaMod.ToString());
            m_SelectedModIcon.sprite = AssetLoader.LoadIcon(arenaMod);
            m_InfoSection.DisplayMod(arenaMod, () => ToogleArenaMod(arenaMod));

            // display selected arena mod
            foreach (var arenaButton in m_ArenaModButtons)
            {
                arenaButton.SetSelected(arenaButton.ArenaMod == arenaMod);
            }
        }

        #endregion


        #region Listeners

        /// <summary>
        /// Register listeners (if needed).
        /// </summary>
        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            PlayerPrefsHandler.ArenaModsChangedEvent += RefreshRewards;
            PlayerPrefsHandler.ArenaExtraDifficultyChanged += RefreshRewards;
        }

        /// <summary>
        /// Unregister listeners 
        /// </summary>
        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            PlayerPrefsHandler.ArenaModsChangedEvent -= RefreshRewards;
            PlayerPrefsHandler.ArenaExtraDifficultyChanged -= RefreshRewards;
        }

        /// <summary>
        /// Called when a mod button is clicked.
        /// </summary>
        /// <param name="modData">The ArenaModData of the selected mod</param>
        private void ToogleArenaMod(EArenaMod arenaMod)
        {
            var currentMods = PlayerPrefsHandler.CurrentArenaMods;
            if (currentMods.Contains(arenaMod)) 
            { 
                currentMods.Remove(arenaMod); 
            } else
            {
                currentMods.Add(arenaMod);
            }

            PlayerPrefsHandler.SetArenaMods(PlayerPrefsHandler.CurrentArenaType, PlayerPrefsHandler.CurrentArenaDifficulty, currentMods);
        }

        #endregion
    }
}
