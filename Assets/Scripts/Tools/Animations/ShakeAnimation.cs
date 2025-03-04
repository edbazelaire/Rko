using System.Collections;
using UnityEngine;

namespace Tools.Animations
{
    public class ShakeAnimation : OvAnimation
    {
        #region Members

        [SerializeField] private float m_Intensity = 10f;
        private Vector3 m_OriginalPosition;

        #endregion


        #region Init & End

        public void Initialize(string id = "", float duration = -1f, float intensity = 10f)
        {
            m_Intensity = intensity;
            m_OriginalPosition = transform.localPosition;

            if (m_AnimationCurve == null || m_AnimationCurve == default)
            {
                m_AnimationCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),       // Start at 0
                    new Keyframe(0.5f, 1f),     // mid picked
                    new Keyframe(1f, 0f)        // End at 0
                );
            }

            base.Initialize(id, duration);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            transform.localPosition = m_OriginalPosition;
        }

        #endregion


        #region Animation

        protected override IEnumerator AnimationFrame()
        {
            yield return base.AnimationFrame();

            float shakeAmount = m_AnimationCurve.Evaluate(GetProgress()) * m_Intensity;
            transform.localPosition = m_OriginalPosition + (Vector3)Random.insideUnitCircle * shakeAmount;

            yield return null;
        }

        #endregion
    }
}