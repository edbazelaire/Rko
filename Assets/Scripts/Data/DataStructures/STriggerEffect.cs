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

        Controller  m_Controller;
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

            Debug.Log("Activated TriggerEffect : " + SpellDataName);

            if (controller == null)
            {
                ErrorHandler.Error("Provided Controller is null for " + SpellDataName);
                return;
            }

            m_IsActivated   = true;
            m_Controller    = controller;

            m_Coroutine = m_Controller.StartCoroutine(ActivationDelay());
        }

        IEnumerator ActivationDelay()
        {
            yield return new WaitForSeconds(Delay);

            if (StateEffectEvent == EStateEffectEvent.None)
            {
                ActivateEffect(CalculateTarget());
            } 
            else
            {
                StateEffect.StateEffectEvent += OnStateEffectEvent;
            }

            // activate duration coroutine
            if (Duration > 0)
                m_Coroutine = m_Controller.StartCoroutine(DurationCoroutine());
        }

        IEnumerator DurationCoroutine()
        {
            yield return new WaitForSeconds(Duration);

            End();
        }

        void ActivateEffect(Controller controller)
        {
            if (!IsActivable())
                return;

            if (controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return;
            }

            m_NActivationsCtr++;
            if (Cooldown > 0)
            {
                m_CooldownTimer = Cooldown;
                controller.StartCoroutine(UpdateCooldownTimer());
            }

            if (SpellLoader.SpellExists(SpellDataName))
            {
                SpellData spellData = SpellLoader.GetSpellData(SpellDataName, Level);

                controller.StartCoroutine(spellData.CastDelay(controller.PlayerId, Vector3.zero, recalculateTarget: true));
                foreach (SPrefabSpawn prefabSpawn in spellData.SpellEventActions)
                {
                    if (prefabSpawn.GFXLifetime.StartSpellPart == ESpellEvent.OnCast)
                        prefabSpawn.Spawn(controller, spellData, null);
                }
            }

            else if (SpellLoader.StateEffectExists(SpellDataName))
            {
                controller.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(SpellDataName, Level), m_Controller);
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
                m_Controller.StopCoroutine(m_Coroutine);
            }
        }

        public void Deactivate()
        {
            if (!m_IsActivated)
                return;

            m_IsActivated = false;
            StateEffect.StateEffectEvent -= OnStateEffectEvent;

            if (m_Controller == null)
            {
                ErrorHandler.Error("Unable to find controller when deactivating " + SpellDataName);
                return;
            }

            if (SpellLoader.StateEffectExists(SpellDataName))
                m_Controller.StateHandler.RemoveStateEffect(SpellDataName);
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
                    return m_Controller;

                case ESpellTarget.CurrentTarget:
                    if (targetId.HasValue)
                        return GameManager.Instance.GetPlayer(targetId.Value);
                    return GameManager.Instance.GetFirstEnemy(m_Controller.Team);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(m_Controller.Team);

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(m_Controller.Team, m_Controller.PlayerId);

                default:
                    ErrorHandler.Warning("Unhandled case : " + Target);
                    return m_Controller;
            }
        }

        #endregion


        #region Listeners

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, ulong targetId, ulong casterId)
        {
            if (m_Controller == null)
            {
                ErrorHandler.Error("Provided Controller is null for state effect : " + stateEffectName + " - at event " + stateEffectEvent);
                return;
            }

            if (! HasStateEffect(stateEffectName))
                return;

            if (stateEffectEvent != StateEffectEvent)
                return;

            if (casterId != m_Controller.PlayerId)
                return;

            ActivateEffect(CalculateTarget(targetId));
        }

        #endregion
    }
}