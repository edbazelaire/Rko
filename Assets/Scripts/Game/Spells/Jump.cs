using Data;
using Enums;
using Game.Loaders;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Game.Spells
{
    public class Jump : Projectile
    {
        #region Members

        JumpData m_SpellData => m_BaseSpellData as JumpData;
        float   m_OffsetY;

        #endregion

        // Use this for initialization
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level)
        {
            base.Initialize(clientId, target, spellName, level);

            m_OffsetY = 0.1f + ((CapsuleCollider2D)m_Controller.Collider).size.y / 2;

            transform.position  = m_Controller.transform.position;
            m_OriginalPosition  = transform.position;
            m_MaxDistance       = Math.Abs(m_Target.x - m_OriginalPosition.x);

            if (EJumpType.Teleport == m_SpellData.JumpType)
                m_Controller.GFXHandler.HideCharacter(true);

            // make player untargatable, unmovable and unrotatable
            if (IsServer)
            {
                m_Controller.StateHandler.SetStateJump(true);
                m_Controller.SpellHandler.ForceBlockCast(true);
                m_Controller.Movement.ForceBlockMovement(true);
            }
               
        }

        // Update is called once per frame
        protected override void Update()
        {
            base.Update();

            // only server can check for distance and update the Controller position
            if (!IsServer)
                return;

            if (m_SpellData.JumpType != EJumpType.Teleport)
                UpdatePlayerPosition();

            // check if the spell has reached its max distance
            if (Math.Abs(transform.position.x - m_OriginalPosition.x) >= m_MaxDistance)
                End();
        }

        protected override void OnTriggerEnter2D(Collider2D collistion)
        {
            return;
        }


        #region Protected Members

        protected override void End()
        {
            base.End();

            // force pos to original Y
            var pos = m_Controller.transform.position;
            pos.y = m_OriginalPosition.y;
            m_Controller.transform.position = pos;

            // re activate collider
            m_Controller.Collider.enabled = true;

            if (m_SpellData.JumpType == EJumpType.Teleport)
            {
                m_Controller.GFXHandler.HideCharacterClientRPC(false);
                m_Controller.transform.position = transform.position;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            // reset player position
            m_Controller.transform.position = m_OriginalPosition;

            // reset jump state
            m_Controller.StateHandler.SetStateJump(false);
            m_Controller.SpellHandler.ForceBlockCast(false);
            m_Controller.Movement.ForceBlockMovement(false);
        }

        #endregion


        #region Private Members

        /// <summary>
        /// Update the player position to the spell position
        /// </summary>
        void UpdatePlayerPosition()
        {
            Vector3 pos = transform.position;
            pos.y += m_OffsetY;
            m_Controller.transform.position = pos;
        }

        #endregion
    }
}