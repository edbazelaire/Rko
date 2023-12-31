﻿using Data;
using Enums;
using Game.Managers;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Game.Spells
{
    public class Spell : NetworkBehaviour
    {
        #region Members

        //protected NetworkVariable<int> m_SpellTypeNet = new NetworkVariable<int>((int)ESpells.Count, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //protected NetworkVariable<int> m_ClientIdNet = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //protected NetworkVariable<Vector3> m_TargetNet = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        //protected NetworkVariable<float> m_Speed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //protected NetworkVariable<int> m_Damage = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //protected NetworkVariable<float> m_Distance = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //protected NetworkVariable<Vector3> m_Direction = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // ownership
        protected Controller        m_Controller;
        protected ulong             m_ClientId;

        // spell data
        protected ESpells           m_SpellType;
        protected float             m_Speed;
        protected int               m_Damage;
        protected float             m_Distance;
        protected Vector3           m_Direction;

        // targetting
        protected Vector3           m_Target;
        protected Vector3           m_OriginalPosition;

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            SpellData spellData = SpellLoader.GetSpellData(m_SpellType);

            m_ClientId = OwnerClientId;
            m_Speed = spellData.Speed;
            m_Damage = spellData.Damage;
            m_Distance = spellData.Distance;
            m_OriginalPosition = transform.position;

            m_Controller = GameManager.Instance.GetPlayer(m_ClientId);

            Debug.Log("OnNetworkSpawn position : " + transform.position);
        }

        public virtual void Initialize(Vector3 target, ESpells spellType)
        {
            m_SpellType = spellType;
            SetTarget(target);

            Debug.Log("Init position : " + transform.position);
        }

        protected virtual void End()
        {
            Destroy(gameObject);
        }

        #endregion


        #region Client RPCs

        [ClientRpc]
        public void InitializeClientRPC(Vector3 target, ESpells spellType)
        {
            Initialize(target, spellType);
        }

        #endregion


        #region Inherited Manipulators  

        protected void Update()
        {
            UpdateMovement();
        }

        protected void OnTriggerEnter2D(Collider2D collision)
        {
            // only server can check for collision
            if (! IsServer)
                return;

            if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                End();
            }
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Controller controller = collision.gameObject.GetComponent<Controller>();
                if (controller.Team == m_Controller.Team)
                    return;

                controller.Life.Hit(m_Damage);
                End();
            }
        }

        #endregion


        #region Server RPC



        #endregion


        #region Protected Manipulators

        /// <summary>
        /// 
        /// </summary>
        protected virtual void UpdateMovement()
        {
            transform.position += m_Direction * m_Speed * Time.deltaTime;

            // only server can check for distance
            if (!IsServer)
                return;

            // check if the spell has reached its max distance
            if (m_Distance > 0 && Math.Abs(transform.position.x - m_OriginalPosition.x) > m_Distance)
                End();
        }

        protected virtual void SetTarget(Vector3 target)
        {
            m_Target = target;
            m_Direction = (m_Target - transform.position).normalized;
            LookAt(m_Target);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        protected virtual void LookAt(Vector3 target)
        {
            Vector3 diff = target - transform.position;
            diff.Normalize();
            float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rot_z);
        }

        #endregion


        #region Network Variable Access

        //protected ESpells m_SpellType
        //{
        //    get => (ESpells)m_SpellTypeNet.Value;
        //    set => m_SpellTypeNet.Value = (int)value;
        //}

        //protected ulong m_ClientId
        //{
        //    get => (ulong)m_ClientIdNet.Value;
        //    set => m_ClientIdNet.Value = (int)value;
        //}

        //protected Vector3 m_Target
        //{
        //    get => m_TargetNet.Value;
        //    set => m_TargetNet.Value = value;
        //}

        #endregion


        #region Debug

        public void DebugMessage()
        {
            Debug.Log("Spell " + m_SpellType);
            Debug.Log("     + ClientId " + m_ClientId);
            Debug.Log("     + Target " + m_Target);
            Debug.Log("     + Direction " + m_Direction);
            Debug.Log("     + Speed " + m_Speed);    
        }

        #endregion
    }
}