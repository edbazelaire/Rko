using Data;
using Enums;
using System.Collections;
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
        Done,
    }


    public class Mine : Spell
    {
        #region Members

        NetworkVariable<EMineState> m_State;

        MineData m_SpellData => m_BaseSpellData as MineData;

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

            Debug.Log("ApplyPostProcessing() : " + m_SpellData.Name);

            m_DurationTimer = m_SpellData.Duration;
            m_ActivationCounter = 0;

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

        protected void OnTriggerEnter2D(Collider2D collider)
        {
            if (!IsServer)
                return;

            // must be armed to start
            if (m_State.Value != EMineState.Armed)
                return;

            if (!TryGetController(collider, out Controller controller))
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

            if (!TryGetController(collider, out Controller controller))
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

            Debug.Log("SetState() " + m_SpellData.Name + " : " + state);

            switch (state)
            {
                case EMineState.Inactive:
                    m_Coroutine = StartCoroutine(WaitForNextState(m_SpellData.InactiveTimer));
                    return;
                    
                case EMineState.Armed:
                    // waiting for activation
                    return;
                    
                case EMineState.Trigerred:
                    StartCoroutine(WaitForNextState(m_SpellData.TrigerredTimer));
                    return;
                    
                case EMineState.Activated:
                    m_Coroutine = StartCoroutine(WaitForNextState(m_SpellData.ActivateTimer));
                    return;
                    
                case EMineState.Done:
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
    }
}