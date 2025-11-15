using Enums;
using System;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Game.UI
{
    public class EmotUI : MObject
    {
        #region Members

        [SerializeField] float DISPLAY_TIME = 2f;

        Image m_EmotIcon;
        float m_Timer;
        bool m_IsTimerStarted = false;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_EmotIcon = Finder.FindComponent<Image>(gameObject, "EmotIcon");
        }

        public void Initialize(string emotName, bool startTimer = true)
        {
            if (! Enum.TryParse(emotName, out EEmot emot))
            {
                ErrorHandler.Warning("Unable to set " + emotName + " as emot");
                Destroy(gameObject);
                return;
            }

            Initialize(emot, startTimer);
        }

        public void Initialize(EEmot emot, bool startTimer = true)
        {
            base.Initialize();
            m_IsTimerStarted = false;

            SetEmot(emot);

            if (startTimer)
                StartTimer();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        public void End()
        {
            Destroy(gameObject);
        }

        #endregion


        #region Update

        private void LateUpdate()
        {
            if (!m_IsTimerStarted)
                return;

            // Freeze rotation (world-space)
            transform.rotation = Quaternion.identity;

            if (m_Timer > 0)
            {
                m_Timer -= Time.deltaTime;
                return;
            }

            End();
        }

        public void StartTimer(float? timer = null)
        {
            m_Timer = timer ?? DISPLAY_TIME;
            m_IsTimerStarted = true;
        }

        #endregion


        #region GUI Manipulators

        public void SetEmot(EEmot emot)
        {
            m_EmotIcon.sprite = AssetLoader.Load<Sprite>(emot.ToString(), AssetLoader.c_EmotsSpritePath);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
