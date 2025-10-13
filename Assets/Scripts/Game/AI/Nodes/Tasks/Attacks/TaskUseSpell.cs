using AI;
using AI.Checkers;
using Enums;
using Game.Character;
using Game.Spells;
using System;
using System.Collections;
using Tools;
using UnityEngine;

namespace Game.AI
{
    enum ECastState
    {
        Inactive,
        TimerActivated,
        StartCast,
        Casting,
        WaitingCallback,
        Success,
    }

    public class TaskUseSpell : BaseTask
    {
        #region Members

        /// <summary> Spell to cast </summary>
        protected ESpell        m_Spell;
        /// <summary> Delay (in seconds) before starting this task </summary>
        protected float         m_Delay;
        /// <summary> Time to wait before using another ability </summary>
        protected float         m_GlobalCooldown;
        /// <summary> Number of times in a row this spell must be used to return SUCCESS </summary
        protected int           m_NTimes;
        /// <summary> ESpellEvent to await to count increase m_NTimesCounter </summary
        protected ESpellEvent   m_SpellEventToAwait;
        /// <summary> reset cooldown before casting ? </summary>
        protected bool          m_ResetCooldown;
        /// <summary> ignore the global cooldowns ? </summary>
        protected bool          m_IgnoreGlobalCooldown;
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
        /// <param name="controller">           Caster Controller                                                       </param>
        /// <param name="spell">                Spell to cast                                                           </param>
        /// <param name="delay">                Delay (in seconds) before starting this task                            </param>
        /// <param name="globalCooldown">       Time to wait before using another ability                               </param>
        /// <param name="nTimes">               Number of times in a row this spell must be used to return SUCCESS      </param>
        /// <param name="spellEvent">           ESpellEvent to await to count increase m_NTimesCounter                  </param>
        /// <param name="resetCooldown">        reset cooldown before casting ?                                         </param>
        /// <param name="ignoreGlobalCooldown"> ignore the global cooldowns ?                                           </param>
        /// <param name="cantCastState">        state returned when the spell cant be casted                            </param>
        public TaskUseSpell(Controller controller, ESpell spell, float delay = 0f, float globalCooldown = 0f, int nTimes = 1, ESpellEvent spellEvent = ESpellEvent.None, bool resetCooldown = false, bool ignoreGlobalCooldown = false, NodeState cantCastState = NodeState.FAILURE, Func<float> weight = null) : base(controller, weight) 
        {
            m_CastState = ECastState.Inactive;

            m_Spell                 = spell;
            m_Delay                 = delay;
            m_GlobalCooldown        = globalCooldown;
            m_NTimes                = nTimes;
            m_SpellEventToAwait     = spellEvent;
            m_ResetCooldown         = resetCooldown;
            m_IgnoreGlobalCooldown  = ignoreGlobalCooldown;
            m_CantCastState         = cantCastState;

            m_Timer = delay;

            // check that spell exists to activate the node
            CheckActivation();
        }

        void CheckActivation()
        {
            m_IsActivated = m_Controller.SpellHandler.Spells.Contains(m_Spell);

            if (m_IsActivated)
                ErrorHandler.Log("TaskUseSell("+m_Spell+") : Deactivated", ELogTag.AITaskUseSpell);
        }

        #endregion

         
        #region Evaluate & Reset

