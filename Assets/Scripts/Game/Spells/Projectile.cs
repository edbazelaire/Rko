using Data;
using Enums;
using MyBox;
using System;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class Projectile : Spell
    {
        #region Members
        ProjectileData m_SpellData => m_BaseSpellData as ProjectileData;

        protected float m_MaxHeight;
        protected float m_MaxDistance;

        protected Vector3 m_OriginalPosition;

        public Vector3 OriginalPosition => m_OriginalPosition;

        #endregion


        #region Init & End
        
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            base.Initialize(clientId, target, spellName, level, parent);

            m_OriginalPosition = transform.position;

            switch (m_SpellData.Trajectory)
            {
                case ESpellTrajectory.Curve:
                    m_MaxHeight = 3f;
                    m_MaxDistance = m_Target.x - m_OriginalPosition.x;
                    break;

                case ESpellTrajectory.Straight:
                    // set target to be align with orginal position
                    target.y = transform.position.y + m_SpellData.TargetOffset.Y;
                    SetTarget(target);
                    break;

                case ESpellTrajectory.Hight:
                case ESpellTrajectory.Diagonal:
                case ESpellTrajectory.DiagonalMiddle:
                    break;

                default:
                    ErrorHandler.Error("Projectile::Initialize() - Unknown trajectory type " + m_SpellData.Trajectory);
                    break;
            }
        }

        #endregion


        #region Inherited Manipulators

        /// <summary>
        /// [SERVER] check for collision with wall or player
        /// </summary>
        /// <param name="collision"></param>
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            // only server can check for collision
            if (!IsServer)
                return;

            // if spell hits a wall, end it
            if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                OnHitWall(collision);
            }

            else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && m_SpellData.TriggerGround)
            {
                OnHitGround(collision);
            }

            // if spell hits a player, hit it and end the spell
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && m_SpellData.TriggerPlayer)
            {
                OnHitPlayer(collision);
            }

            else if (collision.gameObject.layer == LayerMask.NameToLayer("Structure"))
            {
                OnHitStructure(collision);
            }
        }

        protected virtual void OnHitWall(Collider2D collision)
        {
            // check if should apply on hit
            if (!m_SpellData.ApplyIfNotHitting)
            {
                End();
                return;
            }

            // if "ApplyIfNotHitting" : apply effects to every not hit targets
            var allControllers = m_SpellData.IsEnemyTarget ? GameManager.Instance.GetAllEnemies(m_Controller.Team) : GameManager.Instance.GetAllAllies(m_Controller.Team);
            foreach (Controller controller in allControllers)
            {
                OnHit(controller);
            }
        }

        protected virtual void OnHitGround(Collider2D collision)
        {
            OnHitWall(collision);
        }

        protected virtual void OnHitPlayer(Collider2D collision)
        {
            var controller = Finder.FindComponent<Controller>(collision.gameObject);
            if (controller == null)
            {
                ErrorHandler.Error("Player has no Controller");
                return;
            }

            // check if should apply on hit
            if (m_SpellData.ApplyIfNotHitting && !controller.IsSpawn)
            {
                End();
                return;
            }

            OnHit(controller);
        }

        protected virtual void OnHitStructure(Collider2D collision)
        {
            OnHitPlayer(collision);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void UpdateMovement()
        {
            switch (m_SpellData.Trajectory)
            {
                case ESpellTrajectory.Curve:
                    UpdateCurveMovement();
                    break;
            }

            // all clients update the position of the spell (previsualisation)
            transform.Translate(m_SpellData.Speed * Time.deltaTime, 0, 0);

            // only server can check for distance
            if (!IsServer)
                return;

            // check if the spell has reached its max distance
            if ((m_SpellData.Distance > 0) && Math.Abs(transform.position.x - m_OriginalPosition.x) > m_SpellData.Distance)
                End();

            // check if the spell has reached its target position
            if (m_SpellData.StopOnTargetPos 
                && ((m_Target.x > m_OriginalPosition.x && transform.position.x >= m_Target.x)
                    || (m_Target.x < m_OriginalPosition.x && transform.position.x <= m_Target.x))
                )
                End();

            // check if the spell is stuck in the void
            if (m_SpellData.Trajectory != ESpellTrajectory.Straight
                && transform.position.y - m_SpellData.Size / 2 <= 0 
                && ArenaManager.IsInVoid(transform.position.x)
                )
                End();
        }

        protected override void SetTarget(Vector3 target)
        {
            // TODO : remove (TRUE) when IsAutoTarget is implemented
            // add a small adjustement to X to avoid targetting the enemy's feets (only for autotarget aiming the ground)
            if ((true || m_SpellData.IsAutoTarget) && target.y == 0)
            {
                // add X offset depending on the type of projectile
                target.x += CalculateTargetOffsetX();
            }

            // set value of the target
            base.SetTarget(target);

            // look at the direction of the target
            LookAt(m_Target);
        }

        float CalculateTargetOffsetX()
        {
            float offset = 0f;
            int direction = ArenaManager.GetAreaMovementDirection(m_Controller.Team, m_SpellData.IsEnemyTarget);

            switch (m_SpellData.Trajectory)
            {
                case ESpellTrajectory.Count:
                case ESpellTrajectory.Straight:
                case ESpellTrajectory.Hight:
                    offset = 0f;
                    break;

                case ESpellTrajectory.Curve:
                    offset = 0.2f;
                    break;

                case ESpellTrajectory.DiagonalMiddle:
                    offset = 0.5f;
                    break;

                case ESpellTrajectory.Diagonal:
                    offset = 0.8f;
                    break;

                default:
                    ErrorHandler.Warning("Unhandled case");
                    break;
            }

            return direction * offset;
        }

        #endregion


        #region Private Manipulators

        /// <summary>
        /// Rotate the projectile to look at the target
        /// </summary>
        void UpdateCurveMovement()
        {
            // calculate next position
            var x = Mathf.MoveTowards(transform.position.x, m_Target.x, m_SpellData.Speed * Time.deltaTime);
            var baseY = Mathf.Lerp(m_OriginalPosition.y, m_Target.y, (x - m_OriginalPosition.x) / m_MaxDistance);
            var height = m_MaxHeight * Math.Abs(x - m_OriginalPosition.x) * Math.Abs(x - m_Target.x) / (0.25f * m_MaxDistance * m_MaxDistance);

            // update rotation to look at next position
            LookAt(new Vector3(x, baseY + height, transform.position.z));
        }

        /// <summary>
        /// Set rotation of the spell to look at the target
        /// </summary>
        /// <param name="target"></param>
        void LookAt(Vector3 target)
        {
            Vector3 diff = target - transform.position;
            diff.Normalize();
            float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rot_z);
        }

        #endregion
    }
}