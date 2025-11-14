using System.Collections;
using UnityEngine;

namespace Tools.Animations
{
    public class RotateAnimation : OvAnimation
    {
        #region Members

        [SerializeField] Vector3 m_Rotation;
        Vector3 m_BaseRotation;

        #endregion


        #region Init & End

        public void Initialize(string id = "", float duration = 1f, Vector3 rotation = default, Vector3? fromRotation = null)
        {
            base.Initialize(id, duration);

            m_Rotation = rotation;
            m_BaseRotation = fromRotation ?? transform.rotation.eulerAngles;
        }

        public override void Deactivate()
        {
            base.Deactivate();

            transform.rotation = Quaternion.Euler(m_Rotation);
        }

        #endregion


        #region Animation

        protected override IEnumerator AnimationFrame()
        {
            // interpolate position
            transform.rotation = Quaternion.Euler(Vector3.LerpUnclamped(m_BaseRotation, m_Rotation, GetProgress()));

            m_Timer += Time.deltaTime;
            yield return null;
        }

        #endregion
    }
}