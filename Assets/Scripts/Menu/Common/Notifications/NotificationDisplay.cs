using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Notifications
{
    public class NotificationDisplay : MObject
    {
        #region Members

        bool                m_IsActivated;
        Vector2             m_Size;
        bool                m_WasEnabledBackground;

        Image               m_Background;
        ParticlesAnimation  m_ParticleAnimations;
        Color               m_BaseBackgroundColor;

        #endregion


        #region Init & End

        public void Initialize(Image background, Vector2 size)
        {
            m_Size = size;
            m_Background = background;  
            m_ParticleAnimations = null;
            m_BaseBackgroundColor = m_Background.color;

            base.Initialize();
        }

        #endregion


        #region Activation

        public void Activate()
        {
            if (m_IsActivated)
                return;

            m_IsActivated = true;
            m_WasEnabledBackground = false;

            if (m_Background != null)
            {
                if (! m_Background.gameObject.activeSelf)
                {
                    m_Background.gameObject.SetActive(true);
                }
                else
                {
                    m_WasEnabledBackground = true;
                }

                if (ColorUtility.TryParseHtmlString("#f4c633", out Color color))
                {
                    // Set color in settings
                    color.a = 0.75f;
                    m_Background.color = color;
                }
            }

            CoroutineManager.DelayMethod(AddNotificationParticles);
        }

        public void Deactivate()
        {
            if (! m_IsActivated)
                return;

            m_IsActivated = false;

            // Set UI Locked
            if (m_ParticleAnimations != null)
                m_ParticleAnimations.End();

            if (m_Background != null)
            {
                m_Background.color = m_BaseBackgroundColor;

                if (!m_WasEnabledBackground)
                    m_Background.gameObject.SetActive(false);
            }
        }

        void AddNotificationParticles()
        {
            if (! gameObject.activeInHierarchy)
                return;

            var currentCanvas = UIHelper.GetFirstCanvas(gameObject.transform);

            m_ParticleAnimations = gameObject.AddComponent<ParticlesAnimation>();
            m_ParticleAnimations.Initialize("", -1f, particlesName: "Notification", size: m_Size, layer: currentCanvas.sortingLayerName);
        }

        #endregion
    }
}