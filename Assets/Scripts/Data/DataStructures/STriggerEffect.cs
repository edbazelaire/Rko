using Enums;
using Game;
using Game.Loaders;
using System;
using System.Collections;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Data.DataStructures
{
    [Serializable]
    public struct STriggerEffect : INetworkSerializable
    {
        public  string                  SpellDataName;
        public  int                     Level;
        public  ESpellActivationEvent   SpellActivationEvent;
        public  float                   ActivationTreshold;
        public  int                     NActivations;
        public  float                   Cooldown;

        int     m_NActivationsCtr;
        float   m_CooldownTimer;

        public bool IsActivable => (
            NActivations == -1                  // infinite activations
                || m_NActivationsCtr < (NActivations >= 1 ? NActivations : 1)) // OR below min activation 
            && m_CooldownTimer <= 0;            // cooldown must be done

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SpellDataName);
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref SpellActivationEvent);
            serializer.SerializeValue(ref ActivationTreshold);
            serializer.SerializeValue(ref NActivations);
            serializer.SerializeValue(ref Cooldown);
        }

        public void Activate(Controller controller)
        {
            if (! IsActivable)
                return;

            m_NActivationsCtr++;
            if (Cooldown > 0)
            {
                m_CooldownTimer = Cooldown;
                controller.StartCoroutine(UpdateCooldownTimer());
            }

            Debug.LogWarning("ACTIVATE : " + SpellDataName);

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

        IEnumerator UpdateCooldownTimer()
        {
            while (m_CooldownTimer > 0)
            {
                m_CooldownTimer -= Time.deltaTime;
                yield return null;
            }
        }
    }
}