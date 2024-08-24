using Enums;
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
        public  string                  SpellDataName;
        public  int                     Level;
        
        public  ESpellActivationEvent   SpellActivationEvent;
        public  float                   ActivationTreshold;
        public  ESpellActivationEvent   SpellDeactivationEvent;
        public  float                   DeactivationTreshold;

        public  EStateEffectEvent       StateEffectEvent;
        public  string                  StateEffectName;
        
        public  int                     NActivations;
        public  float                   Cooldown;

        Controller  m_Controller;
        int         m_NActivationsCtr;
        float       m_CooldownTimer;
        bool        m_IsActivated;

        public bool IsActivable()
        {
            return (NActivations == -1                  // infinite activations
                || m_NActivationsCtr < (NActivations >= 1 ? NActivations : 1)) // OR below min activation 
            && m_CooldownTimer <= 0;            // cooldown must be done
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SpellDataName);
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref SpellActivationEvent);
            serializer.SerializeValue(ref ActivationTreshold);

            serializer.SerializeValue(ref StateEffectEvent);
            serializer.SerializeValue(ref StateEffectName);

            serializer.SerializeValue(ref NActivations);
            serializer.SerializeValue(ref Cooldown);
        }

        #region Activation

        public void Activate(Controller controller)
        {
            if (m_IsActivated)
                return;

            if (m_Controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return;
            }

            m_IsActivated = true;
            m_Controller = controller;

            if (StateEffectEvent == EStateEffectEvent.None)
            {
                ActivateEffect(controller);
                return;
            }

            StateEffect.StateEffectEvent += OnStateEffectEvent;
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

            Debug.LogWarning("TRIGGER EFFECT : " + SpellDataName);

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
                controller.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(SpellDataName, Level), controller);

            else
                ErrorHandler.Error(SpellDataName + " not recognize either as Spell or StateEffect");
        }

        #endregion


        #region Deactivation

        public void Deactivate(Controller controller)
        {
            if (!m_IsActivated)
                return;

            m_IsActivated = false;
            StateEffect.StateEffectEvent -= OnStateEffectEvent;

            if (SpellLoader.StateEffectExists(SpellDataName))
                controller.StateHandler.RemoveStateEffect(SpellDataName);
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

        #endregion


        #region Listeners

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, ulong targetId, ulong casterId)
        {
            if (m_Controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return;
            }

            if (! HasStateEffect(stateEffectName))
                return;

            if (stateEffectEvent != StateEffectEvent)
                return;

            if (casterId != m_Controller.PlayerId)
                return;

            ActivateEffect(m_Controller);
        }

        #endregion
    }
}