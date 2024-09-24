using Data.GameManagement;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class ShardrotChargeGFX : SpellGFX
    {
        #region Members

        // ================================================================================
        // GameObjects & Components
        GameObject m_Flash;
        GameObject m_Core;
        GameObject m_FatShard;

        // ================================================================================
        // Data
        Queue<IEnumerator> m_AnimationQueue = new Queue<IEnumerator>();
        float m_Timer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            // Find all Particles
            m_Flash = Finder.Find(gameObject, "Flash");
            m_Core = Finder.Find(gameObject, "Core");
            m_FatShard = Finder.Find(gameObject, "FatShard");

            // deactivate all Particles
            m_Flash.SetActive(false);
            m_Core.SetActive(false);
            m_FatShard.SetActive(false);
        }

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            // init timer data 
            CalculateDuration();
            m_Timer = m_Duration;

            // re-adjust size to negate the character's size modification
            transform.localScale /= m_Controller.GFXHandler.CharacterSize;
        }

        protected override void StartAnimation()
        {
            base.StartAnimation();

            // Queue animations
            m_AnimationQueue.Enqueue(MoveFatShard());
            m_AnimationQueue.Enqueue(Explosion());

            // Start the Asynchrone Movement animation
            StartCoroutine(MoveToTarget());

            // Start the first animation
            StartCoroutine(NextAnimation());
        }

        public override void End()
        {
            base.End();
        }

        #endregion


        #region Animations

        private void Update()
        {
            m_Timer -= Time.deltaTime;

            // make sure game object is inclinated towards the target position
            UpdateRotation();

            if (m_Timer < 0)
                End();
        }

        private IEnumerator NextAnimation(float timer = 0f)
        {
            if (m_AnimationQueue.Count == 0)
                yield break;

            yield return new WaitForSeconds(timer);

            IEnumerator currentAnimation = m_AnimationQueue.Dequeue();
            yield return StartCoroutine(currentAnimation);
        }

        IEnumerator MoveToTarget()
        {
            // Save the starting position (P0) at the beginning of the movement
            float startY = transform.position.y;

            // Track elapsed time
            float elapsedTime = 0f;

            // Run the loop for the duration of the animation
            while (elapsedTime < m_Duration)
            {
                // Increment elapsed time
                elapsedTime += Time.deltaTime;

                // Normalize 't' between 0 and 1 based on elapsed time
                float t = Mathf.Clamp01(elapsedTime / m_Duration);

                // Move the object to the new position
                var position = transform.position;
                position.y = startY + (Settings.SPELL_DIAGONAL_POS_Y - startY) * t;
                transform.position = position;

                // Wait for the next frame
                yield return null;
            }
        }

        IEnumerator MoveFatShard()
        {
            // Timer for the animation duration
            m_FatShard.gameObject.SetActive(true);

            yield return new WaitForSeconds(m_Duration * 0.5f);

            // After animation is done, proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        IEnumerator Explosion()
        {
            m_FatShard.SetActive(false);
            m_Flash.SetActive(true);
            m_Core.SetActive(true);

            yield return new WaitForSeconds(m_Duration * 0.5f);

            m_Flash.SetActive(false);                // deactivate at the end of the animation
        }

        #endregion


        #region Helpers

        void UpdateRotation()
        {
            // Get the direction vector between the current position and the target position
            Vector3 direction = new Vector3(transform.position.x + Settings.SpellFixedDistance, 0) - transform.position;

            // Calculate the angle in radians using Atan2
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Apply the rotation around the Z-axis
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        #endregion
    }
}
