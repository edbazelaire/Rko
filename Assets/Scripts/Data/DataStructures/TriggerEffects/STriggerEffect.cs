using Data.DataStructures.PowerEffects;
using Enums;
using Game;
using Game.Loaders;
using Game.Spells;
using MyBox;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Data.DataStructures
{
    public interface ITriggerEffect
    {
        public bool IsActivable();
        public void Activate(Controller controller) { }
    }


    [Serializable]
    public class STriggerEffect : INetworkSerializable, ITriggerEffect
    {
        #region Members

        public  string                  SpellDataName;

        // TODO : ===============================================================================
        // TODO : Handle difference between Spell & StateEffect with SOverridingData<T> 
        public List<SStateEffectProperty> OverridingData;
        // TODO : ===============================================================================

        public  int                     Level;
        public  ESpellTarget            Target;
        
        public  ETriggerType            SpellActivationEvent;
        public  float                   ActivationTreshold;
        public  ETriggerType            SpellDeactivationEvent;
        public  float                   DeactivationTreshold;

        public  EStateEffectEvent       StateEffectEvent;
        public  string                  StateEffectName;
        public  int                     NStateEffectActivationThreshold;

        public  float                   Delay;
        public  float                   Duration;
        public  int                     NActivations;
        public  float                   Cooldown;
        public  bool                    RepeatWhenCooldownOver;
        public  bool                    ConsumeLifeOnActivation;

        // ==================================================================================
        // Data
        string      m_Parent;
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
            serializer.SerializeValue(ref RepeatWhenCooldownOver);
            serializer.SerializeValue(ref ConsumeLifeOnActivation);
        }

        #endregion


        #region Activation

        public bool IsActivable()
        {
            // TODO : Handle multiple "ExtraLife". For now, it is only checking one extra life effect
            if (ConsumeLifeOnActivation && ProgressionCloudData.CurrentArena.CurrentEnemyLifesLost > 0)
                return false;

            // CHECK : return has enough Activations left
            return (NActivations == -1                                          // infinite activations
                || m_NActivationsCtr < (NActivations >= 1 ? NActivations : 1))  // OR below min activation
            && m_CooldownTimer <= 0;                                            // AND not in cooldown
        }

        public void Activate(Controller controller, float delay = 0f)
        {
            if (m_IsActivated)
                return;

            Debug.Log("Activate Effect : " + SpellDataName);

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

            // check consume LIFE in Cloud
            CheckLifeConsumption();

            m_Coroutine = m_Caster.StartCoroutine(ActivationDelay(delay));
        }

        IEnumerator ActivationDelay(float extraDelay = 0f)
        {
            yield return new WaitForSeconds(Delay + extraDelay);

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
                m_TargetController.StartCoroutine(StartCooldownTimer());
            }

            if (SpellLoader.IsSpell(SpellDataName))
            {
                SpellData spellData = SpellLoader.GetSpellData(SpellDataName, Level);
                if (Target != ESpellTarget.None)
                    spellData.SpellTarget = Target;
                spellData.SetCurrentTargetId(m_TargetController.PlayerId);
                spellData.SetParent(m_Parent);
                m_Caster.StartCoroutine(spellData.CastDelay(m_Caster.PlayerId, Vector3.zero, recalculateTarget: true));
            }

            else if (SpellLoader.IsStateEffect(SpellDataName))
            {
                StateEffect stateEffect = SpellLoader.GetStateEffect(SpellDataName, Level, overridingData: OverridingData, parent: m_Parent);
                if (stateEffect.StateEffectName.StartsWith("_"))
                    stateEffect.SetParent(m_Parent);
                m_TargetController.StateHandler.AddStateEffect(stateEffect, m_Caster);
            }

            else if (SpellLoader.PowerUpExists(SpellDataName))
            {
                SPowerEffect powerUp = SpellLoader.GetPowerUp(SpellDataName, Level);
                m_TargetController.TriggerEffectHandler.AddPowerUp(powerUp);
            }

            else
                ErrorHandler.Error(SpellDataName + " not recognize either as Spell or StateEffect");
        }

        void CheckLifeConsumption()
        {
            if (! ConsumeLifeOnActivation)
            {
                return;
            }

            // remove enemy life in cloud
            ProgressionCloudData.RemoveCurrentEnemyLife(1);
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

            Debug.Log("Deactivate Effect : " + SpellDataName);

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

        public void SetParent(string parent)
        {
            if (parent.IsNullOrEmpty())
                return;

            m_Parent = parent;
        }

        IEnumerator StartCooldownTimer()
        {
            m_CooldownTimer = Cooldown;

            while (m_CooldownTimer > 0)
            {
                m_CooldownTimer -= Time.deltaTime;
                yield return null;
            }

            if (RepeatWhenCooldownOver && m_IsActivated && IsActivable())
                ActivateEffect();

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
                    ErrorHandler.Warning("Unhandled case - " + Target + " for TriggerEffect : " + SpellDataName);
                    return m_Caster;
            }
        }

        #endregion


        #region Listeners

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, int stacks, ulong targetId, ulong casterId, string parent)
        {
            // SAFETY : check is server
            if (!GameManager.Instance.IsServer)
                return;

            // SAFETY : has a caster provided
            if (m_Caster == null)
            {
                ErrorHandler.Error("Provided Controller is null for state effect : " + stateEffectName + " - at event " + stateEffectEvent);
                return;
            }

            // SAFETY : is still active
            if (! m_IsActivated)
            {
                return;
            }
            // CHECK : does the provided stateEffect have one of activation effect
            if (! HasStateEffect(stateEffectName))
                return;

            // CHECK : the event is the required one
            if (stateEffectEvent != StateEffectEvent)
                return;

            // CHECK : comes from the correct caster
            if (casterId != m_Caster.PlayerId)
                return;

            // CHECK : effect can be activated
            if (!IsActivable())
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


        #region Description

        public bool TryGetProperty(string property, out string value)
        {
            value = null;

            switch (property)
            {
                case nameof(SpellDataName): value = SpellDataName; return true;
                case nameof(Level): value = Level.ToString(); return true;
                case nameof(Target): value = Target.ToString(); return true;

                case nameof(SpellActivationEvent): value = SpellActivationEvent.ToString(); return true;
                case nameof(ActivationTreshold): value = ActivationTreshold.ToString(); return true;
                case nameof(SpellDeactivationEvent): value = SpellDeactivationEvent.ToString(); return true;
                case nameof(DeactivationTreshold): value = DeactivationTreshold.ToString(); return true;

                case nameof(StateEffectEvent): value = StateEffectEvent.ToString(); return true;
                case nameof(StateEffectName): value = StateEffectName; return true;
                case nameof(NStateEffectActivationThreshold): value = NStateEffectActivationThreshold.ToString(); return true;

                case nameof(Delay): value = Delay.ToString(); return true;
                case nameof(Duration): value = Duration.ToString(); return true;
                case nameof(NActivations): value = NActivations.ToString(); return true;
                case nameof(Cooldown): value = Cooldown.ToString(); return true;
                case nameof(RepeatWhenCooldownOver): value = RepeatWhenCooldownOver.ToString(); return true;
                case nameof(ConsumeLifeOnActivation): value = ConsumeLifeOnActivation.ToString(); return true;

                default:
                    return false;
            }
        }


        #endregion
    }
}