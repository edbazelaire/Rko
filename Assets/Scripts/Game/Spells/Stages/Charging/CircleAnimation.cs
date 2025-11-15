using Game.SpellGFXs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spells
{
    public class CircleAnimation : SpellGFX
    {
        #region Members

        [SerializeField, Tooltip("List of objects affected by the circling animation")]
        protected List<GameObject> m_Objects = new();

        [SerializeField, Tooltip("Distance to the TargetPos at the end of the animation - relative to the start distance (1 = keep same radius, 2 = double, 0.5 = shrink)")]
        protected float m_EndDistance = 1f;

        [Header("Orbit Shape")]
        [SerializeField, Tooltip("Ellipse scaling on X and Y (1,1 makes a circle). These are multipliers applied to each object's initial radius.")]
        protected Vector2 m_EllipseXY = new(1f, 1f);

        [SerializeField, Tooltip("Rotation (in degrees) of the ellipse within the XY plane, around the pivot")]
        protected float m_EllipseRotationDeg = 0f;

        [Header("Motion")]
        [SerializeField, Tooltip("How many full revolutions each object completes over the whole duration")]
        protected float m_Revolutions = 1f;

        [SerializeField, Tooltip("Clockwise motion if true, counter-clockwise if false")]
        protected bool m_Clockwise = false;

        [SerializeField, Tooltip("Controls how the radius scales from 1.0 to EndDistance over normalized time [0..1]")]
        protected AnimationCurve m_RadiusOverTime = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Keep each object's original Z unchanged (useful for 2D setups on XY). If false, use pivot's Z.")]
        protected bool m_LockOriginalZ = true;

        // Internal state per orbiting object captured at start.
        struct OrbitState
        {
            public float startRadius;   // initial distance to pivot in the XY plane
            public float startAngle;    // initial parametric angle (taking ellipse scaling into account)
            public float startZ;        // z to keep if m_LockOriginalZ is true
        }

        // One state per entry in m_Objects (same indexing).
        readonly List<OrbitState> m_States = new();
        readonly List<Vector3> m_InitialPositions = new();

        Vector3 m_TargetPos => transform.position;
        float m_Timer;

        #endregion


        #region Init & End

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            // Reset timer to full duration each time the effect is (re)applied.
            m_Timer = m_Duration;

            // Save absolute initial positions the first time
            if (m_InitialPositions.Count == 0 && m_Objects != null)
            {
                foreach (var go in m_Objects)
                    m_InitialPositions.Add(go != null ? go.transform.position : Vector3.zero);
            }

            // (Re)build per-object cached orbit data based on current positions.
            BuildStates();
        }

        /// <summary>
        /// Precompute each object's starting radius and parametric angle.
        /// Using an ellipse-aware angle makes the initial placement feel natural even when the ellipse is stretched.
        /// </summary>
        void BuildStates()
        {
            m_States.Clear();
            if (m_Objects == null) return;

            // Safety clamps to avoid divisions by zero during ellipse-aware angle calculation.
            float ex = Mathf.Approximately(m_EllipseXY.x, 0f) ? 1e-5f : m_EllipseXY.x;
            float ey = Mathf.Approximately(m_EllipseXY.y, 0f) ? 1e-5f : m_EllipseXY.y;

            Vector3 pivot = m_TargetPos; // Assumes SpellGFX exposes the target pivot position as TargetPos.

            for (int i = 0; i < m_Objects.Count; i++)
            {
                var go = m_Objects[i];
                if (go == null)
                {
                    m_States.Add(default);
                    continue;
                }

                Vector3 world = go.transform.position;
                Vector3 delta = world - pivot;

                // We orbit in the XY plane by design (X,Y control asked).
                Vector2 deltaXY = new(delta.x, delta.y);

                float r = deltaXY.magnitude;

                // Ellipse-aware angle: atan2(y/ey, x/ex) ensures the initial angle maps well to the chosen ellipse.
                float angle = (r > 1e-6f)
                    ? Mathf.Atan2(deltaXY.y / ey, deltaXY.x / ex)
                    : 0f;

                m_States.Add(new OrbitState
                {
                    startRadius = r,
                    startAngle = angle,
                    startZ = world.z
                });
            }
        }

        protected virtual void OnDisable()
        {
            m_Timer = 0f;
            m_States.Clear();

            // Restore original absolute positions
            for (int i = 0; i < m_Objects.Count && i < m_InitialPositions.Count; i++)
            {
                if (m_Objects[i] != null)
                    m_Objects[i].transform.position = m_InitialPositions[i];
            }
        }

        #endregion


        #region Update Manipulators

        protected virtual void Update()
        {
            if (GameManager.IsGameOver)
                return;

            if (m_Timer <= 0f || m_Duration <= 0f)
                return;

            // Decrease remaining time.
            m_Timer = Math.Max(0, m_Timer - Time.deltaTime);

            // Normalized progress [0..1] where 0 = start, 1 = end.
            float t = 1f - (m_Timer / m_Duration);

            // Angle advanced since the start (in radians).
            // m_Revolutions specifies total spins over the whole duration.
            float dir = m_Clockwise ? -1f : 1f;
            float deltaAngle = dir * (Mathf.PI * 2f) * m_Revolutions * t;

            // Radius scale over time:
            // radius(t) = startRadius * lerp(1, EndDistance, curve(t))
            float curve01 = Mathf.Clamp01(m_RadiusOverTime != null ? m_RadiusOverTime.Evaluate(t) : t);
            float radiusScale = Mathf.LerpUnclamped(1f, m_EndDistance, curve01);

            // Precompute ellipse rotation in the plane.
            float rotRad = m_EllipseRotationDeg * Mathf.Deg2Rad;
            float cosR = Mathf.Cos(rotRad);
            float sinR = Mathf.Sin(rotRad);

            // Safety clamps for ellipse multipliers.
            float ex = Mathf.Approximately(m_EllipseXY.x, 0f) ? 1e-5f : m_EllipseXY.x;
            float ey = Mathf.Approximately(m_EllipseXY.y, 0f) ? 1e-5f : m_EllipseXY.y;

            Vector3 pivot = m_TargetPos;

            // Drive each object's position.
            for (int i = 0; i < m_Objects.Count; i++)
            {
                GameObject go = m_Objects[i];
                if (go == null) continue;

                // If states list is out of sync (e.g., objects changed at runtime), rebuild once.
                if (i >= m_States.Count)
                {
                    BuildStates();
                    if (i >= m_States.Count) break;
                }

                OrbitState s = m_States[i];

                // Current parametric angle along the ellipse.
                float theta = s.startAngle + deltaAngle;

                // Semi-axes at time t: startRadius scaled by ellipse multipliers and centrifugal factor.
                float a = s.startRadius * ex * radiusScale; // X semi-axis
                float b = s.startRadius * ey * radiusScale; // Y semi-axis

                // Parametric ellipse in its local (unrotated) frame.
                float x = Mathf.Cos(theta) * a;
                float y = Mathf.Sin(theta) * b;

                // Apply in-plane rotation of the ellipse.
                float xr = x * cosR - y * sinR;
                float yr = x * sinR + y * cosR;

                // Compose final world position.
                Vector3 newPos = new Vector3(pivot.x + xr, pivot.y + yr, m_LockOriginalZ ? s.startZ : pivot.z);

                go.transform.position = newPos;
            }
        }

        #endregion
    }
}
