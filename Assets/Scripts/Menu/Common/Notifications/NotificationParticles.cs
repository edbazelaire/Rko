using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Notifications
{
    public class NotificationParticles : MObject, INotification
    {
        #region Members

        bool                m_IsActivated;
        Vector2             m_Size;
        bool                m_WasEnabledBackground;

        Image               m_Background;
        ParticlesAnimation  m_ParticleAnimations;
        Color               m_BaseBackgroundColor;
        Color?              m_ReplacementColor = null;

        #endregion


        #region Init & End

        public static NotificationParticles Add(GameObject gameObject, Image background, Vector2 size, string colorHexa = "#f4c633", float alpha = 0.75f)
        {
            // not existing on target : add component
            if (!gameObject.TryGetComponent<NotificationParticles>(out var notificationDisplay))
                notificationDisplay = gameObject.AddComponent<NotificationParticles>();

            // if already active : do nothing
            if (notificationDisplay.m_IsActivated)
                return notificationDisplay;

            notificationDisplay.Initialize(background, size, colorHexa, alpha);
            notificationDisplay.Activate();

            return notificationDisplay;
        }

        public static void Remove(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<NotificationParticles>(out var notificationDisplay))
                return;

            notificationDisplay.Deactivate();
        }

        public void Initialize(Image background, Vector2 size, string colorHexa = "#f4c633", float alpha = 0.75f)
        {
            m_Size = size;
            m_Background = background;  
            m_ParticleAnimations = null;

            if (! string.IsNullOrEmpty(colorHexa) && ColorUtility.TryParseHtmlString(colorHexa, out Color color))
            {
                m_BaseBackgroundColor = m_Background.color;
                color.a = Mathf.Clamp01(alpha);
                m_ReplacementColor = color;
            }

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

                if (m_ReplacementColor != null)
                    m_Background.color = m_ReplacementColor.Value;
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

            // check manually
            var particleAnimations = Finder.FindComponent<ParticlesAnimation>(gameObject);
            if (particleAnimations != null)
                particleAnimations.End(); 

            if (m_Background != null)
            {
                if (m_ReplacementColor != null)
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