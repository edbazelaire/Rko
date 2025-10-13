using Game.Spells;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class BeamGFX : SpellGFX
    {
        #region Members

        [Tooltip("Prefab for each pulse of the beam")]
        public GameObject m_BeamRay;

        [Tooltip("Graphics at the end of the beam. Which is also the position that the BeamRay is moving towards.")]
        public GameObject m_EndBeam;

        protected Beam m_BeamSpell => m_Spell as Beam;

        #endregion


        #region Init & End



        #endregion


        #region Update

        protected override void ApplyPostProcessing()
        {
            // call update method once to set every value
            if (m_EndBeam != null && m_BeamSpell != null)
                m_EndBeam.transform.position = m_BeamSpell.CollisionPoint;

            UpdateRay();
            UpdateTarget();
        }

        protected virtual void Update()
        {
            // EndBeam = impact point
            if (m_EndBeam != null && m_BeamSpell != null)
                m_EndBeam.transform.position = m_BeamSpell.CollisionPoint;

            UpdateRay();
            UpdateTarget();
        }

        protected virtual void UpdateRay() { }

        protected virtual void UpdateTarget() 
        {
            // Direction du rayon (dans le plan XY)
            Vector3 dir = (m_EndBeam.transform.position - transform.position).normalized;

            // Calcul de l'angle en 2D (XY → rotation autour de Z)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Appliquer la rotation uniquement sur Z
            m_EndBeam.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        #endregion
    }
}
