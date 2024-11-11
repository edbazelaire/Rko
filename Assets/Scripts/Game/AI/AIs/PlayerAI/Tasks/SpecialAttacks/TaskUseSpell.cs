using AI;
using Enums;
using System.Collections;
using Tools;
using UnityEngine;

namespace Game.AI
{
    enum ECastState
    {
        Inactive,
        TimerActivated,
        WaitingCastStarted,
        Casting
    }

    public class TaskUseSpell : Node
    {
        #region Members

        protected Controller m_Controller;
        protected ESpell m_Spell;
        protected float m_Delay;
        protected int m_NTimes;
        protected int m_MaxTimes;

        protected float m_Timer;
        protected float m_NTimesCounter;
        protected float m_MaxTimesCounter;

        ECastState m_CastState;

        #endregion


        #region Init & End

        public TaskUseSpell(Controller controller, ESpell spell, float delay = 0f, int nTimes = 1, int maxTimes = -1)
        {
            m_CastState = ECastState.Inactive;

            m_Controller = controller;
            m_Spell = spell;
            m_Delay = delay;
            m_NTimes = nTimes;
            m_MaxTimes = maxTimes;

            m_Timer = delay;

            //// check that spell exists to activate the node
            //m_IsActivated = controller.SpellHandler.Spells.Contains(m_Spell);
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            // Inactive : start timer
            if (m_CastState == ECastState.Inactive)
                SetCastState(ECastState.TimerActivated);

            // while timer is not done, this task is considerated as FAILURE
            if (m_CastState == ECastState.TimerActivated)
            {
                m_State = NodeState.FAILURE;
                ErrorHandler.Log("TaskUseSpell("+m_Spell.ToString()+") : " + m_State, ELogTag.AITaskUseSpell);
                return m_State;
            }

            // wait for the Controller to start the cast
            if (m_CastState == ECastState.Casting)
            {
                // wait end of cooldown
                if (m_Controller.SpellHandler.GetCooldown(m_Spell) > 0)
                {
                    m_State = NodeState.FAILURE;
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : the spell is currently in cooldown", ELogTag.AITaskUseSpell);
                    return m_State;
                }

                m_State = NodeState.RUNNING;

                // NOT CASTING : try to cast the spell
                if (! m_Controller.SpellHandler.IsCasting)
                {
                    m_Controller.SpellHandler.TryStartCastSpell(m_Spell);
                } 
            }

            ErrorHandler.Log("TaskUseSpell("+m_Spell.ToString()+") : " + m_State, ELogTag.AITaskUseSpell);
            return m_State;
        }

        #endregion


        #region Methods

        void SetCastState(ECastState castState)
        {
            m_CastState = castState;

            ErrorHandler.Log("      -- TaskUseSpell("+m_Spell.ToString()+") : m_CastState = " + m_CastState, ELogTag.AITaskUseSpell);

            switch (castState)
            {
                case ECastState.Inactive:
                    m_State = NodeState.FAILURE;

                    // remove counter listener
                    m_Controller.SpellHandler.OnPreSpellEvent -= CountAttacks;
                    break;

                case ECastState.TimerActivated:
                    m_State = NodeState.FAILURE;

                    m_Controller.StartCoroutine(ActivateTimer());
                    break;

                case ECastState.Casting:
                    m_State = NodeState.RUNNING;
                    m_NTimesCounter = 0;

                    m_Controller.SpellHandler.OnPreSpellEvent += CountAttacks;
                    break;
            }
        }

        IEnumerator ActivateTimer()
        {
            m_Timer = m_Delay;
            while (m_Timer > 0)
            {
                m_Timer -= Time.deltaTime;
                yield return null;
            }

            SetCastState(ECastState.Casting);
        }

        #endregion


        #region Listeners

        void CountAttacks(string spellName, ESpellEvent spellEvent)
        {
            if (spellName != m_Spell.ToString() || spellEvent != ESpellEvent.OnSpawn)
                return;

            m_NTimesCounter++;
            m_MaxTimesCounter++;
            ErrorHandler.Log("      -- TaskUseSpell("+m_Spell.ToString()+") : m_NTimesCounter = " + m_NTimesCounter, ELogTag.AITaskUseSpell);

            // number of attacks reached 
            if (m_NTimesCounter >= m_NTimes)
            {
                SetCastState(ECastState.Inactive);
            }
        }

        #endregion
    }
}