        public override NodeState Evaluate()
        {
            // SUCCESS : Collect success and set to inactive
            if (m_CastState == ECastState.Success)
            {
                SetCastState(ECastState.Inactive);
                return NodeState.SUCCESS;
            }

            // CHECK : already casting
            if (m_CastState <= ECastState.TimerActivated)
            {
                if (m_Controller.SpellHandler.IsCasting && m_Controller.SpellHandler.SelectedSpell == m_Spell.ToString())
                    SetCastState(ECastState.Casting);
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

            // try to start the casting of the spell
            if (m_CastState == ECastState.StartCast)
            {
                m_State = NodeState.FAILURE;

                // check is currently casting the spell
                if (m_Controller.SpellHandler.IsCasting && m_Controller.SpellHandler.SelectedSpell == m_Spell.ToString())
                {
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : already casting", ELogTag.AITaskUseSpell);
                    SetCastState(ECastState.Casting);
                    return m_State;
                }

                // CHECK : already casting spell 
                if (m_Controller.SpellHandler.IsCastingNotInterruptable)
                {
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : casting not interruptable spell", ELogTag.AITaskUseSpell);
                    return m_State;
                }

                // CHECK : cooldown over 
                if (m_Controller.SpellHandler.GetCooldown(m_Spell) > 0)
                {
                    if (!m_ResetCooldown)
                    {
                        m_State = NodeState.FAILURE;
                        ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : spell is in cooldown", ELogTag.AITaskUseSpell);
                        return m_State;
                    }

                    m_Controller.SpellHandler.ResetCooldown(m_Spell);
                }

                // CHECK : global cooldown for spells
                if (!m_IgnoreGlobalCooldown && m_Controller.BehaviorTree.GetTimer("GlobalCooldown") > 0)
                {
                    m_State = NodeState.FAILURE;
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : waiting the end of global cooldown", ELogTag.AITaskUseSpell);
                    return m_State;
                }

                // try to cast the spell
                if (m_Controller.SpellHandler.TryStartCastSpell(m_Spell, m_Controller.CharacterLevel, out string reason))
                {
                    // in case of instant success
                    if (m_State == NodeState.SUCCESS)
                    {
                        SetCastState(ECastState.Inactive);
                        return NodeState.SUCCESS;
                    }

                    SetCastState(ECastState.Casting);
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : started cast", ELogTag.AITaskUseSpell);
                    return m_State;
                }

                m_State = NodeState.FAILURE;
                ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : unable to cast - " + reason, ELogTag.AITaskUseSpell);
                return m_State;
            }

            // check that the casting is going through
            if (m_CastState == ECastState.Casting)
            {
                // in case a change in current state has occured
                if (m_State != NodeState.RUNNING)
                {
                    SetCastState(ECastState.Inactive);
                    return m_State;
                }

                // check is currently casting the spell
                if (! m_Controller.SpellHandler.IsCasting)
                {
                    ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : cast is over", ELogTag.AITaskUseSpell);
                    SetCastState(ECastState.StartCast);
                    return m_State;
                }

                ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : currently casting", ELogTag.AITaskUseSpell);
                return m_State;
            }

            // waiting for the spell callback to provide 
            if (m_CastState == ECastState.WaitingCallback)
            {
                // in case a change in current state has occured
                if (m_State != NodeState.RUNNING)
                {
                    SetCastState(ECastState.Inactive);
                    return m_State;
                }

                ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : waiting for callback", ELogTag.AITaskUseSpell);
                return m_State;
            }

            ErrorHandler.Log("TaskUseSpell("+m_Spell.ToString()+") - " + m_State, ELogTag.AITaskUseSpell);
            return m_State;
        }

        public override void Reset()
        {
            base.Reset();
        }

        #endregion


        #region Methods

        void SetCastState(ECastState castState)
        {
            if (m_CastState == castState)
                return;

            m_CastState = castState;

            ErrorHandler.Log("      -- TaskUseSpell(" + m_Spell.ToString() + ") : m_CastState = " + m_CastState, ELogTag.AITaskUseSpell);

            switch (castState)
            {
                case ECastState.Inactive:
                    m_State = NodeState.FAILURE;                                // set default state as FAILURE
                    m_NTimesCounter = 0;                                        // reset counter
                    m_Controller.SpellHandler.OnPreSpellEvent -= AwaitCastEvent;
                    m_Controller.SpellHandler.OnPreSpellEvent -= CountAttacks;  // remove counter listener
                    Spell.OnSpellSpawn -= AttachCallback;
                    break;

                case ECastState.TimerActivated:
                    m_State = NodeState.FAILURE;
                    m_Controller.StartCoroutine(ActivateTimer());
                    break;

                case ECastState.StartCast:
                    m_State = NodeState.RUNNING;
                    m_Controller.Movement.SetMovement(0);
                    break;

                case ECastState.Casting:
                    m_State = NodeState.RUNNING;

                    // cancel movement on attacking
                    if (m_SpellEventToAwait == ESpellEvent.None) { }
                    else if (m_SpellEventToAwait < ESpellEvent.OnSpawn)
                        m_Controller.SpellHandler.OnPreSpellEvent += CountAttacks;
                    else
                        Spell.OnSpellSpawn += AttachCallback;

                    m_Controller.SpellHandler.OnPreSpellEvent += AwaitCastEvent;
                    break;

                case ECastState.WaitingCallback:
                    if (m_SpellEventToAwait == ESpellEvent.None)
                        SetCastState(ECastState.Success);
                    break;

                case ECastState.Success:
                    m_State = NodeState.SUCCESS;
                    if (m_GlobalCooldown > 0)
                        m_Controller.BehaviorTree.ResetTimer("GlobalCooldown", m_GlobalCooldown);
                    break;
            }
        }

        IEnumerator ActivateTimer()
        {
            m_Timer = m_Delay;
            while (m_Timer > 0)
            {
                if (m_CastState != ECastState.TimerActivated)
                    yield break;

                m_Timer -= Time.deltaTime;
                yield return null;
            }

            SetCastState(ECastState.StartCast);
        }

        #endregion


        #region Listeners

        void AttachCallback(Spell spell)
        {
            if (spell.SpellData.Name != m_Spell.ToString() || spell.Caster != m_Controller)
                return;

            spell.OnSpellEvent += (ESpellEvent spellEvent) => CountAttacks(m_Spell.ToString(), spellEvent);
        }

        void AwaitCastEvent(string spellName, ESpellEvent spellEvent)
        {
            if (spellName != m_Spell.ToString() || spellEvent != ESpellEvent.OnCast)
                return;

            ErrorHandler.Log("TaskUseSpell(" + m_Spell.ToString() + ") - " + m_State + " : received cast event - " + spellEvent, ELogTag.AITaskUseSpell);
            SetCastState(ECastState.WaitingCallback);
        }

        void CountAttacks(string spellName, ESpellEvent spellEvent)
        {
            if (spellName != m_Spell.ToString() || spellEvent != m_SpellEventToAwait)
                return;

            m_NTimesCounter++;
            ErrorHandler.Log("      -- TaskUseSpell("+ spellName + ") - "+ spellEvent + " : m_NTimesCounter = " + m_NTimesCounter + " / " + m_NTimes, ELogTag.AITaskUseSpell);

            // number of attacks reached 
            if (m_NTimesCounter >= m_NTimes)
            {
                SetCastState(ECastState.Success);
            }
        }

        #endregion


        #region Info

        public override string GetInfo()
        {
            string info = m_CastState.ToString();
            if (m_CastState >= ECastState.Casting)
                info += $" : {m_NTimesCounter}/{m_NTimes}";

            return info;
        }

        #endregion
    }
}