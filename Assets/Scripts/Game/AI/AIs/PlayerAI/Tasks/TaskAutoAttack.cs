using AI;
using Enums;
using Game.Character;
using System.Collections;
using Tools;
using UnityEngine;

namespace Game.AI
{
    public class TaskAutoAttack : Node
    {
        #region Members

        protected Controller m_Controller;
        protected bool m_IfAllowed;
        protected int m_NTimes;

        protected int m_NTimesCounter;

        protected Movement m_Movement => m_Controller.Movement;

        #endregion


        #region Init & End

        public TaskAutoAttack(Controller controller, bool ifAllowed = false, int nTimes = 0)
        {
            m_Controller = controller;
            m_IfAllowed = ifAllowed;
            m_NTimes = 0;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            // if auto attack only when allowed : check that the spell can be used
            if (m_IfAllowed)
            {
                if (! m_Controller.SpellHandler.CanCast(m_Controller.SpellHandler.AutoAttack))
                {
                    ErrorHandler.Log("TaskAutoAttack() - FAILURE : can not cast AutoAttack", ELogTag.AITaskAutoAttack);
                    m_State = NodeState.FAILURE;
                    return m_State;
                }
            }

            m_Movement.SetMovement(0);

            // no specific number of times, the action is a success
            if (m_NTimes <= 0)
            {
                ErrorHandler.Log("TaskAutoAttack() : SUCCESS", ELogTag.AITaskAutoAttack);
                m_State = NodeState.SUCCESS;
                return m_State;
            }

            if (m_State != NodeState.RUNNING)
            {
                ErrorHandler.Log("      -- TaskAutoAttack() : Start AutoAttack Count", ELogTag.AITaskAutoAttack);

                // init values before counting auto attacks
                m_State = NodeState.RUNNING;
                m_NTimesCounter = 0;

                // attach counter to auto attacks on spell events
                m_Controller.SpellHandler.OnPreSpellEvent += CountAutoAttacks;
            }

            ErrorHandler.Log("TaskAutoAttack() : " + m_State, ELogTag.AITaskAutoAttack);
            return m_State;
        }

        #endregion


        #region Listeners

        void CountAutoAttacks(string spellName, ESpellEvent spellEvent)
        {
            if (spellName != m_Controller.SpellHandler.AutoAttack.ToString() || spellEvent != ESpellEvent.OnSpawn)
                return;

            m_NTimesCounter++;
            ErrorHandler.Log("      -- TaskAutoAttack() : m_NTimesCounter = " + m_NTimesCounter, ELogTag.AITaskAutoAttack);

            // number of attacks reached 
            if (m_NTimesCounter >= m_NTimes)
            {
                // remove counter listener
                m_Controller.SpellHandler.OnPreSpellEvent -= CountAutoAttacks;
                // set state has successfull
                m_State = NodeState.SUCCESS;
            }
        }

        #endregion
    }
}