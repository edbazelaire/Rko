using Data.GameManagement;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;


namespace Game.SpellGFXs
{
    public class ShardrotChargeGFX : AnimationQueueGFX
    {
        #region Members

        // ================================================================================
        // GameObjects & Components
        GameObject m_Scythe;
        Animator m_Animator;

        // ================================================================================
        // Data
        Vector3 m_BasePosition;
        Vector3 m_TargetPosition;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            // Find all Components
            m_Scythe = Finder.Find(gameObject, "Scythe");
            m_Animator = Finder.FindComponent<Animator>(m_Scythe);
            m_Animator.enabled = false;
        }

        protected override void RegisterAnimations()
        {
            base.RegisterAnimations();

            m_AnimationQueue.Enqueue(Charge());
            m_AnimationQueue.Enqueue(Attack());
        }

        protected override void StartAnimation()
        {
            // check Data
            m_BasePosition = transform.position;
            m_TargetPosition = m_Controller.SpellHandler.TargetPos;

            // enable animator
            m_Animator.enabled = true;

            base.StartAnimation();
        }

        #endregion


        #region Animations

        IEnumerator Charge()
        {
            m_Animator.Play("Charging");

            yield return new WaitForSeconds(m_Duration * 0.5f);

            // After animation is done, proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        IEnumerator Attack()
        {
            // Track elapsed time
            float duration = m_Duration * 0.5f;

            m_Animator.SetFloat("AnimationSpeed", 1 / duration);
            m_Animator.Play("Attack");

            // Save the starting position (P0) at the beginning of the movement
            float startX = transform.position.x;
            float elapsedTime = 0f;

            // Run the loop for the duration of the animation
            while (elapsedTime < duration)
            {
                // Increment elapsed time
                elapsedTime += Time.deltaTime;

                // Normalize 't' between 0 and 1 based on elapsed time
                float t = Mathf.Clamp01(elapsedTime / duration);

                // Move the object to the new position
                var position = transform.position;
                position.x = startX + (m_TargetPosition.x - startX) * t;
                transform.position = position;

                // Wait for the next frame
                yield return null;
            }
        }

        #endregion


        #region Helpers

        #endregion
    }
}
