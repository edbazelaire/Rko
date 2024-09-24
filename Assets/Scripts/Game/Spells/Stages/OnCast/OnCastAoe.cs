using Game.SpellGFXs;
using System.Collections;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class OnCastAoe : SpellGFX
    {
        #region Members

        const string c_GrowingArea      = "GrowingArea";

        GameObject  m_GrowingArea;
        Vector3     m_GrowingAreaBaseScale;

        /// <summary> curent value of the timer </summary>
        float m_Timer;

        public float Timer => m_Timer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_GrowingArea = Finder.Find(gameObject, c_GrowingArea);
            m_GrowingAreaBaseScale = m_GrowingArea.transform.localScale;
        }

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();
            CalculateDuration();
        }

        #endregion


        #region Update Manipulators

        protected override void StartAnimation()
        {
            // init scale
            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, transform.localScale.z);
            m_GrowingArea.transform.localScale = Vector3.zero;

            // init timer
            m_Timer = m_Duration;

            // start animation
            StartCoroutine(AnimationCoroutine());
        }

        protected virtual IEnumerator AnimationCoroutine()
        {
            while (m_Timer > 0 && ! m_EndStarted)
            {
                m_Timer -= Time.deltaTime;
                m_GrowingArea.transform.localScale = m_GrowingAreaBaseScale * (1 - (m_Timer / m_Duration));
                yield return null;
            }

            End();
        }

        #endregion
    }
}