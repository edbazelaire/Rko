using Data;
using Game.NetworkStructures;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
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

        // ===================================================================================
        // Network Variables
        NetworkVariable<EMineState> m_State = new NetworkVariable<EMineState>(EMineState.None);
        public IReplicatedVar<EMineState> State;

        // ===================================================================================
        // Dependent Members
        MineData m_SpellData    => m_BaseSpellData as MineData;
        float m_Radius          => m_SpellData.Size / 2;

        // ===================================================================================
        // Local Members
        Coroutine       m_Coroutine;
        int             m_ActivationCounter;

        #endregion


        #region Init & End

        public override void OnSpawned()
        {
            base.OnSpawned();
            State = ReplicatedVar.Create(m_State);
        }

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            if (!IsServer)
                return;

            m_DurationTimer = m_SpellData.Duration;
            m_ActivationCounter = 0;

            State.OnValueChanged += SpawnGFXPrefabs;

            SetState(EMineState.Inactive);
        }

        #endregion


        #region Triggers

        protected override void Update()
        {
            base.Update();

            if (! GameManager.Exists)
            {
                Destroy(gameObject);
                return;
            }

            if (!IsServer)
                return;

            // no duration : infinite until expires
            if (m_SpellData.Duration <= 0)
                return;

            if (m_DurationTimer < 0)
                SetState(EMineState.End);
        }

        protected void CreateCollisionCircle()
        {
            // check for collisions within a circle with variableRadius radius
            var filter = Physics2DQueries.BuildFilter(TargetHelper.PLAYER_LAYER_MASK);
            int count = Physics2DQueries.OverlapCircle(transform.position, m_Radius, filter, out Collider2D[]  hits);
            for (int i = 0; i < count; i++)
            {
                if (TryGetController(hits[i], out Controller _))
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
            if (State.Value != EMineState.Armed)
                return;

            // Check if collider belongs to expected layers
            if ((TargetHelper.PLAYER_LAYER_MASK & (1 << collider.gameObject.layer)) == 0)
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
            if (State.Value != EMineState.Trigerred)
                return;

            // Check if collider belongs to expected layers
            if ((TargetHelper.PLAYER_LAYER_MASK & (1 << collider.gameObject.layer)) == 0)
                return;

            if (!TryGetController(collider, out Controller _))
                return;

            SetState(EMineState.Armed);
        }

        #endregion


        #region State

        void SetState(EMineState state)
        {
            State.Value = state;

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
                        m_SpellData.ActivationData.Cast(m_Caster.PlayerId, transform.position, transform.position, recalculateTarget: false);

                    m_ActivationCounter++;
                    if (m_SpellData.NumActivations < 0 || m_ActivationCounter < m_SpellData.NumActivations)
                        SetState(EMineState.Inactive);
                    else
                        SetState(EMineState.End);

                    return;

                case EMineState.End:
                    End();
                    break;
            }
        }

        void NextSate()
        {
            SetState(State.Value + 1);
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

                spawnPrefab.Spawn(m_Caster, m_SpellData, this, null, null, transform.position, m_Target);
            }
        }

        #endregion
    }
}