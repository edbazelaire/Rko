using System.Collections;
using Tools;
using UnityEngine;

namespace Game.Character.BotComponents
{
    public class BotEmotHandler : MObject
    {
        #region Members

        Controller m_Controller;
        Controller m_Target;

        /// <summary> is this compentent currently activated ? </summary>
        bool m_IsActivated;

        ///// <summary> value between 0 (calm) -> 1 (frustrated) </summary>
        //float m_FrustrationMeter;
        ///// <summary> value between 0 (stressed) -> 1 (overconfident) </summary>
        //float m_ConfidenceMeter;
        ///// <summary> value between 0 (fairplay) -> 1 (trolling) </summary>
        //float m_TrollMeter;
        ///// <summary> value between 0 (unfocused) -> 1 (focused) </summary>
        //float m_FocusMeter;

        /// <summary> value between 0 (losing) -> 1 (winning) </summary>
        float m_WinMeter => m_Controller.Life.PercHp / (m_Controller.Life.PercHp + m_Target.Life.PercHp);

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_Target = GameManager.Instance.GetFirstEnemy(m_Controller.Team);
        }

        public override void Initialize()
        {
            base.Initialize();
            ResetMeters();
            Activate(true);
        }

        void ResetMeters()
        {
            //m_FrustrationMeter = 0.5f;
            //m_ConfidenceMeter = 0.5f;
            //m_TrollMeter = 0.5f;
            //m_FocusMeter = 0.5f;
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!m_IsActivated)
                return;

            CheckUseEmot();
        }

        void CheckUseEmot()
        {

        }

        #endregion


        #region Activation

        public void Activate(bool activate)
        {
            m_IsActivated = activate;
        }

        #endregion


        #region External Influences

        void OnOpponentEmot()
        {
            
        }

        public void OnMissAction()
        {
            //m_FrustrationMeter += 0.2f;
            //m_ConfidenceMeter -= 0.1f;
        }

        public void OnSuccessfulAction()
        {
            //m_FrustrationMeter -= 0.1f;
            //m_ConfidenceMeter += 0.15f;
        }

        #endregion
    }
}
