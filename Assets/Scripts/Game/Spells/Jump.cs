using Data;
using Enums;
using System.Collections;
using UnityEngine;

namespace Game.Spells
{
    public class Jump : Projectile
    {
        #region Members

        const float c_JumpReturnFailsafeDelay = 1f;

        JumpData    m_SpellData => m_BaseSpellData as JumpData;
        float       m_CharacterOffsetY;
        Coroutine   m_ReturnFailsafeCoroutine;

        public override float Speed => m_SpellData.Speed * Mathf.Max(0.2f, m_Caster.Movement.CalculateRawSpeed());

        #endregion


        #region Init & End

        public override void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            base.Initialize(clientId, target, spellData);
            
            m_CharacterOffsetY = 0.1f + ((CapsuleCollider2D)m_Caster.Collider).size.y / 2;
            transform.localScale = m_Caster.transform.localScale * m_SpellData.BaseSize;

            if (EJumpType.Teleport == m_SpellData.JumpType)
                m_Caster.GFXHandler.HideCharacter(true);

            // make player untargatable, unmovable and unrotatable
            if (IsServer)
            {
                // get collider of the Controller
                var collider = CopyCollider(m_Caster.GFXHandler.Collider);
                collider.isTrigger = true;

                m_Caster.StateHandler.SetStateJump(true);
                m_Caster.SpellHandler.ForceBlockCast(true);
                m_Caster.Movement.ForceBlockMovement(true);

                // Safety net: if jump lifecycle gets interrupted, force a return to origin.
                m_ReturnFailsafeCoroutine = StartCoroutine(StartReturnFailsafe());
            }

            // play corresponding animation
            if (m_SpellData.JumpAnimation == EAnimation.None)
                m_Caster.AnimationHandler.CancelCastAnimation();
            else
                m_Caster.AnimationHandler.PlayAnimation(m_SpellData.JumpAnimation);
        }

        protected override void End()
        {
            if (!IsServer)
                return;

            // re activate collider
            m_Caster.Collider.enabled = true;

            // force pos to original Y
            var pos = m_Caster.transform.position;
            pos.y = 0f;
            m_Caster.transform.position = pos;

            if (m_SpellData.JumpType == EJumpType.Teleport)
            {
                m_Caster.GFXHandler.HideCharacterClientRPC(false);
                m_Caster.transform.position = transform.position;
            }

            base.End();
        }

        public override void OnDespawned()
        {
            if (m_ReturnFailsafeCoroutine != null)
            {
                StopCoroutine(m_ReturnFailsafeCoroutine);
                m_ReturnFailsafeCoroutine = null;
            }

            base.OnDespawned();

            // cancel animation
            m_Caster.AnimationHandler.CancelCastAnimation();

            // reset player position
            m_OriginalPosition.y = 0.5f;
            m_Caster.transform.position = m_OriginalPosition;

            if (!IsServer)
                return;

            // reset jump state
            m_Caster.StateHandler.SetStateJump(false);
            m_Caster.SpellHandler.ForceBlockCast(false);
            m_Caster.Movement.ForceBlockMovement(false);
        }

        IEnumerator StartReturnFailsafe()
        {
            yield return new WaitForSeconds(c_JumpReturnFailsafeDelay);

            if (!IsServer || m_Caster == null)
                yield break;

            if (!m_Caster.StateHandler.HasState(EStateEffect.Jump))
                yield break;

            // Force restore of player state and position if jump never properly ended.
            m_OriginalPosition.y = 0.5f;
            m_Caster.transform.position = m_OriginalPosition;
            m_Caster.Collider.enabled = true;
            m_Caster.GFXHandler.HideCharacterClientRPC(false);
            m_Caster.StateHandler.SetStateJump(false);
            m_Caster.SpellHandler.ForceBlockCast(false);
            m_Caster.Movement.ForceBlockMovement(false);

            if (!m_IsOver)
                Terminate(instant: true);
        }

        #endregion


        #region Update

        protected override void Update()
        {
            if (m_IsOver) 
                return;

            base.Update();

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
            //pos.y += m_CharacterOffsetY;
            m_Caster.transform.position = pos;
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
            var arenaTransform = ArenaManager.GetTargettableAreaTransform(m_Caster.Team, false);
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