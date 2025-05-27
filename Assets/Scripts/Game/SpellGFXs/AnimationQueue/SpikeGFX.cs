using System.Collections;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class SpikeGFX : AnimationQueueGFX
    {
        #region Members

        // ================================================================================  
        // Serialized Fields  
        [SerializeField, Tooltip("Max Height")]
        protected float m_MaxHeight;

        [SerializeField, Tooltip("Height Curve for animation")]
        private AnimationCurve m_HeightCurve;  

        private GameObject m_Spike;
        private Vector3 m_InitialPosition; // Store starting position

        #endregion

        #region Init & End

        protected override void FindComponents()
        {
            // Set Data
            m_Spike = Finder.Find(gameObject, "Spike");

            // save initial position and make sure that the Y starts at 0f
            m_InitialPosition = m_Spike.transform.localPosition; // Store initial local position
            m_InitialPosition.y = 0f;
            m_Spike.transform.localPosition = m_InitialPosition;
        }

        protected override void RegisterAnimations()
        {
            base.RegisterAnimations();
            m_AnimationQueue.Enqueue(Proc());
        }

        #endregion


        #region Animations

        IEnumerator Proc()
        {
            // No effects: go straight to next animation
            if (m_Spike == null)
            {
                ErrorHandler.Warning("Spike is null");
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < m_Duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / m_Duration); // Normalize time [0,1]

                // Apply curve to determine height offset
                float heightOffset = m_HeightCurve.Evaluate(t) * m_MaxHeight;

                // Set new position
                m_Spike.transform.localPosition = m_InitialPosition + new Vector3(0, heightOffset, 0);

                yield return null;
            }

            // Proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        #endregion
    }

}
