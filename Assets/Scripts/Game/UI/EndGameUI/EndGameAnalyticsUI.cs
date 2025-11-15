using Assets.Scripts.Game;
using Enums;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.EndGameUI
{
    /// <summary>
    /// Main EndGame UI that shows analytics for the match.
    /// Displays summary (by HitType) and details (by spell).
    /// </summary>
    public class EndGameAnalyticsUI : MObject
    {
        #region Memners

        // GameObjects & Components
        Button m_CancelButton;
        List<EndGameAnalyticsDisplayerUI> m_EndGameAnalyticsDisplayers;
        Button m_DeltButton;
        Button m_TakenButton;
        Image m_DeltButtonBackground;
        Image m_TakenButtonBackground;

        // Local Data
        bool m_IsDisplayed = false;
        int m_CurrentIndex = -1;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CancelButton = Finder.FindComponent<Button>(gameObject, "CancelButton");
            m_DeltButton = Finder.FindComponent<Button>(gameObject, "DeltButton");
            m_DeltButtonBackground = Finder.FindComponent<Image>(m_DeltButton.gameObject);
            m_TakenButton = Finder.FindComponent<Button>(gameObject, "TakenButton");
            m_TakenButtonBackground = Finder.FindComponent<Image>(m_TakenButton.gameObject);

            m_EndGameAnalyticsDisplayers = Finder.FindComponents<EndGameAnalyticsDisplayerUI>(gameObject);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Activate / Deactivate display of Analytics
        /// </summary>
        public void ToggleDisplay()
        {
            gameObject.SetActive(!gameObject.activeInHierarchy);

            if (m_IsDisplayed || !gameObject.activeInHierarchy)
                return;

            DisplayIndex(0);
        }

        /// <summary>
        /// Select a "Tab" index
        /// </summary>
        /// <param name="index"></param>
        void DisplayIndex(int index)
        {
            if (index == m_CurrentIndex)
                return;

            // activate proper analytics displayer
            m_EndGameAnalyticsDisplayers[0].Activate(index == 0);
            m_EndGameAnalyticsDisplayers[1].Activate(index == 1);

            // set button as selected
            m_DeltButtonBackground.color = index == 0 ? Color.black : Color.white;
            m_TakenButtonBackground.color = index == 1 ? Color.black : Color.white;

            // set new current index
            m_CurrentIndex = index;
        }

        public EndGameAnalyticsDisplayerUI GetDisplayer(int index)
        {
            if (index >= m_EndGameAnalyticsDisplayers.Count)
            {
                ErrorHandler.Error("Unable to find displayer at index " +  index);
                return null;
            }

            return m_EndGameAnalyticsDisplayers[index];
        }

        /// <summary>
        /// Provide Analytics from outside (at the end of the game)
        /// </summary>
        /// <param name="spellHitTypeDatas"></param>
        /// <param name="index"></param>
        public void UpdateAnalytics(List<SSpellHitTypeData> spellHitTypeDatas, int index)
        {
            if (!m_Initialized)
                return;

            m_EndGameAnalyticsDisplayers[index].Initialize(spellHitTypeDatas);
        }

        public void AddSpecialValue(ESpecialValue specialValue, float value, int index)
        {
            m_EndGameAnalyticsDisplayers[index].AddSpecialValue(specialValue, value);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            m_CancelButton.onClick.AddListener(OnCancelButtonClicked);
            m_DeltButton.onClick.AddListener(() => DisplayIndex(0));
            m_TakenButton.onClick.AddListener(() => DisplayIndex(1));
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            m_CancelButton.onClick.RemoveAllListeners();
            m_DeltButton.onClick.RemoveAllListeners();
            m_TakenButton.onClick.RemoveAllListeners();
        }

        void OnCancelButtonClicked()
        {
            ToggleDisplay();
        }

        #endregion
    }
}
