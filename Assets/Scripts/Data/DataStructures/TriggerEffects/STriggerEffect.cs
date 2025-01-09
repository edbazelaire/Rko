using Assets.Scripts.Data.DataStructures;
using Assets.Scripts.Data.PowerUp;
using Enums;
using Game;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections;
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

        public  float                   Delay;
        public  float                   Duration;
        public  int                     NActivations;
        public  float                   Cooldown;

        Controller  m_Caster;
        Controller  m_TargetController;
        Coroutine   m_Coroutine;
        int         m_NActivationsCtr;
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

            serializer.SerializeValue(ref Delay);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref NActivations);
            serializer.SerializeValue(ref Cooldown);
        }

        #endregion


        #region Activation

        public bool IsActivable()
        {
            return (NActivations == -1          // infinite activations
                || m_NActivationsCtr < (NActivations >= 1 ? NActivations : 1)) // OR below min activation 
            && m_CooldownTimer <= 0;            // cooldown must be done
        }

        public void Activate(Controller controller)
        {
            if (m_IsActivated)
                return;

            Debug.LogWarning("Activating " + SpellDataName);
            
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

            if (SpellLoader.SpellExists(SpellDataName))
            {
                SpellData spellData = SpellLoader.GetSpellData(SpellDataName, Level);

                m_TargetController.StartCoroutine(spellData.CastDelay(m_TargetController.PlayerId, Vector3.zero, recalculateTarget: true));
                foreach (SpellPrefabSpawn prefabSpawn in spellData.SpellEventActions)
                {
                    if (prefabSpawn.GFXLifetime.StartSpellPart == ESpellEvent.OnCast)
                        prefabSpawn.Spawn(m_TargetController, spellData, null);
                }
            }

            else if (SpellLoader.StateEffectExists(SpellDataName))
            {
                m_TargetController.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(SpellDataName, Level), m_Caster);
            }

            else
                ErrorHandler.Error(SpellDataName + " not recognize either as Spell or StateEffect");
        }

        #endregion


        #region Deactivation / End

        public void End()
        {
            if (StateEffectEvent != EStateEffectEvent.None)
            {
                StateEffect.StateEffectEvent -= OnStateEffectEvent;
            }

            if (m_Coroutine != null)
            {
                m_TargetController.StopCoroutine(m_Coroutine);
            }
        }

        public void Deactivate()
        {
            if (!m_IsActivated)
                return;

            m_IsActivated = false;
            StateEffect.StateEffectEvent -= OnStateEffectEvent;

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

            if (SpellLoader.StateEffectExists(SpellDataName))
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
            if (m_Caster == null)
            {
                ErrorHandler.Error("Provided Controller is null for state effect : " + stateEffectName + " - at event " + stateEffectEvent);
                return;
            }

            if (! HasStateEffect(stateEffectName))
                return;

            if (stateEffectEvent != StateEffectEvent)
                return;

            if (casterId != m_Caster.PlayerId)
                return;

            m_TargetController = CalculateTarget(targetId);
            ActivateEffect();
        }

        #endregion
    }
}