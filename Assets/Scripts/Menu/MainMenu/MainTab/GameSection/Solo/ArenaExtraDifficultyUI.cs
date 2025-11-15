using Enums;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu.MainTab
{
    public class ArenaExtraDifficultyUI : MObject
    {
        #region Members

        // =================================================================================
        // GameObjects & Components
        Button          m_MinusButton;
        Button          m_PlusButton;
        GameObject      m_ValueDisplayer;

        // =================================================================================
        // Local Data
        EArenaType          m_ArenaType;
        EArenaDifficulty    m_ArenaDifficulty;
        int                 m_Value;

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
            m_ValueDisplayer = Finder.Find(gameObject, "ValueDisplayer");
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

            SetValue(PlayerPrefsHandler.CurrentArenaExtraDifficulty);
            RefreshButtons();
        }

        #endregion


        #region GUI Manipulators

        void SetValue(int value)
        {
            if (value > 5 || value < 0)
                return;

            // update value
            m_Value = value;

            // display N times the "Skull" to represent the difficulty
            UIHelper.DisplayIconCount(value, AssetLoader.Load<Sprite>("Skull_2_White", AssetLoader.c_OtherUIPath), m_ValueDisplayer.transform);

            // save in player prefs (this send an event)
            //PlayerPrefsHandler.SetArenaExtraDifficulty(m_ArenaType, m_ArenaDifficulty, value);
        }

        void RefreshButtons()
        {
            m_MinusButton.gameObject.SetActive(! ProgressionCloudData.HasArenaInProgress);
            m_PlusButton.gameObject.SetActive(! ProgressionCloudData.HasArenaInProgress);
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
