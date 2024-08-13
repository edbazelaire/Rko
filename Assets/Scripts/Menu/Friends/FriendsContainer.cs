using Managers.Friends;
using System.Collections.Generic;
using TMPro;
using Tools;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using UnityEngine;


namespace Menu.HUD
{
    public class FriendsContainer : MObject
    {
        #region Members

        [SerializeField, Tooltip("Refresh the friend counters each N seconds to prevent missed event from displaying the correct number of connections")]
        int m_RefreshRate = 60;

        // =============================================================================================
        // Data
        float m_UpdateTimer = 0f;

        // =============================================================================================
        // GameObjects & Components
        TMP_Text m_OnlineCounter;
        TMP_Text m_BusyCounter;
        TMP_Text m_AwayCounter;
        TMP_Text m_OfflineCounter;

        #endregion


        #region Init & End

        void Awake()
        {
            Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();
            m_OnlineCounter     = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "OnlineAvailabilityCounter"),  "Counter");
            m_BusyCounter       = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "BusyAvailabilityCounter"),    "Counter");
            m_AwayCounter       = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "AwayAvailabilityCounter"),    "Counter");
            m_OfflineCounter    = Finder.FindComponent<TMP_Text>(Finder.Find(gameObject, "OfflineAvailabilityCounter"), "Counter");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            RefreshUI();
        }

        #endregion


        #region GUI Manipulators

        void Update() 
        {
            m_UpdateTimer -= Time.deltaTime;
            if (m_UpdateTimer < 0f)
                RefreshUI();
        }

        void RefreshUI()
        {
            // refresh timer
            m_UpdateTimer = m_RefreshRate;

            Dictionary<Availability, int> nFriends = new()
            {
                { Availability.Online,  0 },
                { Availability.Busy,    0 },
                { Availability.Away,    0 },
                { Availability.Offline, 0 },
            };
            
            var friends = FriendsHandler.Instance.GetFriends();
            foreach (var friend in friends)
            {
                switch (friend.Presence.Availability)
                {
                    case Availability.Online:
                    case Availability.Busy:
                    case Availability.Away:
                        nFriends[friend.Presence.Availability]++;
                        break;

                    default:
                        nFriends[Availability.Offline]++;
                        break;
                }
                
            }

            m_OnlineCounter.text    = nFriends[Availability.Online].ToString();
            m_BusyCounter.text      = nFriends[Availability.Busy].ToString();
            m_AwayCounter.text      = nFriends[Availability.Away].ToString();
            m_OfflineCounter.text   = nFriends[Availability.Offline].ToString();   
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            try
            {
                FriendsService.Instance.RelationshipAdded   += OnRelationshipAdded;
                FriendsService.Instance.MessageReceived     += OnMessageReceived;
                FriendsService.Instance.PresenceUpdated     += OnPresenceUpdated;
                FriendsService.Instance.RelationshipDeleted += RelationshipDeleted;
            }
            catch (FriendsServiceException e)
            {
                Debug.Log("An error occurred while performing the action. HttpCode: " + e.StatusCode + ", FriendsErrorCode: " + e.ErrorCode + ", Message: " + e.Message);
            }
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            FriendsService.Instance.RelationshipAdded       -= OnRelationshipAdded;
            FriendsService.Instance.MessageReceived         -= OnMessageReceived;
            FriendsService.Instance.PresenceUpdated         -= OnPresenceUpdated;
            FriendsService.Instance.RelationshipDeleted     -= RelationshipDeleted;
        }

        void OnRelationshipAdded(IRelationshipAddedEvent e)
        {
            RefreshUI();
        }

        void OnMessageReceived(IMessageReceivedEvent e)
        {
            Debug.Log("MessageReceived EventReceived from : " + e.UserId);
        }

        void OnPresenceUpdated(IPresenceUpdatedEvent e)
        {
            RefreshUI();
            Debug.Log("PresenceUpdated EventReceived ("+e.ID+") : " + e.Presence);
        }

        void RelationshipDeleted(IRelationshipDeletedEvent e)
        {
            RefreshUI();
            Debug.Log($"Delete {e.Relationship} EventReceived");
        }

        #endregion
    }
}
