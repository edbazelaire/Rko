using System.Collections;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class PreviewFading : PreviewTimer
    {
        #region Members

        [SerializeField] private AnimationCurve m_FadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Default fade curve
        private GameObject m_FadingArea;
        private SpriteRenderer[] m_SpriteRenderers;
        private ParticleSystem[] m_ParticleSystems;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
            m_FadingArea = Finder.Find(gameObject, "FadingArea", false);

            // If FadingArea exists, only fade its children; otherwise, fade the entire object
            GameObject target = m_FadingArea != null ? m_FadingArea : gameObject;

            // Get all SpriteRenderers & ParticleSystems inside target
            m_SpriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            m_ParticleSystems = target.GetComponentsInChildren<ParticleSystem>(true);
        }

        #endregion


        #region Animation Methods

        protected override void OnAnimationTick()
        {
            base.OnAnimationTick();

            // Get fade value from the curve
            float fadeValue = m_FadeCurve.Evaluate(1 - m_PercentageTimeRemaining); // Uses remaining time percentage

            ApplyFade(fadeValue);
        }

        private void ApplyFade(float fadeValue)
        {
            // Apply opacity to all sprites
            foreach (var sprite in m_SpriteRenderers)
            {
                if (sprite == null)
                    return;

                Color color = sprite.color;
                color.a = fadeValue;
                sprite.color = color;
            }

            // Apply opacity to all particles
            foreach (var particleSystem in m_ParticleSystems)
            {
                if (particleSystem == null)
                    continue;
                
                var mainModule = particleSystem.main;
                Color color = mainModule.startColor.color;
                color.a = fadeValue;
                mainModule.startColor = color;
            }
        }

        #endregion
    }
}
