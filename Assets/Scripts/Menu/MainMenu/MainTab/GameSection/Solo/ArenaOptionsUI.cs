using Enums;
using Menu.Common.Filters;
using Save;
using System;
using Tools;
using UnityEngine;


namespace Menu.MainMenu.MainTab
{
    public class ArenaOptionsUI : MObject
    {
        #region Members

        // =================================================================================
        // Actions
        public static Action ArenaModsChangeEvent;

        // =================================================================================
        // GameObject & Components
        SpellFilterDropdown m_ArenaModsDropdown;
        ArenaExtraDifficultyUI m_ArenaExtraDifficulty;

        // =================================================================================
        // Local Data
        EArenaType m_ArenaType;
        EArenaDifficulty m_ArenaDifficulty;

        // =================================================================================
        // Public Accessors
        public ArenaExtraDifficultyUI ArenaExtraDifficulty => m_ArenaExtraDifficulty;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ArenaModsDropdown = Finder.FindComponent<SpellFilterDropdown>(gameObject, "ArenaModsDropdown");
            m_ArenaExtraDifficulty = Finder.FindComponent<ArenaExtraDifficultyUI>(gameObject, "ArenaExtraDifficulty");
        }

        public virtual void Initialize(EArenaType arenaType, EArenaDifficulty arenaDifficulty)
        {
            Debug.LogWarning("m_ArenaOptionsUI.Initialize() : m_ArenaType = " + m_ArenaType);

            m_ArenaType = arenaType;
            m_ArenaDifficulty = arenaDifficulty;

            base.Initialize();

            var arenaMods = PlayerPrefsHandler.GetArenaMods(arenaType, arenaDifficulty);
            m_ArenaModsDropdown.Initialize(typeof(EArenaMod), arenaMods.Count == 0 ? SpellFilterDropdown.CLEAR_VALUE : arenaMods[0].ToString(), withClearValue: true, allowNone: false);
            m_ArenaExtraDifficulty.Initialize(arenaType, arenaDifficulty);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            if (m_ArenaDifficulty != ProgressionCloudData.MaxArenaDifficulty && m_ArenaDifficulty >= ProgressionCloudData.GetUnlockedArenaDifficulty(m_ArenaType))
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        #endregion


        #region GUI Manipulators

        public void Lock(bool isLocked = true)
        {
            m_ArenaModsDropdown.Dropdown.interactable = ! isLocked;
            m_ArenaExtraDifficulty.MinusButton.interactable = ! isLocked;
            m_ArenaExtraDifficulty.PlusButton.interactable = ! isLocked;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_ArenaModsDropdown.OnDropdownValueChanged += OnArenaModChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_ArenaModsDropdown.OnDropdownValueChanged -= OnArenaModChanged;
        }

        void OnArenaModChanged(string newValue)
        {
            PlayerPrefsHandler.SetArenaMods(m_ArenaType, m_ArenaDifficulty, m_ArenaModsDropdown.GetValues<EArenaMod>());
            ArenaModsChangeEvent?.Invoke();
        }

        #endregion
    }
}
