using Game.Spells;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class BeamRayGFX : BeamGFX
    {
        #region Members

        /// <summary> list of all particle systems inside the "BeamRay" object that need to be adjusted </summary>
        List<ParticleSystem> m_RayParticleSystems;
        /// <summary> initial distance between base of the Ray and its End </summary>
        float m_InitialDistance;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_InitialDistance = Mathf.Abs(Vector2.Distance(m_BeamRay.transform.position, m_EndBeam.transform.position));
            m_RayParticleSystems = Finder.FindComponents<ParticleSystem>(m_BeamRay);
        }

        protected override void ForceEnd()
        {
            base.ForceEnd();
        }

        #endregion


        #region Update

        /// <summary>
        /// Update ray lifetime and rotation
        /// </summary>
        protected override void UpdateRay()
        {
            // adjust rotation
            m_BeamRay.transform.LookAt(m_EndBeam.transform);

            // calculate new distance to adjust lifetime
            var currentDistance = Mathf.Abs(Vector2.Distance(m_BeamRay.transform.position, m_EndBeam.transform.position));
            foreach (var ps in m_RayParticleSystems)
            {
                var main = ps.main;
                main.startLifetimeMultiplier = currentDistance / m_InitialDistance;
            }
        }

        #endregion
    }
}
