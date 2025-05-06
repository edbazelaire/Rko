using Assets.Scripts.Data.DataStructures;
using Enums;
using Game;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Data.DataStructures
{
    public interface ITriggerEffect
    {
        public bool IsActivable();
        public void Activate(Controller controller) { }
    }

    [Serializable]
    public struct STriggerEffect : INetworkSerializable, ITriggerEffect
    {
        #region Members

        public  string                  SpellDataName;
        public  int                     Level;
        public  ESpellTarget            Target;
        
        public  ESpellActivation        SpellActivationEvent;
        public  float                   ActivationTreshold;
        public  ESpellActivation        SpellDeactivationEvent;
        public  float                   DeactivationTreshold;

        public  EStateEffectEvent       StateEffectEvent;
        public  string                  StateEffectName;
        public  int                     NStateEffectActivationThreshold;

        public  float                   Delay;
        public  float                   Duration;
        public  int                     NActivations;
        public  float                   Cooldown;

        // ==================================================================================
        // Data
        Controller  m_Caster;
        Controller  m_TargetController;
        Coroutine   m_Coroutine;
        int         m_NActivationsCtr;
        int         m_NStateEffectActivationCtr;
        float       m_CooldownTimer;
        bool        m_IsActivated;

        public bool IsActivated => m_IsActivated;

        #endregion


        #region Network Serialization

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SpellDataName);
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref Target);
            serializer.SerializeValue(ref SpellActivationEvent);
            serializer.SerializeValue(ref ActivationTreshold);
            serializer.SerializeValue(ref SpellDeactivationEvent);
            serializer.SerializeValue(ref DeactivationTreshold);

            serializer.SerializeValue(ref StateEffectEvent);
            serializer.SerializeValue(ref StateEffectName);
            serializer.SerializeValue(ref NStateEffectActivationThreshold);

            serializer.SerializeValue(ref Delay);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref NActivations);
            serializer.SerializeValue(ref Cooldown);
        }

        #endregion


        #region Activation

        public bool IsActivable()
        {
            return (NActivations == -1                                          // infinite activations
                || m_NActivationsCtr < (NActivations >= 1 ? NActivations : 1))  // OR below min activation
            && m_CooldownTimer <= 0;                                            // AND not in cooldown
        }

        public void Activate(Controller controller)
        {
            if (m_IsActivated)
                return;

            if (! IsActivable())
                return;

            if (controller == null)
            {
                ErrorHandler.Error("Provided Controller is null for " + SpellDataName);
                return;
            }

            m_IsActivated           = true;
            m_Caster                = controller;
            m_TargetController      = CalculateTarget();
            m_NActivationsCtr++;

            m_Coroutine = m_Caster.StartCoroutine(ActivationDelay());
        }

        IEnumerator ActivationDelay()
        {
            yield return new WaitForSeconds(Delay);

            if (NStateEffectActivationThreshold > 1)
            {
                m_Caster.TriggerEffectHandler.QuestValueChanged?.Invoke(SpellDataName, NStateEffectActivationThreshold);
            }

            if (StateEffectEvent == EStateEffectEvent.None)
            {
                ActivateEffect();
            } 
            else
            {
                StateEffect.StateEffectEvent += OnStateEffectEvent;
            }

            // activate duration coroutine
            if (Duration > 0)
                m_Coroutine = m_Caster.StartCoroutine(DurationCoroutine());
        }

        IEnumerator DurationCoroutine()
        {
            yield return new WaitForSeconds(Duration);

            End();
        }

        void ActivateEffect()
        {
            if (m_TargetController == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return;
            }

            // start cooldown
            if (Cooldown > 0)
            {
                m_CooldownTimer = Cooldown;
                m_TargetController.StartCoroutine(UpdateCooldownTimer());
            }

            if (SpellLoader.IsSpell(SpellDataName))
            {
                SpellData spellData = SpellLoader.GetSpellData(SpellDataName, Level);

                m_TargetController.StartCoroutine(spellData.CastDelay(m_TargetController.PlayerId, Vector3.zero, recalculateTarget: true));
            }

            else if (SpellLoader.IsStateEffect(SpellDataName))
            {
                m_TargetController.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(SpellDataName, Level), m_Caster);
            }

            else if (SpellLoader.PowerUpExists(SpellDataName))
            {
                SRunePower powerUp = SpellLoader.GetPowerUp(SpellDataName, Level);
                m_TargetController.TriggerEffectHandler.AddPowerUp(powerUp);
            }

            else
                ErrorHandler.Error(SpellDataName + " not recognize either as Spell or StateEffect");
        }

        #endregion


        #region Deactivation / End

        public void End()
        {
            if (GameManager.IsGameOver)
                return;

            Deactivate();

            if (m_TargetController == null)
                return;

            m_TargetController.TriggerEffectHandler.RemoveTriggerEffect(this);
        }

        public void Deactivate()
        {
            if (! m_IsActivated)
                return;

            StateEffect.StateEffectEvent -= OnStateEffectEvent;

            m_IsActivated = false;

            if (m_Caster == null)
            {
                ErrorHandler.Error("Unable to find CASTER controller when deactivating " + SpellDataName);
                return;
            }

            if (m_TargetController == null)
            {
                ErrorHandler.Error("Unable to find TARGET controller when deactivating " + SpellDataName);
                return;
            }

            if (m_Coroutine != null)
            {
                m_TargetController.StopCoroutine(m_Coroutine);
            }

            if (SpellLoader.IsStateEffect(SpellDataName))
                m_TargetController.StateHandler.RemoveStateEffect(SpellDataName);
        }

        #endregion


        #region Helpers

        IEnumerator UpdateCooldownTimer()
        {
            while (m_CooldownTimer > 0)
            {
                m_CooldownTimer -= Time.deltaTime;
                yield return null;
            }
        }

        bool HasStateEffect(string stateEffectName)
        {
            if (!StateEffectName.Contains(","))
            {
                return StateEffectName == stateEffectName;
            }

            return StateEffectName.Split(",").Contains(stateEffectName);
        }

        Controller CalculateTarget(ulong? targetId = null)
        {
            switch (Target)
            {
                case ESpellTarget.None:
                case ESpellTarget.Self:
                    return m_Caster;

                case ESpellTarget.CurrentTarget:
                    if (targetId.HasValue)
                        return GameManager.Instance.GetPlayer(targetId.Value);
                    return GameManager.Instance.GetFirstEnemy(m_Caster.Team);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(m_Caster.Team);

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(m_Caster.Team, m_Caster.PlayerId);

                default:
                    ErrorHandler.Warning("Unhandled case : " + Target);
                    return m_Caster;
            }
        }

        #endregion


        #region Listeners

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, ulong targetId, ulong casterId)
        {
            // SAFETY : has a caster provided
            if (m_Caster == null)
            {
                ErrorHandler.Error("Provided Controller is null for state effect : " + stateEffectName + " - at event " + stateEffectEvent);
                return;
            }

            // SAFETY : is still active
            if (! m_IsActivated)
            {
                //ErrorHandler.Error("Trying to activate effect (" + SpellDataName + ") that has been deactivated");
                return;
            }

            // CHECK : has the provided stateEffect as one of activation effect
            if (! HasStateEffect(stateEffectName))
                return;

            // CHECK : the event is the required one
            if (stateEffectEvent != StateEffectEvent)
                return;

            // CHECK : comes from the correct caster
            if (casterId != m_Caster.PlayerId)
                return;

            // QUEST : increase number of activations
            if (NStateEffectActivationThreshold > 0)
            {
                // increase counter of state activation
                m_NStateEffectActivationCtr++;

                // update decreasing counter of state effect activation
                m_Caster.TriggerEffectHandler.QuestValueChanged?.Invoke(SpellDataName, NStateEffectActivationThreshold - m_NStateEffectActivationCtr);

                if (m_NStateEffectActivationCtr < NStateEffectActivationThreshold)
                    return;
            }
            
            // ACTIVATE EFFECT
            m_TargetController = CalculateTarget(targetId);
            ActivateEffect();

            // if QUEST completed : end the effect
            if (NStateEffectActivationThreshold > 0 && m_NStateEffectActivationCtr >= NStateEffectActivationThreshold)
            {
                Deactivate();
            }
        }

        #endregion
    }
}