using Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Spells
{
    public enum EMineState
    {
        None,

        Inactive,
        Armed,
        Trigerred,
        Activated,
        InUse,

        End,
    }


    public class Mine : Spell
    {
        #region Members

        NetworkVariable<EMineState> m_State = new NetworkVariable<EMineState>(EMineState.None);

        public NetworkVariable<EMineState> State => m_State;
        MineData m_SpellData => m_BaseSpellData as MineData;
        float m_Radius => m_SpellData.Size / 2;

        Coroutine       m_Coroutine;
        float           m_DurationTimer;
        int             m_ActivationCounter;

        #endregion

        
        #region Init & End

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            if (!IsServer)
                return;

            m_DurationTimer = m_SpellData.Duration;
            m_ActivationCounter = 0;

            m_State.OnValueChanged += SpawnGFXPrefabs;

            SetState(EMineState.Inactive);
        }

        #endregion


        #region Triggers

        protected override void Update()
        {
            base.Update();

            if (!IsServer)
                return;

            // no duration : infinite until expires
            if (m_SpellData.Duration <= 0)
                return;

            m_DurationTimer -= Time.deltaTime;
            if (m_DurationTimer < 0)
                End();
        }

        protected void CreateCollisionCircle()
        {
            // Check for collisions within a circle with variableRadius radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, m_Radius);

            foreach (Collider2D collider in colliders)
            {
                if (TryGetController(collider, out Controller _))
                {
                    SetState(EMineState.Trigerred);
                    return;
                }
            }
        }

        protected void OnTriggerEnter2D(Collider2D collider)
        {
            if (!IsServer)
                return;

            // must be armed to start
            if (m_State.Value != EMineState.Armed)
                return;

            if (!TryGetController(collider, out Controller _))
                return;

            SetState(EMineState.Trigerred);
        }

        protected void OnTriggeExit2D(Collider2D collider)
        {
            if (!IsServer)
                return;

            // must be armed to start
            if (m_State.Value != EMineState.Trigerred)
                return;

            if (!TryGetController(collider, out Controller _))
                return;

            SetState(EMineState.Armed);
        }

        #endregion


        #region State

        void SetState(EMineState state)
        {
            m_State.Value = state;

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);

            switch (state)
            {
                case EMineState.Inactive:
                    m_Coroutine = StartCoroutine(WaitForNextState(m_SpellData.InactiveTimer));
                    return;
                    
                case EMineState.Armed:
                    CreateCollisionCircle();
                    return;
                    
                case EMineState.Trigerred:
                    StartCoroutine(WaitForNextState(m_SpellData.TrigerredTimer));
                    return;
                    
                case EMineState.Activated:
                    m_Coroutine = StartCoroutine(WaitForNextState(m_SpellData.ActivateTimer));
                    return;
                    
                case EMineState.InUse:
                    if (m_SpellData.ActivationData == null)
                        ErrorHandler.Error("MineData set with no ActivationData");
                    else
                        m_SpellData.ActivationData.Cast(OwnerClientId, transform.position, transform.position, recalculateTarget: false);

                    m_ActivationCounter++;
                    if (m_SpellData.NumActivations < 0 || m_ActivationCounter < m_SpellData.NumActivations)
                        SetState(EMineState.Inactive);
                    else
                        End();

                    return;
            }
        }

        void NextSate()
        {
            SetState(m_State.Value + 1);
        }

        IEnumerator WaitForNextState(float timer)
        {
            yield return new WaitForSeconds(timer);

            NextSate();
        }

        #endregion


        #region Target & Position

        protected override void SetTarget(Vector3 target)
        {
            transform.position = target;
            base.SetTarget(target);
        }

        #endregion


        #region Spell Event

        /// <summary>
        /// Spell event : instantiate/destroy the graphics matching the event of the spell
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="targetController"></param>
        protected virtual void SpawnGFXPrefabs(EMineState previousState, EMineState mineState)
        {
            foreach (var spawnPrefab in m_SpellData.MineSpawnGFX)
            {
                if (spawnPrefab.GFXLifetime.StartSpellPart != mineState)
                    continue;

                spawnPrefab.Spawn(m_Controller, m_SpellData, this, null, null, transform.position, m_Target);
            }
        }

        #endregion
    }
}