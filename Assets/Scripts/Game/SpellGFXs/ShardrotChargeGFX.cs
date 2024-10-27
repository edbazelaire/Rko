using Data.GameManagement;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class ScythefallGFX : AnimationQueueGFX
    {
        #region Members

        // ================================================================================
        // GameObjects & Components
        GameObject m_Flash;
        GameObject m_Core;
        GameObject m_FatShard;

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

        protected override void RegisterAnimations()
        {
            base.RegisterAnimations();

            m_AnimationQueue.Enqueue(MoveFatShard());
            m_AnimationQueue.Enqueue(Explosion());
        }

        protected override void StartAnimation()
        {
            // Start the Asynchrone Movement animation
            StartCoroutine(MoveToTarget());

            base.StartAnimation();
        }

        #endregion


        #region Animations

        protected override void Update()
        {
            base.Update();

            // make sure game object is inclinated towards the target position
            UpdateRotation();
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
