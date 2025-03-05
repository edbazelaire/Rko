using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Data.GameManagement;
using Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class Movement : NetworkBehaviour
    {
        #region Members

        Controller m_Controller;

        NetworkVariable<int> m_MoveX = new(0);

        // [Server Data]
        List<SForce> m_Forces = new List<SForce>();

        // [Client Data]
        bool m_IsActive = false;
        bool m_CanMoveClient = true;
        int m_MovementInput = 0;
        bool m_MovementBlocked = false;
        bool m_MovementCancelled = false;
        float m_SpeedBonus = 0f;
        float m_InitialSpeed;

        public NetworkVariable<int> MoveX => m_MoveX;
        private NetworkVariable<Vector2> m_NetworkPosition = new NetworkVariable<Vector2>(Vector2.zero);
        public float Speed => Math.Max(0, Settings.CharacterSpeedFactor * (m_InitialSpeed + m_SpeedBonus));
        public bool IsMoving => m_MoveX.Value != 0;

        #endregion


        #region Init & End

        void Awake()
        {
            m_Controller = GetComponent<Controller>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                return;

            // CLIENT SIDE --------------------------------------------
            m_MoveX.OnValueChanged += OnMoveXChanged;

            if (IsOwner)
                ShakeServerRpc();
        }

        /// <summary>
        /// Initialize player movement speed
        /// </summary>
        /// <param name="characterSpeed"></param>
        public void Initialize(float characterSpeed)
        {
            m_Controller.StateHandler.SpeedBonus.OnValueChanged += OnSpeedBonusValueChanged;

            if (!IsServer)
                return;

            m_InitialSpeed = characterSpeed;
        }

        public void Activate(bool activate)
        {
            if (!activate)
            {
                SetMovement(0);
                ResetRotation();
            }

            m_IsActive = activate;

            if (IsServer)
            {
                m_MoveX.Value = 0;
            }
        }


        void Update()
        {
            if (!m_Controller.GameRunning || !m_IsActive)
                return;

            CheckInputs();

            if (!IsServer)
                return;

            UpdateCanMove();
            UpdateMovement();
        }

        private void FixedUpdate()
        {
            // transform.position = Vector2.Lerp(transform.position, m_NetworkPosition.Value, 0.2f);

            //if (!IsOwner)
            //{
            //    transform.position = Vector2.Lerp(transform.position, m_NetworkPosition.Value, 0.2f);

            //    //float distance = Vector2.Distance(transform.position, m_NetworkPosition.Value);

            //    //// If small desync, snap instantly
            //    //if (distance < 0.05f)
            //    //{
            //    //    transform.position = m_NetworkPosition.Value;
            //    //}
            //    //// If large desync, smooth it out
            //    //else
            //    //{
            //    //    transform.position = Vector2.Lerp(transform.position, m_NetworkPosition.Value, 0.4f);
            //    //}
            //}
        }

        #endregion


        #region ServerRPC Methods

        [ServerRpc]
        public void SetMovementServerRPC(int moveX)
        {
            if (m_MovementCancelled)
                m_MovementCancelled = false;
            else
                SetMovement(moveX);
        }

        [ServerRpc]
        public void ResetCancelMovementServerRPC()
        {
            m_MovementCancelled = false;
        }

        public void SetMovement(int moveX)
        {
            if (!IsServer)
                return;

            m_MovementInput = moveX;
        }

        #endregion


        #region Force

        public float Force
        {
            get
            {
                float force = 0f;
                foreach (var sforce in m_Forces)
                {
                    force += sforce.Speed;
                }
                return force;
            }
        }

        public void AddForce(SForce force)
        {
            if (force == null || force == default)
                return;

            if (force.Duration > 0)
                StartCoroutine(StartForceTimer(force));

            m_Forces.Add(force);
        }

        public void RemoveForce(SForce force)
        {
            if (!m_Forces.Contains(force))
                return;
            m_Forces.Remove(force);
        }

        IEnumerator StartForceTimer(SForce force)
        {
            yield return new WaitForSeconds(force.Duration);
            m_Forces.Remove(force);
        }

        #endregion


        #region Private Manipulators

        /// <summary>
        /// Apply speed on position
        /// </summary>
        void UpdateMovement()
        {
            // depending on team, the camera is rotated implying that movement is inverted
            float teamFactor = m_Controller.Team == 0 ? 1f : -1f;

            if (!CanMove || m_MovementInput == 0)
            {
                if (m_MoveX.Value != 0)
                    m_MoveX.Value = 0;
            }
            else
            {
                if (m_MoveX.Value != m_MovementInput)
                    m_MoveX.Value = m_MovementInput;

                // update rotation depending on movement (and team)
                if (teamFactor * m_MoveX.Value == 1)
                    SetRotation(0f);
                else if (teamFactor * m_MoveX.Value == -1)
                    SetRotation(180f);
            }

            // apply movement and Force
            transform.position += new Vector3(
                teamFactor * (m_MoveX.Value * Speed + Force) * Time.deltaTime,
                0f, 0f);

            // ============================================================================
            // TODO : REMOVE ?
            //m_NetworkPosition.Value = transform.position;
            // ============================================================================
        }

        /// <summary>
        /// Check if movement allowed (on server side) is the same as most recent value provided to the Client.
        /// If not -> send the correct value to the client
        /// </summary>
        void UpdateCanMove()
        {
            if (!IsServer)
                return;

            bool canMove = CanMove;
            if (m_CanMoveClient != canMove)
            {
                canMove = CanMove;
                SetCanMoveClientRPC(canMove);
            }
        }

        void SetRotation(float y)
        {
            transform.localRotation = Quaternion.Euler(0f, y, 0f);
        }

        /// <summary>
        /// Check if movement inputs have beed pressed
        /// </summary>
        void CheckInputs()
        {
            if (!IsOwner)
                return;

            if (!m_Controller.IsPlayer)
                return;

            int moveX = 0;

            if (Input.GetKey(KeyCode.Q) || GameUIManager.LeftMovementButtonPressed)
            {
                moveX = -1;
            }
            else if (Input.GetKey(KeyCode.D) || GameUIManager.RightMovementButtonPressed)
            {
                moveX = 1;
            }

            if (m_MovementInput != moveX)
            {
                SetMovementInput(moveX);
                SetMovementServerRPC(moveX);
            }
        }

        void SetMovementInput(int moveX)
        {
            if (m_MovementInput == moveX)
                return;

            m_MovementInput = moveX;
        }

        void UpdateRotation(int moveX)
        {
            if (moveX == -1)
            {
                SetRotation(m_Controller.Team == 0 ? -180f : 0f);
                return;
            }

            ResetRotation();
        }

        [ClientRpc]
        void ResetRotationClientRPC()
        {
            ResetRotation();
        }

        [ClientRpc]
        void SetCanMoveClientRPC(bool value)
        {
            m_CanMoveClient = value;
        }

        void ResetRotation()
        {
            SetRotation(m_Controller.Team == 0 ? 0f : -180f);
        }

        #endregion


        #region Public Manipulators

        /// <summary>
        /// Add a little bit of movement and a reset rotation on server side to be sure that the clients synchronized properly
        /// </summary>
        public void Shake()
        {
            if (!IsServer)
                return;

            // apply small movement and rotation
            transform.position += new Vector3(0.15f, 0, 0);
            transform.rotation = Quaternion.Euler(0.1f, 0.1f, 0.1f);

            // reset to default values next frame
            CoroutineManager.DelayMethod(ResetRotation);
        }

        [ServerRpc]
        public void ShakeServerRpc()
        {
            Shake();
        }

        public void CancelMovement(bool cancel)
        {
            if (!IsServer)
                return;

            if (cancel)
                ResetRotationClientRPC();

            if (cancel && !IsMoving)
                return;

            m_MovementCancelled = cancel;
            m_MoveX.Value = 0;
        }

        public void ForceBlockMovement(bool block)
        {
            if (!IsServer)
                return;

            CancelMovement(block);
            m_MovementBlocked = block;
        }

        #endregion


        #region Listeners

        void OnMoveXChanged(int oldValue, int moveX)
        {
            UpdateRotation(moveX);
        }

        private void OnSpeedBonusValueChanged(float oldValue, float newValue)
        {
            m_SpeedBonus = newValue;
        }

        #endregion


        #region Dependent Attributes


        public bool CanMove
        {
            get
            {
                if (m_Controller.StateHandler.IsStunned)
                {
                    ErrorHandler.Log("CanMove - FALSE : IsStunned", ELogTag.Movement);
                    return false;
                }

                if (m_Controller.StateHandler.IsAirborned)
                {
                    ErrorHandler.Log("CanMove - FALSE : IsAirborned", ELogTag.Movement);
                    return false;
                }

                if (m_Controller.StateHandler.HasState(EStateEffect.Jump))
                {
                    ErrorHandler.Log("CanMove - FALSE : is Jumping", ELogTag.Movement);
                    return false;
                }

                if (m_Controller.StateHandler.HasState(EStateEffect.Frozen))
                {
                    ErrorHandler.Log("CanMove - FALSE : is Frozen", ELogTag.Movement);
                    return false;
                }

                if (m_Controller.StateHandler.HasState(EStateEffect.SpecialAnimation))
                {
                    ErrorHandler.Log("CanMove - FALSE : has SpecialAnimation", ELogTag.Movement);
                    return false;
                }

                if (m_MovementBlocked)
                {
                    ErrorHandler.Log("CanMove - FALSE : Movement is blocked", ELogTag.Movement);
                    return false;
                }

                if (m_MovementCancelled)
                {
                    ErrorHandler.Log("CanMove - FALSE : Movement is cancelled", ELogTag.Movement);
                    return false;
                }

                if (m_Controller.SpellHandler.IsCastingUncancellable)
                {
                    ErrorHandler.Log("CanMove - FALSE : Current cast is not cancellable", ELogTag.Movement);
                    return false;
                }

                if (m_Controller.CounterHandler.IsBlockingMovement.Value)
                {
                    ErrorHandler.Log("CanMove - FALSE : Has counter blocking movement", ELogTag.Movement);
                    return false;
                }

                return true;
            }
        }

        #endregion
    }
}