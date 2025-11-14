using Assets.Scripts.Game.Character.Netcode;
using Data.DataStructures.SpellSubStructures;
using Data.GameManagement;
using Enums;
using Game.Character.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using UnityEngine;
using Utilities;

namespace Game.Character
{
    public class Movement : NetworkBehaviour
    {
        #region Members

        // ===================================================================================================
        // Events
        public Action<int> MovementInputChangedEvent;
        public Action<int> MoveXChangedEvent;

        // ===================================================================================================
        // Constants
        const float SERVER_TICK_RATE = 60f;
        const int BUFFER_SIZE = 1024;

        // ===================================================================================================
        // GameObjects & Components
        Controller m_Controller;
        ClientNetworkTransform m_ClientNetworkTransform;

        // Network Variables
        NetworkVariable<float>  m_InitialSpeed      = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        NetworkVariable<float>  m_FinalSpeedFactor  = new(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        NetworkVariable<float>  m_Force             = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        bool m_IsActive = false;

        // Netcode general
        NetworkTimer m_NetworkTimer;
        CountdownTimer m_ReconciliationCooldown;

        // Netcode client specific
        CircularBuffer<SStatePayload> m_ClientStateBuffer;
        CircularBuffer<SInputPayload> m_ClientInputBuffer;
        SStatePayload m_LastServerState;
        SStatePayload m_LastProcessedState;

        // Netcode server specific
        CircularBuffer<SStatePayload> m_ServerStateBuffer;
        Queue<SInputPayload> m_ServerInputQueue;

        [Header("Netcode")]
        float m_ReconciliationThreshold = 0.2f;
        float m_ReconciliationCooldownTime = 1f;
        float m_ExtrapolationLimit = 0.5f;      // 500 ms
        float m_ExtrapolationMultiplier = 1.2f;

        SStatePayload m_ExtrapolationState;
        CountdownTimer m_ExtrapolationCooldown;

        // Shared Data
        int m_MoveX = 0;

        // Server Data
        List<SForce>    m_Forces = new List<SForce>();
        bool            m_MovementBlocked = false;
        bool            m_MovementCancelled = false;

        // Client Data
        int m_MovementInput     = 0;
        bool m_CanMoveClient    = true;
        bool m_IsGroundedClient = false;

        public float Speed      => Settings.CharacterSpeedFactor * CalculateRawSpeed();
        public bool IsMoving    => m_MoveX != 0;
        public int MoveX        => m_MoveX;

        protected float GetVelocity(int direction)
        {
            if (float.IsNaN(direction * Speed + m_Force.Value))
            {
                ErrorHandler.Error("Velocity of " + gameObject.name + " is Nan");
                return 0;
            }
            return direction * Speed + (m_IsGroundedClient ? 0 : m_Force.Value);
        }

        public float CalculateRawSpeed()
        {
            float speed = m_InitialSpeed.Value + m_Controller.StateHandler.SpeedBonus.Value;
            if (speed < 1)
            {
                speed = 1 / (2 - speed);
            }

            return Mathf.Clamp(m_InitialSpeed.Value * speed * m_FinalSpeedFactor.Value, 0, 3);
        }

        #endregion


        #region Init & End

        void Awake()
        {
            m_Controller = GetComponent<Controller>();
            m_ClientNetworkTransform = GetComponent<ClientNetworkTransform>();

            m_NetworkTimer          = new NetworkTimer(SERVER_TICK_RATE);
            m_ClientStateBuffer     = new CircularBuffer<SStatePayload>(BUFFER_SIZE);
            m_ClientInputBuffer     = new CircularBuffer<SInputPayload>(BUFFER_SIZE);
            m_ServerStateBuffer     = new CircularBuffer<SStatePayload>(BUFFER_SIZE);
            m_ServerInputQueue      = new Queue<SInputPayload>(BUFFER_SIZE);

            m_ReconciliationCooldown = new CountdownTimer(m_ReconciliationCooldownTime);
            m_ExtrapolationCooldown = new CountdownTimer(m_ExtrapolationLimit);

            m_ReconciliationCooldown.OnTimerStart += () =>
            {
                m_ExtrapolationCooldown.Stop();
            };

            m_ExtrapolationCooldown.OnTimerStart += () =>
            {
                m_ReconciliationCooldown.Stop();

                ChangeAuthority(AuthorityMode.Server);
                m_ClientNetworkTransform.SyncPositionX = false;
                m_ClientNetworkTransform.SyncPositionY = false;
            };

            m_ExtrapolationCooldown.OnTimerStop += () =>
            {
                m_ExtrapolationState = default;

                ChangeAuthority(AuthorityMode.Client);
                m_ClientNetworkTransform.SyncPositionX = true;
                m_ClientNetworkTransform.SyncPositionY = true;
            };
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                return;
        }

        public void Initialize(float characterSpeed)
        {
            m_InitialSpeed.Value = characterSpeed;
        }

        public void Activate(bool activate)
        {
            if (! activate)
            {
                SetMovementInput(0);
                SetMovement(0);
                ResetRotation();
                UnRegisterListeners();
            }
            else
            {
                RegisterListeners();
            }

            m_IsActive = activate;
        }

        #endregion


        #region Updates

        void Update()
        {
            // must be in game and active to process
            if (!m_Controller.GameRunning || !m_IsActive)
                return;

            // has to be a player to update this (ISSUE WITH INVOCATIONS ??)
            if (!m_Controller.IsPlayer)
                return;

            if (IsOwner)
            {
                CheckInputs();
            }

            m_NetworkTimer.Update(Time.deltaTime);
            m_ReconciliationCooldown.Tick(Time.deltaTime);
            m_ExtrapolationCooldown.Tick(Time.deltaTime);

            // Update or Fixed Update ? Or Both ?
            Extrapolate();
        }

        void FixedUpdate()
        {
            // must be in game and active to process
            if (!m_Controller.GameRunning || !m_IsActive)
                return;

            if (IsServer)
                UpdateCanMove();

            // --------------------------------------------------
            // AI Movement
            if (! m_Controller.IsPlayer)
            {
                if (IsServer)
                    Move(m_MoveX);
                return;
            }

            // --------------------------------------------------
            // Player synchronized movement
            while (m_NetworkTimer.ShouldTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }

        void HandleServerTick()
        {
            if (!IsServer) return;

            SInputPayload inputPayload = default;
            SStatePayload statePayload;
            var bufferIndex = -1;

            while (m_ServerInputQueue.Count > 0)
            {
                inputPayload = m_ServerInputQueue.Dequeue();
                bufferIndex = inputPayload.Tick % BUFFER_SIZE;

                // If we dont check if its host then we will have double input from host.
                if (IsHost && IsOwner) 
                {
                    statePayload = new SStatePayload()
                    {
                        Tick = inputPayload.Tick,
                        Position = transform.position,
                        Velocity = GetVelocity(inputPayload.Direction)
                    };

                    m_ServerStateBuffer.Add(statePayload, bufferIndex);
                    SendToClientRPC(statePayload);
                    continue;
                }

                statePayload = ProcessMovement(inputPayload);
                m_ServerStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;

            SendToClientRPC(m_ServerStateBuffer.Get(bufferIndex));
            HandleExtrapolation(m_ServerStateBuffer.Get(bufferIndex), CalculateLatencyInMillis(inputPayload));
        }

        void HandleClientTick()
        {
            if (!IsClient) return;

            var currentTick = m_NetworkTimer.CurrentTick;
            var bufferIndex = currentTick % BUFFER_SIZE;

            SInputPayload inputPayload = new SInputPayload()
            {
                Tick        = currentTick,
                Timestamp   = DateTime.Now,
                Direction   = m_MovementInput,
            };

            // only owner can handle Inputs
            if (IsOwner)
            {
                m_ClientInputBuffer.Add(inputPayload, bufferIndex);
                SendToServerRPC(inputPayload);
            }

            SStatePayload statePayload = ProcessMovement(inputPayload);
            m_ClientStateBuffer.Add(statePayload, bufferIndex);

            HandleServerReconciliation();
        }

        bool ShouldReconcile()
        {
            bool isNewServerState = !m_LastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = m_LastProcessedState.Equals(default)
                || !m_LastProcessedState.Equals(m_LastServerState);

            return isNewServerState 
                && isLastStateUndefinedOrDifferent 
                && !m_ReconciliationCooldown.IsRunning 
                && !m_ExtrapolationCooldown.IsRunning;
        }

        void HandleServerReconciliation()
        {
            if (!ShouldReconcile()) return;

            float positionError;
            int bufferIndex;

            bufferIndex = m_LastServerState.Tick % BUFFER_SIZE;
            if (bufferIndex <= 0) return;   // not enough data to reconcile

            SStatePayload rewindState = IsHost ? m_ServerStateBuffer.Get(bufferIndex - 1) : m_LastServerState;
            SStatePayload clientState = IsHost ? m_ClientStateBuffer.Get(bufferIndex - 1) : m_ClientStateBuffer.Get(bufferIndex);
            positionError = Vector3.Distance(rewindState.Position, clientState.Position);

            if (positionError > m_ReconciliationThreshold)
                ReconcileState(rewindState);

            m_LastProcessedState = m_LastServerState;
        }

        void ReconcileState(SStatePayload rewindState)
        {
            m_ReconciliationCooldown.Start();

            transform.position = rewindState.Position;

            if (!rewindState.Equals(m_LastServerState))
                return;

            m_ClientStateBuffer.Add(rewindState, rewindState.Tick);

            // replay all inputs from the rewind state to the current data
            int tickToReplay = m_LastServerState.Tick;

            while (tickToReplay < m_NetworkTimer.CurrentTick)
            {
                int bufferIndex = tickToReplay % BUFFER_SIZE;
                SStatePayload statePayload = ProcessMovement(m_ClientInputBuffer.Get(bufferIndex));
                m_ClientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        [ServerRpc]
        void SendToServerRPC(SInputPayload inputPayload)
        {
            m_ServerInputQueue.Enqueue(inputPayload);
        }

        [ClientRpc]
        void SendToClientRPC(SStatePayload statePayload)
        {
            m_LastServerState = statePayload;
        }

        SStatePayload ProcessMovement(SInputPayload input)
        {
            // Update movement input
            SetMovementInput(input.Direction);

            // Move character
            Move(input.Direction);

            return new SStatePayload()
            {
                Tick        = input.Tick,
                Position    = transform.position,
                Velocity    = GetVelocity(input.Direction)
            };
        }

        #endregion


        #region Movement Input Methods

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

            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A) || GameUIManager.LeftMovementButtonPressed)
            {
                moveX = -1;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.E) || GameUIManager.RightMovementButtonPressed)
            {
                moveX = 1;
            }

            SetMovementInput(moveX);
        }

        void SetMovementInput(int direction)
        {
            if (m_MovementInput == direction)
                return;

            if (m_MovementCancelled)
            {
                m_MovementCancelled = false;
                direction = 0;
            }

            m_MovementInput = direction;
            MovementInputChangedEvent?.Invoke(direction);
        }

        public void SetMovement(int moveX)
        {
            if (m_MoveX == moveX)
                return;

            m_MoveX = moveX;
            MoveXChangedEvent?.Invoke(m_MoveX);

            if (IsServer)
                SetMovementClientRPC(moveX);
        }

        [ClientRpc]
        void SetMovementClientRPC(int moveX)
        {
            if (IsOwner)
                return;

            SetMovementInput(moveX);
            SetMovement(moveX);
        }

        #endregion


        #region Force

        public void AddForce(SForce force)
        {
            if (! IsServer)
                return;

            if (force == null || force == default || force.Speed == 0)
                return;

            ErrorHandler.Log("AddForce() : " + force.Speed, ELogTag.Forces);

            if (force.Duration > 0)
                StartCoroutine(StartForceTimer(force));

            m_Forces.Add(force);
            UpdateForce();
        }

        public void RemoveForce(SForce force)
        {
            if (!IsServer)
                return;

            if (force == null || force == default || force.Speed == 0)
                return;

            if (m_Forces.Contains(force))
                m_Forces.Remove(force);

            UpdateForce();
        }

        public void UpdateForce()
        {
            float force = 0f;
            foreach (var sforce in m_Forces)
            {
                force += sforce.Speed;
            }

            m_Force.Value = force;
        }

        IEnumerator StartForceTimer(SForce force)
        {
            yield return new WaitForSeconds(force.Duration);
            RemoveForce(force);
        }

        #endregion


        #region Movement Logic

        /// <summary>
        /// Server authoritative movement calculation
        /// </summary>
        void Move(int direction)
        {
            if (! m_CanMoveClient)
            {
                direction = 0;
            }

            // Update movement to expected direction
            SetMovement(direction);

            float teamFactor = m_Controller.Team == 0 ? 1f : -1f;
            transform.position += new Vector3(
                teamFactor * GetVelocity(m_MoveX) * Time.deltaTime,
                0f, 0f);
        }

        void UpdateCanMove()
        {
            if (!IsServer)
                return;

            // CHECK : changes in "CanMove"
            bool canMove = CanMove;
            if (m_CanMoveClient != canMove)
            {
                // send changes to client
                m_CanMoveClient = canMove;
                SetCanMoveClientRPC(canMove);
            }

            // CHECK : changes in "IsGrounded"
            bool isGrounded = m_Controller.StateHandler.IsGrounded;
            if (m_IsGroundedClient != isGrounded)
            {
                m_IsGroundedClient = isGrounded;
                SetIsGroundedClientRPC(isGrounded);
            }
                
        }

        #endregion


        #region Rotation

        void UpdateRotation(int moveX)
        {
            if (moveX == -1)
            {
                SetRotation(m_Controller.Team == 0 ? -180f : 0f);
                return;
            }

            ResetRotation();
        }

        void ResetRotation()
        {
            SetRotation(m_Controller.Team == 0 ? 0f : -180f);
        }

        void SetRotation(float y)
        {
            transform.localRotation = Quaternion.Euler(0f, y, 0f);
        }

        #endregion


        #region Extrapolation

        static float CalculateLatencyInMillis(SInputPayload inputPayload)
        {
            return (DateTime.Now - inputPayload.Timestamp).Milliseconds / 1000f;
        }

        bool ShouldExtrapolate(float latency) => latency < m_ExtrapolationLimit && latency > Time.fixedDeltaTime;

        void HandleExtrapolation(SStatePayload latestPayload, float latency)
        {
            if (ShouldExtrapolate(latency))
            {
                if (m_ExtrapolationState.Position != default)
                {
                    latestPayload = m_ExtrapolationState;
                }

                float teamFactor = m_Controller.Team == 0 ? 1f : -1f;
                m_ExtrapolationState.Position = new Vector3(
                    teamFactor * latestPayload.Velocity * (1 + latency * m_ExtrapolationMultiplier),
                0f, 0f);
                m_ExtrapolationState.Velocity = latestPayload.Velocity;
            }
            else
            {
                m_ExtrapolationCooldown.Stop();
            }
        }

        void Extrapolate()
        {
            if (IsServer && m_ExtrapolationCooldown.IsRunning)
            {
                transform.position += new Vector3(m_ExtrapolationState.Position.x, m_ExtrapolationState.Position.y, 0f);
            }
        }

        #endregion


        #region Client Sync

        [ClientRpc]
        void SetCanMoveClientRPC(bool value)
        {
            m_CanMoveClient = value;
        }

        [ClientRpc]
        void SetIsGroundedClientRPC(bool value)
        {
            m_IsGroundedClient = value;
        }

        [ClientRpc]
        void ResetRotationClientRPC()
        {
            ResetRotation();
        }

        #endregion


        #region Public Manipulators

        public void CancelMovement(bool cancel)
        {
            if (!IsServer)
                return;

            if (cancel)
                ResetRotationClientRPC();

            if (cancel && !IsMoving)
                return;

            m_MovementCancelled = cancel;

            if (cancel)
                SetMovement(0);
        }

        public void ForceBlockMovement(bool block)
        {
            if (!IsServer)
                return;

            CancelMovement(block);
            m_MovementBlocked = block;
        }

        public void SetFinalSpeedFactor(float finalSpeedFactor)
        {
            m_FinalSpeedFactor.Value = finalSpeedFactor;
        }

        #endregion


        #region Helpers

        public void ChangeAuthority(AuthorityMode authority)
        {
            m_ClientNetworkTransform.AuthorityMode = authority;
        }

        #endregion


        #region Listeners

        void RegisterListeners()
        {
            if (IsClient)
            {
                MoveXChangedEvent += OnMoveXChanged;
            }
        }

        void UnRegisterListeners()
        {
            if (IsClient)
            {
                MoveXChangedEvent -= OnMoveXChanged;
            }
        }

        void OnMoveXChanged(int moveX)
        {
            // update rotation (on client side) to match the moving direction
            UpdateRotation(moveX);
        }

        #endregion


        #region Dependent Attributes

        public bool CanMove
        {
            get
            {
                if (! m_Controller.StateHandler.CanMove)
                {
                    ErrorHandler.Log("CanMove - FALSE : has state preventing movement", ELogTag.Movement);
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
