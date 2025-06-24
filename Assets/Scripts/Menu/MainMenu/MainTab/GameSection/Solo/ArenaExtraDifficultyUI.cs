using Enums;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu.MainTab
{
    public class ArenaExtraDifficultyUI : MObject
    {
        #region Members

        // =================================================================================
        // Actions
        public Action ArenaExtraDifficultyChangeEvent;

        // =================================================================================
        // GameObjects & Components
        Button      m_MinusButton;
        Button      m_PlusButton;
        TMP_Text    m_ValueDisplayer;

        // =================================================================================
        // Local Data
        EArenaType m_ArenaType;
        EArenaDifficulty m_ArenaDifficulty;
        int m_Value;

        // =================================================================================
        // Public Accessors
        public int Value => m_Value;
        public Button MinusButton => m_MinusButton;
        public Button PlusButton => m_PlusButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_MinusButton = Finder.FindComponent<Button>(gameObject, "MinusButton");
            m_PlusButton = Finder.FindComponent<Button>(gameObject, "PlusButton");
            m_ValueDisplayer = Finder.FindComponent<TMP_Text>(gameObject, "ValueDisplayer");
        }

        public virtual void Initialize(EArenaType arenaType, EArenaDifficulty arenaDifficulty)
        {
            m_ArenaType = arenaType;
            m_ArenaDifficulty = arenaDifficulty;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetValue(PlayerPrefsHandler.GetArenaExtraDifficulty(m_ArenaType, m_ArenaDifficulty));
        }

        #endregion


        #region GUI Manipulators

        void SetValue(int value)
        {
            if (value > 5 || value < 0)
                return;

            m_Value = value;
            m_ValueDisplayer.text = new string('+', m_Value); ;

            PlayerPrefsHandler.SetArenaExtraDifficulty(m_ArenaType, m_ArenaDifficulty, value);

            ArenaExtraDifficultyChangeEvent?.Invoke();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            m_MinusButton.onClick.AddListener(OnMinus);
            m_PlusButton.onClick.AddListener(OnPlus);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            m_MinusButton.onClick.RemoveAllListeners();
            m_PlusButton.onClick.RemoveAllListeners();
        }

        void OnMinus()
        {
            SetValue(m_Value - 1);

        }

        void OnPlus()
        {
            SetValue(m_Value + 1);
        }

        #endregion
    }
}
