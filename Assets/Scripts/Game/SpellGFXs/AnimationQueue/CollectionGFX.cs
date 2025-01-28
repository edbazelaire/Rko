using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class CollectionGFX : AnimationQueueGFX
    {
        #region Members

        // ================================================================================
        // Serialized Fields
        [SerializeField, Tooltip("List of particles called when spawning on base position")]
        protected List<GameObject> m_OnProcGFX;
        [SerializeField, Tooltip("List of particles called during movement from base position to the target")]
        protected List<GameObject> m_OnMoveGFX;
        [SerializeField, Tooltip("List of particles called when the spell reaches the target")]
        protected List<GameObject> m_OnEndGFX;
        
        [SerializeField, Tooltip("Offset of the target at the end (to compensate for particle movements)")]
        protected SOffset m_TargetOffset;

        // ================================================================================
        // Data
        protected Vector3 m_BasePosition;
        protected Controller m_Target;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            // set Data
            m_BasePosition = transform.position;
            m_Target = m_Controller;

            // deactivate all stages
            ActivateObjects(m_OnProcGFX,    false);
            ActivateObjects(m_OnMoveGFX,    false);
            ActivateObjects(m_OnEndGFX,     false);
        }

        protected override void RegisterAnimations()
        {
            base.RegisterAnimations();

            m_AnimationQueue.Enqueue(Proc());
            m_AnimationQueue.Enqueue(Move());
            m_AnimationQueue.Enqueue(EndEffect());
        }

        #endregion


        #region Animations

        IEnumerator Proc()
        {
            // No effects : go straight to next animation
            if (m_OnProcGFX == null || m_OnProcGFX.Count == 0)
            {
                // After animation is done, proceed to the next animation
                StartCoroutine(NextAnimation());
                yield break;
            }

            ActivateObjects(m_OnProcGFX, true);

            yield return new WaitForSeconds(m_Duration * 0.15f);

            ActivateObjects(m_OnProcGFX, false);

            // After animation is done, proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        IEnumerator Move()
        {
            if (! m_Target.GFXHandler.IsVisible)
            {
                // After animation is done, proceed to the next animation
                StartCoroutine(NextAnimation());
                yield break;
            }

            // No effects : go straight to next animation
            if (m_OnMoveGFX == null || m_OnMoveGFX.Count == 0)
            {            
                // After animation is done, proceed to the next animation
                StartCoroutine(NextAnimation());
                yield break;
            }

            ActivateObjects(m_OnMoveGFX, true);

            // Track elapsed time
            float duration = m_Duration * 0.6f;

            // Save the starting position (P0) at the beginning of the movement
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
                position.x = m_BasePosition.x + (m_Target.transform.position.x - m_BasePosition.x + m_TargetOffset.X) * t;
                position.y = m_BasePosition.y + (m_Target.transform.position.y - m_BasePosition.y + m_TargetOffset.X) * t;
                transform.position = position;

                // update rotation
                transform.LookAt(m_Target.transform.position);

                // Wait for the next frame
                yield return null;
            }

            ActivateObjects(m_OnMoveGFX, false);

            // After animation is done, proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        IEnumerator EndEffect()
        {
            if (!m_Target.GFXHandler.IsVisible)
            {
                // character not visible, skip end effect
                StartCoroutine(NextAnimation());
                yield break;
            }

            if (m_OnEndGFX == null || m_OnEndGFX.Count == 0)
            {
                // After animation is done, proceed to the next animation
                StartCoroutine(NextAnimation());
                yield break;
            }

            ActivateObjects(m_OnEndGFX, true);
            foreach(var obj in m_OnEndGFX)
                obj.transform.localScale = Vector3.one * m_Target.GFXHandler.CharacterSize;

            yield return new WaitForSeconds(m_Duration * 0.25f);

            ActivateObjects(m_OnEndGFX, false);

            // After animation is done, proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        #endregion
    }       
}
