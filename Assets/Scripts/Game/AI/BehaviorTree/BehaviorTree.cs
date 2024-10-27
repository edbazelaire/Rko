using Game;
using Managers;
using System;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace AI
{
    public abstract class BehaviorTree : NetworkBehaviour
    {
        #region Members

        private Node m_Root = null;
        protected Controller m_Controller;
        protected bool m_IsActivated = false;

        protected float m_Randomness = 0f;
        protected float m_DecisionRefresh = 0f;
        
        protected float m_DecisionTimer = 0f;

        public Controller Controller => m_Controller;
        public bool IsActivated => m_IsActivated;
        public float Randomness => m_Randomness;
        public float DecisionRefresh => m_DecisionRefresh;

        #endregion


        #region Init & End

        public virtual void Initialize(SBotData botData)
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_Root = SetupTree();

            m_Randomness        = botData.Randomness;
            m_DecisionRefresh   = botData.DecisionRefresh;

            m_IsActivated = false;
        }

        public virtual void Activate(bool activated = true)
        {
            m_IsActivated = activated;
            m_Controller.AutoAttackHandler.enabled = activated;

            if (activated == false && IsServer) 
            {
                m_Controller.Movement.MoveX.Value = 0;
                m_Controller.AnimationHandler.CancelCastAnimation();
            }
        }

        #endregion

        protected virtual void Update()
        {
            if (!m_IsActivated || !IsServer)
                return;

            if (m_DecisionTimer > 0f)
            {
                m_DecisionTimer -= Time.deltaTime;
                return;
            }

            if (GameManager.IsGameOver)
            {
                Activate(false);
                return;
            }

            m_DecisionTimer = m_DecisionRefresh;

            if (m_Root != null)
                m_Root.Evaluate();
        }

        protected abstract Node SetupTree();
    }

}
