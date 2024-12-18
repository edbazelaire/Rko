using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class AnimationQueueGFX : SpellGFX
    {
        #region Members

        protected Queue<IEnumerator> m_AnimationQueue = new Queue<IEnumerator>();
        protected float m_Timer;

        #endregion


        #region Init

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            // init timer data 
            CalculateDuration();
            m_Timer = m_Duration;

            // re-adjust size to negate the character's size modification
            transform.localScale /= m_Controller.GFXHandler.CharacterSize;

            // register list of animations
            RegisterAnimations();
        }

        protected virtual void RegisterAnimations() { }

        #endregion


        #region Update

        protected virtual void Update() 
        {
            m_Timer -= Time.deltaTime;

            if (m_Timer < 0)
                End();
        }

        #endregion


        #region Animation Management

        protected override void StartAnimation()
        {
            base.StartAnimation();

            // Start the first animation
            StartCoroutine(NextAnimation());
        }

        protected IEnumerator NextAnimation(float timer = 0f)
        {
            if (m_AnimationQueue.Count == 0)
                yield break;

            yield return new WaitForSeconds(timer);

            IEnumerator currentAnimation = m_AnimationQueue.Dequeue();
            yield return StartCoroutine(currentAnimation);
        }

        #endregion
    }
}