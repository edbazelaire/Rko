using Data;
using Data.GameManagement;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    public class Jump : Projectile
    {
        #region Members

        JumpData    m_SpellData => m_BaseSpellData as JumpData;
        float       m_CharacterOffsetY;

        #endregion

        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level)
        {
            base.Initialize(clientId, target, spellName, level);

            m_CharacterOffsetY      = 0.1f + ((CapsuleCollider2D)m_Controller.Collider).size.y / 2;
            transform.localScale    = m_Controller.transform.localScale * m_SpellData.BaseSize;

            if (EJumpType.Teleport == m_SpellData.JumpType)
                m_Controller.GFXHandler.HideCharacter(true);

            // make player untargatable, unmovable and unrotatable
            if (IsServer)
            {
                // get collider of the Controller
                var collider = CopyCollider(m_Controller.GFXHandler.Collider);
                collider.isTrigger = true;

                m_Controller.StateHandler.SetStateJump(true);
                m_Controller.SpellHandler.ForceBlockCast(true);
                m_Controller.Movement.ForceBlockMovement(true);
            }

            // play corresponding animation
            if (m_SpellData.JumpAnimation == EAnimation.None)
                m_Controller.AnimationHandler.CancelCastAnimation();
            else
                m_Controller.AnimationHandler.PlayAnimation(m_SpellData.JumpAnimation);
        }

        protected override void Update()
        {
            base.Update();

            // only server can check for distance and update the Controller position
            if (!IsServer)
                return;

            if (m_SpellData.JumpType != EJumpType.Teleport)
                UpdatePlayerPosition();
        }

        protected override void OnHitGround(Collider2D collision)
        {
            // check if is caster's arena : do not collide with our arena
            var arenaTransform = ArenaManager.GetTargettableArea(m_Controller.Team, false);
            if (arenaTransform == collision.transform)
                return;

            base.OnHitGround(collision);
        }

        protected override void OnHitStructure(Collider2D collision)
        {
            // do not hit structures with jumps
            return;
        }


        #region Protected Members

        protected override void End()
        {
            base.End();

            if (! IsServer) 
                return;

            // force pos to original Y
            var pos = m_Controller.transform.position;
            pos.y = 0;
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

            // cancel animation
            m_Controller.AnimationHandler.CancelCastAnimation();

            if (!IsServer)
                return;

            // reset player position
            m_OriginalPosition.y = 0;
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
            pos.y += m_CharacterOffsetY;
            m_Controller.transform.position = pos;
        }

        #endregion
    }
}