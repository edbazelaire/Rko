using Assets.Scripts.Managers;
using Enums;
using Menu.Common.Filters;
using Menu.PopUps;
using MyBox;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu.MainTab
{
    public class ArenaOptionsUI : MObject
    {
        #region Members

        // =================================================================================
        // GameObject & Components
        CurrentArenaModsDisplayer m_CurrentArenaModsDisplayer;
        Button m_Button;
        TMP_Text m_Replacement;

        // =================================================================================
        // Local Data
        EArenaType m_ArenaType              = EArenaType.None;
        EArenaDifficulty m_ArenaDifficulty;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CurrentArenaModsDisplayer = Finder.FindComponent<CurrentArenaModsDisplayer>(gameObject);
            m_Button = Finder.FindComponent<Button>(gameObject, "Button");
            m_Replacement = Finder.FindComponent<TMP_Text>(gameObject, "Replacement");
        }

        public virtual void Initialize(EArenaType arenaType, EArenaDifficulty arenaDifficulty)
        {
            if (arenaType == EArenaType.None)
            {
                ErrorHandler.Warning("Trying to set arena with arena type : " + arenaType);
                return;
            }

            m_ArenaType = arenaType;
            m_ArenaDifficulty = arenaDifficulty;

            base.Initialize();

            m_CurrentArenaModsDisplayer.Initialize();
            CheckIsEmpty();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            if (m_ArenaType == EArenaType.None)
            {
                ErrorHandler.Warning("Trying to set arena with arena type : " + m_ArenaType);
                return;
            }

            // display options only if conditions are met
            gameObject.SetActive(ShouldDisplay());
        }

        void CheckIsEmpty()
        {
            m_Replacement.gameObject.SetActive(PlayerPrefsHandler.CurrentArenaMods.IsNullOrEmpty());
        }

        bool ShouldDisplay()
        {
            return m_ArenaDifficulty >= EArenaDifficulty.Brutal &&
               ProgressionCloudData.IsCompleted(m_ArenaType, m_ArenaDifficulty);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnButtonClicked);
            PlayerPrefsHandler.ArenaModsChangedEvent        += CheckIsEmpty;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveAllListeners();
            PlayerPrefsHandler.ArenaModsChangedEvent -= CheckIsEmpty;
        }

        void OnButtonClicked()
        {
            ScreenManager.SetPopUp(EPopUpState.ArenaOptionsPopUp);
        }

        #endregion
    }
}
