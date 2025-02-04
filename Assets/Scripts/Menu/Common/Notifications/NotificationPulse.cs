using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Notifications
{
    public enum ENotificationPosition
    {
        None = 0,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }


    public class NotificationPulse : MObject, INotification
    {
        #region Members

        /// <summary> Game object that will receive the Pulse Animation </summary>
        GameObject m_AnimationTarget;
        /// <summary> Game object that will hold the RedDot </summary>
        GameObject m_RedDotTarget; 

        /// <summary> animation red dot </summary>
        RedDot m_RedDot;
        /// <summary> size of the red dot </summary>
        float m_Size = 1;
        /// <summary> counter of the red dot </summary>
        int m_Counter = 0;

        bool                m_IsActivated;
        OvAnimation         m_Animation;

        public RedDot RedDot => m_RedDot;

        #endregion


        #region Init & End

        public static NotificationPulse Add(GameObject baseGameObject, GameObject animationTarget = null, GameObject redDotTarget = null, int counter = 0, float size = 1)
        {
            if (animationTarget == null)
            {
                ErrorHandler.Error("no target for animation was provided");
                return null;
            }

            if (! baseGameObject.TryGetComponent<NotificationPulse>(out var notificationDisplay))
                notificationDisplay = baseGameObject.AddComponent<NotificationPulse>();

            notificationDisplay.Initialize(animationTarget, redDotTarget, counter, size);
            notificationDisplay.Activate();

            return notificationDisplay;
        }

        public static void Remove(GameObject gameObject)
        {
            if (! gameObject.TryGetComponent<NotificationPulse>(out var notificationDisplay))
                return;

            notificationDisplay.Deactivate();
            Destroy(notificationDisplay);
        }

        public static void UpdateCounter(GameObject gameObject, int counter)
        {
            if (!gameObject.TryGetComponent<NotificationPulse>(out var notificationDisplay))
            {
                ErrorHandler.Error("Trying to update counter of " + gameObject.name + " but no NotificationPulse was found on the object - adding new one");
                Add(gameObject, gameObject, gameObject, counter);
                return;
            }

            if (notificationDisplay.RedDot == null)
            {
                ErrorHandler.Error("Trying to update RedDot on " + gameObject.name + " but no redDot found in the gameObject - adding new one");
                notificationDisplay.AddRedDot(gameObject, counter);
                return;
            }

            notificationDisplay.RedDot.UpdateCounter(counter);
        }

        protected void Initialize(GameObject animationTarget, GameObject redDotTarget, int counter = 0, float size = 1)
        {
            m_AnimationTarget = animationTarget;
            m_RedDotTarget = redDotTarget;

            m_Counter = counter;
            m_Size = size;
        }

        #endregion


        #region Activation

        public void Activate()
        {
            if (m_IsActivated)
                return;

            m_IsActivated = true;

            if (m_RedDotTarget != null)
                AddRedDot(m_RedDotTarget, m_Counter);

            if (m_AnimationTarget != null)
                m_Animation = m_AnimationTarget.AddComponent<Pulse>();
        }

        public void Deactivate()
        {
            m_IsActivated = false;

            // Set UI Locked
            if (m_Animation != null)
                m_Animation.End();

            // Remove red dot
            if (m_RedDot != null)
                Destroy(m_RedDot.gameObject);
        }

        public void End()
        {
            Deactivate();

            Destroy(this);
        }

        #endregion


        #region Red Dot

        public void AddRedDot(GameObject redDotTarget, int counter = 0)
        {
            m_RedDotTarget = redDotTarget;
            m_Counter = counter;
            m_RedDot = RedDot.AddRedDot(redDotTarget, counter, m_Size);
        }

        #endregion
    }
}