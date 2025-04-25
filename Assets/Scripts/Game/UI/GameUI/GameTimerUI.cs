using System.Collections;
using TMPro;
using Tools;
using UnityEngine;


namespace Game.UI.GameUI
{
    public class GameTimerUI : MObject
    {
        #region Members

        [SerializeField] int m_GameDuration = 240;

        TMP_Text m_Text;

        float m_Timer;
        public float Timer => m_Timer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Text = Finder.FindComponent<TMP_Text>(gameObject);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Timer = m_GameDuration;
            RefreshTimer();
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!GameManager.Exists || !GameManager.IsGameRunning || m_Timer == 0)
                return;

            m_Timer -= Time.deltaTime;

            if (m_Timer < 0)
                m_Timer = 0;

            RefreshTimer();

            if (m_Timer <= 0)
                GameManager.Instance.OnTimerEnd();
        }

        void RefreshTimer()
        {
            int minutes = (int)Mathf.Floor(m_Timer / 60);
            int seconds = (int)m_Timer - minutes * 60;
            string adj = seconds < 10 ? "0" : "";
            m_Text.text = $"{minutes}:{adj}{seconds}";
        }

        #endregion
    }
}
