using Data;
using Game.Spells;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class BeamRayCastGFX : BeamGFX
    {
        #region Members

        /// <summary> line renderer </summary>
        [SerializeField] LineRenderer m_LineRenderer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            // Setup de base du LineRenderer (tu peux ajuster dans l’Inspector aussi)
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.useWorldSpace = true;

            // update width based on current scale
            m_LineRenderer.widthMultiplier = transform.localScale.y * (m_SpellData as BeamData).RaySizePerc;
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
            // Set start and end points
            m_LineRenderer.SetPosition(0, transform.position);
            m_LineRenderer.SetPosition(1, m_EndBeam.transform.position);
        }

        #endregion
    }
}
