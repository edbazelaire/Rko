using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Data.GameManagement;
using Enums;
using Game.Character.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class NewMovement : NetworkBehaviour
    {
        #region Members

        Controller m_Controller;

        NetworkVariable<float> m_Force              = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        NetworkVariable<int> m_MoveX                = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        bool m_IsActive = false;
        float m_InitialSpeed;

        // Netcode general
        NetworkTimer    m_NetworkTimer;
        const float     SERVER_TICK_RATE = 60f;
        const int       BUFFER_SIZE = 1024;

        // Netcode client specific
        CircularBuffer<SStatePayload>   m_ClientStateBuffer;
        CircularBuffer<SInputPayload>   m_ClientInputBuffer;
        SStatePayload                   m_LastServerState;
        SStatePayload                   m_LastProcessedState;

        // Netcode server specific
        CircularBuffer<SStatePayload>   m_ServerStateBuffer;
        Queue<SInputPayload>            m_ServerInputQueue;
        float                           m_ReconciliationThreshold = 0.2f;

        // Server Data
        List<SForce>    m_Forces            = new List<SForce>();
        int             m_MovementInput     = 0;
        bool            m_MovementBlocked   = false;
        bool            m_MovementCancelled = false;

        // Client Data
        bool m_CanMoveClient = true;

        public float Speed                  => Math.Max(0, Settings.CharacterSpeedFactor * (m_InitialSpeed + m_Controller.StateHandler.SpeedBonus.Value));
        public bool IsMoving                => m_MoveX.Value != 0;
        public NetworkVariable<int> MoveX   => m_MoveX;

        #endregion


        #region Init & End

        void Awake()
        {
            m_Controller = GetComponent<Controller>();

            m_NetworkTimer          = new NetworkTimer(SERVER_TICK_RATE);
            m_ClientStateBuffer     = new CircularBuffer<SStatePayload>(BUFFER_SIZE);
            m_ClientInputBuffer     = new CircularBuffer<SInputPayload>(BUFFER_SIZE);
            m_ServerStateBuffer     = new CircularBuffer<SStatePayload>(BUFFER_SIZE);
            m_ServerInputQueue      = new Queue<SInputPayload>(BUFFER_SIZE);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                return;
        }

        public void Initialize(float characterSpeed)
        {
            m_InitialSpeed = characterSpeed;
        }

        public void Activate(bool activate)
        {
            if (!activate)
            {
                SetMovement(0);
                ResetRotation();
                UnRegisterListeners();
            } else
            {
                RegisterListeners();
            }

            m_IsActive = activate;

            if (IsServer)
            {
                m_MoveX.Value = 0;
            }
        }

        void Update()
        {
            m_NetworkTimer.Update(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (!m_Controller.GameRunning || !m_IsActive)
                return;

            if (IsServer)
                UpdateCanMove();

            if (IsOwner)
            {
                CheckInputs();
            }

            while (m_NetworkTimer.ShouldTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }

        void HandleClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = m_NetworkTimer.CurrentTick;
            var bufferIndex = currentTick % BUFFER_SIZE;

            SInputPayload inputPayload = new SInputPayload()
            {
                Tick = currentTick,
                Direction = m_MovementInput
            };

            m_ClientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRPC(inputPayload);

            SStatePayload statePayload = ProcessMovement(inputPayload);
            m_ClientStateBuffer.Add(statePayload, bufferIndex);

            HandleServerReconciliation();
        }

        void HandleServerTick()
        {
            if (! IsServer) return;

            SInputPayload inputPayload = default;
            SStatePayload statePayload;
            var bufferIndex = -1;

            while (m_ServerInputQueue.Count > 0)
            {
                inputPayload = m_ServerInputQueue.Dequeue();
                bufferIndex = inputPayload.Tick % BUFFER_SIZE;

                if (IsHost) //If we dont check if its host then we will have double input from host. I mean host will move twice faster then he should
                {
                    statePayload = new SStatePayload()
                    {
                        Tick = inputPayload.Tick,
                        Position = transform.position,
                    };

                    m_ServerStateBuffer.Add(statePayload, bufferIndex);
                    SendToClientRPC(statePayload);
                    continue;
                }

                statePayload = SimulateMovement(inputPayload);
                m_ServerStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;

            SendToClientRPC(m_ServerStateBuffer.Get(bufferIndex));
        }

        bool ShouldReconcile()
        {
            bool isNewServerState = ! m_LastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = m_LastProcessedState.Equals(default)
                || ! m_LastProcessedState.Equals(m_LastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent;
        }

        void HandleServerReconciliation()
        {
            if (! ShouldReconcile()) return;

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
            Debug.LogWarning("Reconcilating State   ---------------------------------");

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

        SStatePayload SimulateMovement(SInputPayload inputPayload)
        {
            Physics.simulationMode = SimulationMode.Script;

            Move(inputPayload.Direction);
            Physics.Simulate(Time.deltaTime);

            Physics.simulationMode = SimulationMode.FixedUpdate;

            return new SStatePayload()
            {
                Tick = inputPayload.Tick,
                Position = transform.position
            };
        }

        [ServerRpc]
        void SendToServerRPC(SInputPayload inputPayload)
        {
            if (inputPayload.Direction != 0)
                Debug.Log($"Received INPUT PAYLOAD from ({m_Controller.PlayerId}) - direction : {inputPayload.Direction}");

            m_ServerInputQueue.Enqueue(inputPayload);
        }

        [ClientRpc]
        void SendToClientRPC(SStatePayload statePayload)
        {
            if (!IsOwner) return;

            m_LastServerState = statePayload;
        }

        SStatePayload ProcessMovement(SInputPayload input)
        {
            Move(input.Direction);

            return new SStatePayload()
            {
                Tick = input.Tick,
                Position = transform.position
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
                //SetMovementServerRPC(moveX);
            }
        }

        void SetMovementInput(int moveX)
        {
            if (m_MovementInput == moveX)
                return;

            m_MovementInput = moveX;
        }

        [ServerRpc]
        public void SetMovementServerRPC(int moveX)
        {
            if (m_MovementCancelled)
                m_MovementCancelled = false;
            else
                SetMovement(moveX);
        }

        public void SetMovement(int moveX)
        {
            if (m_MovementCancelled)
                m_MovementCancelled = false;

            m_MovementInput = moveX;
        }

        #endregion


        #region Force

        public void AddForce(SForce force)
        {
            if (force == null || force == default)
                return;

            if (force.Duration > 0)
                StartCoroutine(StartForceTimer(force));

            m_Forces.Add(force);
            UpdateForce();
        }

        public void RemoveForce(SForce force)
        {
            if (!m_Forces.Contains(force))
                return;

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
            m_Forces.Remove(force);
        }

        #endregion


        #region Movement Logic

        /// <summary>
        /// Server authoritative movement calculation
        /// </summary>
        void Move(int direction)
        {
            float teamFactor = m_Controller.Team == 0 ? 1f : -1f;

            if (! m_CanMoveClient)
            {
                direction = 0;
            }

            if (IsServer && m_MoveX.Value != direction)
                m_MoveX.Value = direction;

            // ====================================================================
            // TODO : remove
            var previousXPos = transform.position.x;
            // ====================================================================

            transform.position += new Vector3(
                teamFactor * (direction * Speed + m_Force.Value) * Time.deltaTime,
                0f, 0f);

            // ====================================================================
            // TODO : remove
            if (m_Controller.Team == 1) 
            {
                if (previousXPos != transform.position.x)
                    Debug.Log((IsServer ? "[SERVER]" : "[CLIENT]") + " new XPos : from ("+ previousXPos + ") to ("+ transform.position.x + ")");
            }
            // TODO : remove
            // ====================================================================
        }

        void UpdateCanMove()
        {
            if (!IsServer)
                return;

            bool canMove = CanMove;
            if (m_CanMoveClient != canMove)
            {
                m_CanMoveClient = canMove;
                SetCanMoveClientRPC(canMove);
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


        #region Client Sync

        [ClientRpc]
        void SetCanMoveClientRPC(bool value)
        {
            // TODO : remove    =====================================================
            Debug.LogWarning("m_CanMoveClient : " + m_CanMoveClient);
            // TODO : remove    =====================================================
            m_CanMoveClient = value;
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


        #region Helpers

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

        #endregion


        #region Listeners

        void RegisterListeners()
        {
            // CLIENT SIDE --------------------------------------------
            if (IsClient)
            {
                m_MoveX.OnValueChanged += OnMoveXChanged;
            }
        }

        void UnRegisterListeners()
        {
            if (IsClient)
            {
                m_MoveX.OnValueChanged -= OnMoveXChanged;
            }
        }

        void OnMoveXChanged(int oldValue, int moveX)
        {
            UpdateRotation(moveX);
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
