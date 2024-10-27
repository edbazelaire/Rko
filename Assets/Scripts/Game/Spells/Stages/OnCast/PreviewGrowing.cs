using Game.SpellGFXs;
using System.Collections;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class PreviewGrowing : PreviewTimer
    {
        #region Members

        GameObject  m_GrowingArea;
        Vector3     m_GrowingAreaBaseScale;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_GrowingArea = Finder.Find(gameObject, "GrowingArea");
            m_GrowingAreaBaseScale = m_GrowingArea.transform.localScale;
        }

        #endregion


        #region Animation Methods

        protected override void OnAnimationStarting()
        {
            base.OnAnimationStarting();

            // init scale
            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, m_SpellData.Size);
            m_GrowingArea.transform.localScale = Vector3.zero;
        }


        protected override void OnAnimationTick()
        {
            m_GrowingArea.transform.localScale = m_GrowingAreaBaseScale * (1 - (m_Timer / m_Duration));
        }

        #endregion
    }
}