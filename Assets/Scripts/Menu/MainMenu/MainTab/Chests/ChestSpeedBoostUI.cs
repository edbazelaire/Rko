using Enums;
using Save;
using System;
using TMPro;
using Tools;

namespace Menu
{
    public class ChestSpeedBoostUI : MObject
    {
        #region Members

        TMP_Text m_TimerText;

        int m_ResetAt;

        #endregion


        #region Init & End

        private void Awake()
        {
            if (!TimeCloudData.HasBoost(EBoost.ChestSpeedBoost))
            {
                Activate(false);
                return;
            }

            Activate(true);
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TimerText = Finder.FindComponent<TMP_Text>(gameObject, "Timer");
        }

        #endregion


        #region GUI Manipulators

        private void Update()
        {
            if (!m_Initialized)
                return;

            int remainingTime = m_ResetAt - (int)(new DateTimeOffset(DateTime.UtcNow)).ToUnixTimeSeconds();
            if (remainingTime <= 0)
            {
                Activate(false);
                return;
            }

            m_TimerText.text = TextHandler.FormatTimestamp(remainingTime);
        }

        public void Activate(bool activate = true)
        {
            gameObject.SetActive(activate);

            if (!activate)
                return;

            m_ResetAt = TimeCloudData.GetBoost(EBoost.ChestSpeedBoost).Value.ResetAt;

            if (!m_Initialized)
                Initialize();
        }

        #endregion


        #region Register Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            TimeCloudData.BoostChangedEvent += OnBoostChangedEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            TimeCloudData.BoostChangedEvent -= OnBoostChangedEvent;
        }

        void OnBoostChangedEvent(string name, bool added)
        {
            if (name != EBoost.ChestSpeedBoost.ToString())
                return;

            Activate(added);
        }

        #endregion
    }
}

