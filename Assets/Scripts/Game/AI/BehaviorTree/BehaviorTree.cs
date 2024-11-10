using Enums;
using Game;
using Managers;
using PlayFab.MultiplayerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace AI
{
    public abstract class BehaviorTree : MonoBehaviour
    {
        #region Members

        List<string> NO_BT_CHARACTERS = new List<string>() { ESpawn.Stalacmite.ToString(), ESpawn.DarkVeil.ToString() };

        private Node m_Root = null;
        protected Controller m_Controller;
        protected bool m_IsActivated = false;

        protected Dictionary<string, float> m_Timers    = new Dictionary<string, float>();
        protected List<string> m_FrozenTimers           = new ();
        protected Dictionary<string, int> m_Counters    = new Dictionary<string, int>();
        protected float m_Randomness                    = 0f;
        protected float m_DecisionRefresh               = 0f;

        protected float m_DecisionTimer                 = 0f;

        public Controller Controller    => m_Controller;
        public bool IsActivated         => m_IsActivated;
        public float Randomness         => m_Randomness;
        public float DecisionRefresh    => m_DecisionRefresh;

        #endregion


        #region Init & End

        public virtual void Initialize(SBotData botData)
        {
            m_IsActivated = false;
            m_Controller = Finder.FindComponent<Controller>(gameObject);

            if (NO_BT_CHARACTERS.Contains(m_Controller.Character))
            {
                Destroy(this);
                return;
            }

            if (!m_Controller.IsServer)
                return;

            m_Root = SetupTree(botData.ArenaDifficulty);

            m_Timers            = new Dictionary<string, float>();
            m_Randomness        = botData.Randomness;
            m_DecisionRefresh   = botData.DecisionRefresh;
        }

        public virtual void Activate(bool activated = true)
        {
            if (!m_Controller.IsServer)
                return;
            
            m_IsActivated = activated;
            m_Controller.AutoAttackHandler.enabled = activated;

            if (activated == false && m_Controller.IsServer) 
            {
                m_Controller.Movement.MoveX.Value = 0;
                m_Controller.AnimationHandler.CancelCastAnimation();
            }
        }

        #endregion

        protected virtual void Update()
        {
            if (!m_IsActivated || !m_Controller.IsServer)
                return;

            // update all timers
            List<string> allIds = m_Timers.Keys.ToList();
            foreach (string id in allIds)
            {
                if (!m_Timers.ContainsKey(id) || IsTimerFrozen(id))
                    continue;

                if (m_Timers[id] > 0)
                    m_Timers[id] -= Time.deltaTime;
            }

            // update decision refresh
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

        protected abstract Node SetupTree(EArenaDifficulty arenaDifficulty);


        #region Timers

        public bool CheckTimer(string id, float timer)
        {
            if (! m_Timers.ContainsKey(id))
                m_Timers.Add(id, timer);

            if (IsTimerFrozen(id))
                FreezeTimer(id, false);

            return m_Timers[id] <= 0;
        }

        public void ResetTimer(string id, float timer)
        {
            m_Timers[id] = timer;
        }

        public void DeleteTimer(string id)
        {
            if (! m_Timers.ContainsKey(id))
            {
                ErrorHandler.Warning("Trying to delete timer " + id + " but this id was not found in list of timers");
                return;
            }

            // unfreeze timer on deletion
            if (IsTimerFrozen(id))
                FreezeTimer(id, false);

            // remove from timers
            m_Timers.Remove(id);
        }

        public bool IsTimerFrozen(string id)
        {
            return m_FrozenTimers.Contains(id);
        }

        public void FreezeTimer(string id, bool freeze)
        {
            if (! m_Timers.ContainsKey(id))
            {
                ErrorHandler.Warning("Trying to freeze timer " + id + " but this id was not found in list of timers");
            }

            if (freeze)
            {
                if (IsTimerFrozen(id))
                {
                    ErrorHandler.Warning("Trying to UN-FREEZE timer " + id + " but this id was not found in list of m_FreezeTimers");
                    return;
                }

                m_FrozenTimers.Add(id);
            }

            else
            {
                if (! IsTimerFrozen(id))
                {
                    ErrorHandler.Warning("Trying to UN-FREEZE timer " + id + " but this id was not found in list of m_FreezeTimers");
                    return;
                }

                m_FrozenTimers.Remove(id);
            }
        }

        #endregion


        #region Counters

        public bool CheckCounter(string id, int maxValue)
        {
            if (!m_Counters.ContainsKey(id))
                m_Counters.Add(id, 0);

            return m_Counters[id] < maxValue || maxValue <= -1;
        }

        public void ResetCounter(string id, int value = 0)
        {
            m_Counters[id] = value;
        }

        public void DeleteCounter(string id)
        {
            if (!m_Counters.ContainsKey(id))
            {
                ErrorHandler.Warning("Trying to delete Counter " + id + " but this id was not found in list of m_Counters");
                return;
            }

            m_Counters.Remove(id);
        }

        #endregion

    }

}
