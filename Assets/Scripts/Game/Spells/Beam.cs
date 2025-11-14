using Data;
using Data.GameManagement;
using Tools;
using Tools.Helpers;
using UnityEngine;

namespace Game.Spells
{
    public class Beam : Zone
    {
        #region Members

        BeamData m_SpellData => m_BaseSpellData as BeamData;

        float   m_CurrentLength;
        Vector3 m_AimingTarget;
        Vector3 m_CollisionPoint;

        Vector3 m_Origin => transform.position;
        public Vector3 AimingTarget => m_AimingTarget;
        public Vector3 CollisionPoint => m_CollisionPoint;

        #endregion


        #region Init & End

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            m_AimingTarget = m_Target;
        }

        protected override void End()
        {
            base.End();
        }

        #endregion


        #region Update

        protected override void Update()
        {
            base.Update();

            if (!IsServer)
                return;

            if (m_IsOver)
                return;

            // update aiming position (to follow target if needed)
            UpdateAimingPosition();

            // prevent rotation
            transform.rotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            // prevent rotation
            transform.rotation = Quaternion.identity;
        }

        void UpdateAimingPosition()
        {
            if (! m_SpellData.FollowTarget)
                return;

            // recalculate current target to follow
            TargetHelper.GetTargetPosition(
                target:         ref m_Target, 
                spellTarget:    m_SpellData.TargetToFollow, 
                offset:         Vector2.zero,                   // TODO : Offset for re-target ?
                clampTargetPos: m_SpellData.ClampTargetPos, 
                casterId:       m_Caster.PlayerId, 
                targetId:       null
            );

            // inifinit speed - instantly snap
            if (m_SpellData.FollowingSpeed < 0f)
            {
                m_AimingTarget = m_Target;
                return;
            }

            // direction to the final target
            Vector3 dir = m_Target - m_AimingTarget;

            // move to the target
            float step = m_SpellData.FollowingSpeed * Time.deltaTime;
            if (dir.magnitude <= step)
            {
                m_AimingTarget = m_Target; // snap if close
            }
            else
            {
                m_AimingTarget += dir.normalized * step;
            }
        }

        protected override void CreateCollisionCircle()
        {
            // set collision point to current aiming target
            m_CollisionPoint = m_AimingTarget;

            if (m_SpellData.RaySize > 0)
            {
                float maxDist = m_SpellData.MaxLength > 0 ? m_SpellData.MaxLength : Mathf.Infinity;
                Vector3 dir = (m_AimingTarget - m_Origin).normalized;

                // length of the ray
                m_CurrentLength = Mathf.Min(Vector3.Distance(m_Origin, m_CollisionPoint), maxDist);

                // get colliders in the Raycast
                RaycastHit2D[] hits = Physics2D.CircleCastAll(
                    m_Origin,
                    m_SpellData.RaySize * Settings.SpellSizeFactor,               // circle size projecting
                    dir,
                    m_CurrentLength,
                    TargetHelper.ALL_LAYER_MASK
                );

                // check hits
                foreach (var h in hits)
                {
                    if (!h.collider) continue;

                    // CHECK : Counters
                    if (CheckHitCounter(h))
                    {
                        m_CollisionPoint = h.point;
                        break;
                    }

                    var controller = Finder.FindComponent<Controller>(h.collider.gameObject);

                    if (!controller || controller == Caster) continue;

                    // TODO : CHECK targeting of the beam (Ally or Enemy ?)
                    if (controller.Team != m_Caster.Team)
                    {
                        // set collision point to current aiming target
                        m_CollisionPoint = h.point;
                        break;
                    }
                }
            }

            base.CreateCollisionCircleOnPosition(m_CollisionPoint);
        }

        bool CheckHitCounter(RaycastHit2D hit)
        {
            var counter = hit.collider.gameObject.GetComponent<Counter>();
            if (counter == null || counter.Caster.Team == m_Caster.Team)
                return false;

            if (! counter.ProcCounter(this))
                return false;

            return true;
        }

        #endregion


        #region Target

        protected override void SetTarget(Vector3 target)
        {
            m_Target = target;
        }

        #endregion


        #region Debug Tools

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (m_IsOver || m_SpellData == null)
                return;

            // Origin
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(m_Origin, 0.1f);

            // CollisionPoint
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(m_CollisionPoint, 0.1f);

            // AimingPosition
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(m_AimingTarget, 0.1f);

            // Target (final)
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(m_Target, 0.1f);

            // Beam ray
            DisplayBeam();
        }

        void DisplayBeam()
        {
            Vector3 dir = (m_AimingTarget - m_Origin).normalized;

            // === Beam line (center) ===
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(m_Origin, m_Origin + dir * m_CurrentLength);

            // === Beam width (CircleCast radius) ===
            float radius = m_SpellData.Size * 0.5f;

            // draw wire spheres at start & end
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_Origin, radius);
            Gizmos.DrawWireSphere(m_Origin + dir * m_CurrentLength, radius);

            // draw "edges" (approximate tube)
            Vector3 ortho = Vector3.Cross(dir, Vector3.forward).normalized * radius;

            Vector3 startLeft = m_Origin - ortho;
            Vector3 startRight = m_Origin + ortho;
            Vector3 endLeft = (m_Origin + dir * m_CurrentLength) - ortho;
            Vector3 endRight = (m_Origin + dir * m_CurrentLength) + ortho;

            Gizmos.DrawLine(startLeft, endLeft);
            Gizmos.DrawLine(startRight, endRight);
        }

        #endregion
    }
}
