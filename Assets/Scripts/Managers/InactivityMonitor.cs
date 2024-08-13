using Managers.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;

namespace Managers
{
    public class InactivityMonitor : MonoBehaviour
    {
        #region Members

        [SerializeField]
        private float m_InactivityThreshold = 30f; // Time in seconds before considering the player inactive (e.g., 10 minutes)

        private float m_LastInputTime;
        private bool m_IsPlayerInactive;

        #endregion


        #region Inherited Methods

        void Start()
        {
            // Initialize the last input time to the current time when the game starts
            m_LastInputTime = Time.time;
            m_IsPlayerInactive = false;
        }

        void Update()
        {
            // Check for any input and reset the last input time if detected
            if (Input.anyKey || Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                m_LastInputTime = Time.time;

                if (m_IsPlayerInactive)
                    OnPlayerActivity();

                return;
            }

            // already inactive : exit
            if (m_IsPlayerInactive)
                return;

            // Check if the inactivity threshold has been exceeded
            if (Time.time - m_LastInputTime > m_InactivityThreshold)
            {
                m_IsPlayerInactive = true;
                OnPlayerInactivity();
            }
        }

        #endregion


        #region Activity Management

        private async void OnPlayerActivity()
        {
            Debug.Log("OnPlayerActivity()");
            m_IsPlayerInactive = false;
            await FriendsHandler.Instance.SetPresence(Availability.Online);
        }

        private async void OnPlayerInactivity()
        {
            Debug.Log("OnPlayerInactivity()");
            await FriendsHandler.Instance.SetPresence(Availability.Away);
        }

        #endregion

    }
}