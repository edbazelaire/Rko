using Data;
using Enums;
using UnityEngine;

namespace Game.Spells
{
    public class Jump : Projectile
    {
        #region Members

        JumpData    m_SpellData => m_BaseSpellData as JumpData;
        float       m_CharacterOffsetY;

        #endregion


        #region Init & End

        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            base.Initialize(clientId, target, spellName, level, parent);
            
            m_CharacterOffsetY = 0.1f + ((CapsuleCollider2D)m_Controller.Collider).size.y / 2;
            transform.localScale = m_Controller.transform.localScale * m_SpellData.BaseSize;

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

        protected override void End()
        {
            if (!IsServer)
                return;

            // re activate collider
            m_Controller.Collider.enabled = true;

            // force pos to original Y
            var pos = m_Controller.transform.position;
            pos.y = 0f;
            m_Controller.transform.position = pos;

            if (m_SpellData.JumpType == EJumpType.Teleport)
            {
                m_Controller.GFXHandler.HideCharacterClientRPC(false);
                m_Controller.transform.position = transform.position;
            }

            base.End();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // cancel animation
            m_Controller.AnimationHandler.CancelCastAnimation();

            // reset player position
            m_OriginalPosition.y = 0.5f;
            m_Controller.transform.position = m_OriginalPosition;

            if (!IsServer)
                return;

            // reset jump state
            m_Controller.StateHandler.SetStateJump(false);
            m_Controller.SpellHandler.ForceBlockCast(false);
            m_Controller.Movement.ForceBlockMovement(false);
        }

        #endregion


        #region Update

        protected override void Update()
        {
            if (m_IsOver) 
                return;

            base.Update();

            // only server can check for distance and update the Controller position
            //if (!IsServer)
            //    return;

            if (m_SpellData.JumpType != EJumpType.Teleport)
                UpdatePlayerPosition();
        }

        /// <summary>
        /// Update the player position to the spell position
        /// </summary>
        void UpdatePlayerPosition()
        {
            if (m_IsOver)
                return;

            Vector3 pos = transform.position;
            pos.y += m_CharacterOffsetY;
            m_Controller.transform.position = pos;
        }

        #endregion


        #region On Hit

        /// <summary>
        /// Jumps ends on hitting enemy ground 
        /// </summary>
        /// <param name="collision"></param>
        protected override void OnHitGround(Collider2D collision)
        {
            // check if is caster's arena : do not collide with our arena
            var arenaTransform = ArenaManager.GetTargettableAreaTransform(m_Controller.Team, false);
            if (arenaTransform == collision.transform)
                return;

            base.OnHitGround(collision);
        }

        /// <summary>
        /// Jumps go throught structures
        /// </summary>
        /// <param name="collision"></param>
        protected override void OnHitStructure(Collider2D collision)
        {
            // do not hit structures with jumps
            return;
        }

        /// <summary>
        /// </summary>
        /// <param name="collision"></param>
        protected override void OnHitWall(Collider2D collision)
        {
            base.OnHitWall(collision);
        }

        #endregion
    }
}