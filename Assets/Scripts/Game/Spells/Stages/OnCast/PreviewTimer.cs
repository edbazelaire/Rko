using Data;
using Data.GameManagement;
using Enums;
using Game.SpellGFXs;
using System.Collections;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Spells
{
    public class PreviewTimer : Preview
    {
        #region Members

        /// <summary> curent value of the timer </summary>
        protected float m_Timer;

        protected float m_PercentageTimeRemaining => Mathf.Clamp01(m_Timer / m_Duration);
        public float Timer => m_Timer;

        #endregion


        #region Init & End

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();
            CalculateDuration();
        }

        #endregion


        #region Animations 

        protected override void StartAnimation()
        {
            base.StartAnimation();

            // initialize when animation starts
            OnAnimationStarting();

            // start animation
            StartCoroutine(AnimationCoroutine());
        }

        protected virtual void OnAnimationStarting()
        {
            // init timer
            m_Timer = m_Duration;
        }

        protected virtual IEnumerator AnimationCoroutine()
        {
            while (m_Timer > 0 && !m_EndStarted)
            {
                m_Timer -= Time.deltaTime;
                OnAnimationTick();
                yield return null;
            }

            End();
        }

        protected virtual void OnAnimationTick() { }

        #endregion
    }
}