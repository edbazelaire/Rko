using Enums;
using System.Collections;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class DuskfallGFX : AnimationQueueGFX
    {
        #region Members

        // ================================================================================
        // GameObjects & Components
        GameObject m_Charge;

        // ================================================================================
        // Data
        Vector3 m_InitialPosition;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            // Find all Particles
            m_Charge = Finder.Find(gameObject, "Charge");

            // Find Initial position
            m_InitialPosition = m_Controller.GFXHandler.CharacterPreview.transform.localPosition;
            // Block movement during the animation
            m_Controller.Movement.CancelMovement(true);

            // deactivate all Particles
            m_Charge.SetActive(false);
        }

        protected override void RegisterAnimations()
        {
            base.RegisterAnimations();

            m_AnimationQueue.Enqueue(Charge());
            m_AnimationQueue.Enqueue(Strike());
        }

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();
        }

        protected override void StartAnimation()
        {
            base.StartAnimation();
        }

        protected override void ForceEnd()
        {
            m_Controller.GFXHandler.CharacterPreview.transform.localPosition = m_InitialPosition;
            base.ForceEnd();
        }

        #endregion


        #region Animations

        protected override void Update()
        {
            base.Update();
        }

        IEnumerator Charge()
        {
            m_Controller.AnimationHandler.PlayAnimation(EAnimation.Jump, m_Duration);

            m_Charge.SetActive(true);

            m_Controller.GFXHandler.CharacterPreview.transform.position = transform.position;

            yield return new WaitForSeconds(m_Duration * 0.9f);

            m_Charge.SetActive(false);

            // After animation is done, proceed to the next animation
            StartCoroutine(NextAnimation());
        }

        IEnumerator Strike()
        {
            var baseHight = transform.position.y;
            var position = transform.position;
            var baseDuration = m_Duration * 0.1f;
            var duration = baseDuration;

            while (duration > 0)
            {
                position.y = baseHight * (duration / baseDuration);
                duration -= Time.deltaTime;
                yield return null;
            }
        }

        #endregion


        #region Helpers


        #endregion
    }
}
