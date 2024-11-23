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
        Casting,
        Success,
    }

    public class TaskUseSpell : Node
    {
        #region Members

        /// <summary> Caster Controller </summary>
        protected Controller    m_Controller;
        /// <summary> Spell to cast </summary>
        protected ESpell        m_Spell;
        /// <summary> Delay (in seconds) before starting this task </summary>
        protected float         m_Delay;
        /// <summary> Number of times in a row this spell must be used to return SUCCESS </summary
        protected int           m_NTimes;
        /// <summary> Max number of times this spell should be used before beeing deactivated </summary
        protected int           m_MaxTimes;
        /// <summary> ESpellEvent to await to count increase m_NTimesCounter </summary
        protected ESpellEvent   m_SpellEventToAwait;
        /// <summary> reset cooldown before casting ? </summary>
        protected bool          m_ResetCooldown;
        /// <summary> state returned when the spell cant be casted </summary>
        protected NodeState     m_CantCastState;

        protected bool          m_IsSuccessReturned;
        protected float         m_Timer;
        protected float         m_NTimesCounter;
        protected float         m_MaxTimesCounter;

        ECastState m_CastState;

        #endregion


        #region Init & End

        /// <summary>
        /// Make the AI use a spell
        /// </summary>
        /// <param name="controller">       Caster Controller                                                       </param>
        /// <param name="spell">            Spell to cast                                                           </param>
        /// <param name="delay">            Delay (in seconds) before starting this task                            </param>
        /// <param name="nTimes">           Number of times in a row this spell must be used to return SUCCESS      </param>
        /// <param name="maxTimes">         Max number of times this spell should be used before beeing deactivated </param>
        /// <param name="spellEvent">       ESpellEvent to await to count increase m_NTimesCounter                  </param>
        /// <param name="resetCooldown">    reset cooldown before casting ?                                         </param>
        /// <param name="cantCastState">    state returned when the spell cant be casted                            </param>
        public TaskUseSpell(Controller controller, ESpell spell, float delay = 0f, int nTimes = 1, int maxTimes = -1, ESpellEvent spellEvent = ESpellEvent.OnSpawn, bool resetCooldown = false, NodeState cantCastState = NodeState.FAILURE)
        {
            m_CastState = ECastState.Inactive;

            m_Controller        = controller;
            m_Spell             = spell;
            m_Delay             = delay;
            m_NTimes            = nTimes;
            m_MaxTimes          = maxTimes;
            m_SpellEventToAwait = spellEvent;
            m_ResetCooldown     = resetCooldown;
            m_CantCastState     = cantCastState;

            m_Timer = delay;

            // check that spell exists to activate the node
            m_IsActivated = controller.SpellHandler.Spells.Contains(m_Spell);
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            // SUCCESS : Collect success and set to inactive
            if (m_CastState == ECastState.Success)
            {
                SetCastState(ECastState.Inactive);
                return NodeState.SUCCESS;
            }

            // Inactive : start timer
            if (m_CastState == ECastState.Inactive)
            {
                if (m_ResetCooldown)
                    m_Controller.SpellHandler.ResetCooldown(m_Spell);

                SetCastState(ECastState.TimerActivated);
            }
                
            // while timer is not done, this task is considerated as FAILURE
            if (m_CastState == ECastState.TimerActivated)
            {
                m_State = NodeState.FAILURE;
                ErrorHandler.Log("TaskUseSpell("+m_Spell.ToString()+") - " + m_State + " : waiting for timer to end (" + (Mathf.Round(m_Timer * 100) / 100) + ")", ELogTag.AITaskUseSpell);
                return m_State;
            }

            // wait for the Controller to start the cast
            if (m_CastState == ECastState.Casting)
            {
                // check is currently casting the spell
                if (m_Controller.SpellHandler.IsCasting && m_Controller.SpellHandler.SelectedSpell == m_Spell)
                {
                    m_State = NodeState.RUNNING;
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : currently casting", ELogTag.AITaskUseSpell);
                    return m_State;
                }

                if (m_Controller.SpellHandler.GetCooldown(m_Spell) > 0)
                {
                    if (! m_ResetCooldown)
                    {
                        m_State = NodeState.FAILURE;
                        ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : spell is in cooldown", ELogTag.AITaskUseSpell);
                        return m_State;
                    }

                    m_Controller.SpellHandler.ResetCooldown(m_Spell);
                }

                // try to cast the spell
                if (m_Controller.SpellHandler.TryStartCastSpell(m_Spell, out string reason))
                {
                    // in case of instant success
                    if (m_State == NodeState.SUCCESS)
                    {
                        SetCastState(ECastState.Inactive);
                        return NodeState.SUCCESS;
                    }

                    m_State = NodeState.RUNNING;
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : started cast", ELogTag.AITaskUseSpell);
                    return m_State;
                }

                // unable to cast for other reason
                m_State = m_CantCastState;
                ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : " + reason, ELogTag.AITaskUseSpell);
                return m_State;
            }

            ErrorHandler.Log("TaskUseSpell("+m_Spell.ToString()+") - " + m_State, ELogTag.AITaskUseSpell);
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
                    break;

                case ECastState.TimerActivated:
                    m_State = NodeState.FAILURE;

                    if (m_Delay > 0)
                        m_Controller.StartCoroutine(ActivateTimer());
                    else
                        SetCastState(ECastState.Casting);

                    break;

                case ECastState.Casting:
                    m_State = NodeState.RUNNING;
                    m_NTimesCounter = 0;

                    m_Controller.SpellHandler.OnPreSpellEvent += CountAttacks;
                    break;

                case ECastState.Success:
                    m_State = NodeState.SUCCESS;

                    // remove counter listener
                    m_Controller.SpellHandler.OnPreSpellEvent -= CountAttacks;
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
            if (spellName != m_Spell.ToString() || spellEvent != m_SpellEventToAwait)
                return;

            m_NTimesCounter++;
            m_MaxTimesCounter++;
            ErrorHandler.Log("      -- TaskUseSpell("+m_Spell.ToString()+") : m_NTimesCounter = " + m_NTimesCounter, ELogTag.AITaskUseSpell);

            // number of attacks reached 
            if (m_NTimesCounter >= m_NTimes)
            {
                SetCastState(ECastState.Success);
            }
        }

        #endregion
    }
}