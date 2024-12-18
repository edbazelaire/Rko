using UnityEngine;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Authentication;
using System.Collections.Generic;
using Unity.Services.Friends.Notifications;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Friends.Exceptions;
using Save;
using Tools;
using Assets.Scripts.Network;

namespace Managers.Friends
{
    public class FriendsHandler : MonoBehaviour
    {
        #region Members

        #region -- Instance & Init

        static FriendsHandler s_Instance;
        public static FriendsHandler Instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;

                s_Instance = FindAnyObjectByType<FriendsHandler>();
                if (s_Instance != null)
                    return s_Instance;

                var gameObject = new GameObject("FriendsHandler");
                s_Instance = gameObject.AddComponent<FriendsHandler>();
                return s_Instance;
            }
        }

        private bool m_Initialized = false;
        public static bool Initialized => s_Instance != null && Instance.m_Initialized;

        #endregion


        //This gameObject reference is only needed to get the IRelationshipUIController component from it.
        [Tooltip("Reference a GameObject that has a component extending from IRelationshipsUIController."), SerializeField]
        GameObject m_RelationshipsViewGameObject;

        IRelationshipsView m_RelationshipsView;

        List<FriendsEntryData> m_FriendsEntryDatas = new List<FriendsEntryData>();
        List<PlayerProfile> m_RequestsEntryDatas = new List<PlayerProfile>();
        List<PlayerProfile> m_BlockEntryDatas = new List<PlayerProfile>();

        ILocalPlayerView    m_LocalPlayerView;
        IAddFriendView      m_AddFriendView;
        IFriendsListView    m_FriendsListView;
        IRequestListView    m_RequestListView;
        IBlockedListView    m_BlockListView;

        PlayerProfile m_LoggedPlayerProfile;

        Activity m_CurrentActivity;
        private FriendsEventConnectionState m_current_state;

        public static Activity CurrentActivity => Instance.m_CurrentActivity;

        #endregion


        #region Init & End

        public async void Initialize()
        {
            Debug.Log("FriendsHandler.Initialize()");

            // Registering callbacks before Friends Initializing to ensure the receiving of all
            // `NotificationsConnectivityChanged` events.
            RegisterFriendsEventCallbacks();

            await FriendsService.Instance.InitializeAsync();

            // TODO : REMOVE WHEN FRIENDSHIP IS NO LONGER AUTOMATIC
            AcceptAllCurrentRequests();
            // TODO : REMOVE WHEN FRIENDSHIP IS NO LONGER AUTOMATIC

            //UIInit();

            await LogInAsync();

            RefreshAll();

            m_Initialized = true;
        }

        async Task LogInAsync()
        {
            var playerID = AuthenticationService.Instance.PlayerId;
            var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            m_LoggedPlayerProfile = new PlayerProfile(playerName, playerID);

            await SetPresence(Availability.Online, "Logged In");
            //m_LocalPlayerView.Refresh(
            //    m_LoggedPlayerProfile.Name,
            //    "In Friends Menu",
            //    Availability.Online);
            RefreshAll();

            Debug.Log($"Logged in as {m_LoggedPlayerProfile}");
        }


        void UIInit()
        {
            if (m_RelationshipsViewGameObject == null)
            {
                Debug.LogError($"Missing GameObject in {name}", gameObject);
                return;
            }

            m_RelationshipsView = m_RelationshipsViewGameObject.GetComponent<IRelationshipsView>();
            if (m_RelationshipsView == null)
            {
                Debug.LogError($"No Component extending IRelationshipsView {m_RelationshipsViewGameObject.name}", m_RelationshipsViewGameObject);
                return;
            }

            m_RelationshipsView.Init();
            m_LocalPlayerView   = m_RelationshipsView.LocalPlayerView;
            m_AddFriendView     = m_RelationshipsView.AddFriendView;

            //Bind Lists
            m_FriendsListView   = m_RelationshipsView.FriendsListView;
            m_FriendsListView.BindList(m_FriendsEntryDatas);
            m_RequestListView   = m_RelationshipsView.RequestListView;
            m_RequestListView.BindList(m_RequestsEntryDatas);
            m_BlockListView     = m_RelationshipsView.BlockListView;
            m_BlockListView.BindList(m_BlockEntryDatas);

            // Bind Friends SDK Callbacks
            m_AddFriendView.onFriendRequestSent     += AddFriendAsync;
            m_FriendsListView.onRemove              += RemoveFriendAsync;
            m_FriendsListView.onBlock               += BlockFriendAsync;
            m_RequestListView.onAccept              += AcceptRequestAsync;
            m_RequestListView.onDecline             += DeclineRequestAsync;
            m_RequestListView.onBlock               += BlockFriendAsync;
            m_BlockListView.onUnblock               += UnblockFriendAsync;
            m_LocalPlayerView.onPresenceChanged     += SetPresenceAsync;
        }

        #endregion


        #region Manipulators

        void RefreshAll()
        {
            RefreshFriends();
            RefreshRequests();
            RefreshBlocks();
        }

        public static async void AcceptAllCurrentRequests()
        {
            List<Member> requests = Instance.GetRequests();
            foreach (Member member in requests)
            {
                await Instance.AcceptRequest(member.Profile.Name);
            }
        }

        /// <summary>
        /// Send a FriendRequest to every player from the same region
        /// </summary>
        public static async void SendFriendRequestToAll()
        {
            // get all valid pseudos
            List<SPublicProfileData> allPlayers = await ProfileCloudData.GetAllPlayersPublicData(validOnly: true);
            foreach (SPublicProfileData playerData in allPlayers)
            {
                // get only player from the same region
                if (RelayHandler.TrimRegion(playerData.Region) != RelayHandler.TrimRegion(ProfileCloudData.Region))
                    continue;

                // ignore self pseudo
                if (playerData.Pseudo == ProfileCloudData.GamerTag)
                    continue;

                if (FriendsHandler.HasFriend(playerData.PlayerName))
                    continue;

                bool success = await Instance.SendFriendRequest(playerData.PlayerName);
                if (!success)
                {
                    ErrorHandler.Warning("Unable to add " + playerData.PlayerName + " as friend");
                }
            }
        }

        public static void RemoveAllCurrentFriends()
        {
            var friends = Instance.GetFriends();
            foreach (var member in friends)
            {
                Instance.RemoveFriendAsync(member.Id);
            }
        }

        public static bool HasFriend(string name)
        {
            var friends = Instance.GetFriends();
            foreach (Member friend in friends)
            {
                if (friend.Profile.Name == name)
                    return true;
            }

            return false;
        }

        async void BlockFriendAsync(string id)
        {
            await BlockFriend(id);
            RefreshAll();
        }

        async void UnblockFriendAsync(string id)
        {
            await UnblockFriend(id);
            RefreshBlocks();
            RefreshFriends();
        }

        async void RemoveFriendAsync(string id)
        {
            await RemoveFriend(id);
            RefreshFriends();
        }

        async void AcceptRequestAsync(string name)
        {
            await AcceptRequest(name);
            RefreshRequests();
            RefreshFriends();
        }

        async void DeclineRequestAsync(string id)
        {
            await DeclineRequest(id);
            RefreshRequests();
        }

        async void SetPresenceAsync((Availability presence, string activity) status)
        {
            await SetPresence(status.presence, status.activity);
            //m_LocalPlayerView.Refresh(m_LoggedPlayerProfile.Name, status.activity, status.presence);
        }

        async void AddFriendAsync(string name)
        {
            var success = await SendFriendRequest(name);
            //if (success)
            //{
            //    m_AddFriendView.FriendRequestSuccess();
            //    //If the added friend has also requested friendship, he is already a friend, just refresh the views.
            //    if (m_RequestsEntryDatas.Find(entry => entry.Name == name) != null)
            //        RefreshAll();
            //}
            //else
            //{
            //    m_AddFriendView.FriendRequestFailed();
            //}
        }

        void RefreshFriends()
        {
            m_FriendsEntryDatas.Clear();

            var friends = GetFriends();

            foreach (var friend in friends)
            {
                string activityText;
                if (friend.Presence.Availability == Availability.Offline ||
                    friend.Presence.Availability == Availability.Invisible)
                {
                    activityText = friend.Presence.LastSeen.ToShortDateString() + " " +
                                   friend.Presence.LastSeen.ToLongTimeString();
                }
                else
                {
                    activityText = friend.Presence.GetActivity<Activity>() == null
                        ? ""
                        : friend.Presence.GetActivity<Activity>().Status;
                }

                var info = new FriendsEntryData
                {
                    Name = friend.Profile.Name,
                    Id = friend.Id,
                    Availability = friend.Presence.Availability,
                    Activity = activityText
                };
                m_FriendsEntryDatas.Add(info);
            }

            //m_RelationshipsView.RelationshipBarView.Refresh();
        }

        void RefreshRequests()
        {
            m_RequestsEntryDatas.Clear();
            var requests = GetRequests();

            foreach (var request in requests)
                m_RequestsEntryDatas.Add(new PlayerProfile(request.Profile.Name, request.Id));

            //m_RelationshipsView.RelationshipBarView.Refresh();
        }

        void RefreshBlocks()
        {
            m_BlockEntryDatas.Clear();

            foreach (var block in FriendsService.Instance.Blocks)
                m_BlockEntryDatas.Add(new PlayerProfile(block.Member.Profile.Name, block.Member.Id));

            //m_RelationshipsView.RelationshipBarView.Refresh();
        }

        public async Task<bool> SendFriendRequest(string playerName)
        {
            try
            {
                //We add the friend by name in this sample but you can also add a friend by ID using AddFriendAsync
                var relationship = await FriendsService.Instance.AddFriendByNameAsync(playerName);
                Debug.Log($"Friend request sent to {playerName}.");
                //If both players send friend request to each other, their relationship is changed to Friend.
                return relationship.Type is RelationshipType.FriendRequest or RelationshipType.Friend;
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to Request {playerName} - {e}.");
                return false;
            }
        }

        async Task RemoveFriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteFriendAsync(playerId);
                Debug.Log($"{playerId} was removed from the friends list.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to remove {playerId}. - {e}");
            }
        }

        async Task BlockFriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.AddBlockAsync(playerId);
                Debug.Log($"{playerId} was blocked.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to block {playerId}. - {e}");
            }
        }

        async Task UnblockFriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteBlockAsync(playerId);
                Debug.Log($"{playerId} was unblocked.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to unblock {playerId} - {e}.");
            }
        }

        async Task AcceptRequest(string playerName)
        {
            try
            {
                await SendFriendRequest(playerName);
                Debug.Log($"Friend request from {playerName} was accepted.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to accept request from {playerName}. - {e}");
            }
        }

        async Task DeclineRequest(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteIncomingFriendRequestAsync(playerId);
                Debug.Log($"Friend request from {playerId} was declined.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to decline request from {playerId}. - {e}");
            }
        }

        /// <summary>
        /// Get an amount of friends (including presence data).
        /// </summary>
        /// <returns>List of friends.</returns>
        public List<Member> GetFriends()
        {
            return GetNonBlockedMembers(FriendsService.Instance.Friends);
        }

        /// <summary>
        /// Get an amount of Requests. The friends SDK maintains relationships unless explicitly deleted, even those
        /// towards blocked players. We don't want to show blocked players' requests, so we filter them out.
        /// </summary>
        /// <returns>List of players.</returns>
        public List<Member> GetRequests()
        {
            return GetNonBlockedMembers(FriendsService.Instance.IncomingFriendRequests);
        }

        public async Task SetPresence(Availability presenceAvailabilityOptions,
            string activityStatus = "")
        {
            var activity = new Activity { Status = activityStatus };
            try
            {
                await FriendsService.Instance.SetPresenceAsync(presenceAvailabilityOptions, activity);
                m_CurrentActivity = activity;
                Debug.Log($"Availability changed to {presenceAvailabilityOptions}.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to set the presence to {presenceAvailabilityOptions} - {e}");
            }
        }

        /// <summary>
        /// Returns a list of members that are not blocked by the active user.
        /// </summary>
        /// <param name="relationships">The list of relationships to filter.</param>
        /// <returns>Filtered list of members.</returns>
        private List<Member> GetNonBlockedMembers(IReadOnlyList<Relationship> relationships)
        {
            var blocks = FriendsService.Instance.Blocks;
            return relationships
                   .Where(relationship =>
                       !blocks.Any(blockedRelationship => blockedRelationship.Member.Id == relationship.Member.Id))
                   .Select(relationship => relationship.Member)
                   .ToList();
        }

        #endregion


        #region Listeners

        void RegisterFriendsEventCallbacks()
        {
            try
            {
                FriendsService.Instance.RelationshipAdded += e =>
                {
                    RefreshRequests();
                    RefreshFriends();
                    Debug.Log($"create {e.Relationship} EventReceived");
                };

                FriendsService.Instance.MessageReceived += e =>
                {
                    RefreshRequests();
                    Debug.Log("MessageReceived EventReceived");
                };

                FriendsService.Instance.PresenceUpdated += e =>
                {
                    RefreshFriends();
                    Debug.Log("PresenceUpdated EventReceived");
                };

                FriendsService.Instance.RelationshipDeleted += e =>
                {
                    RefreshFriends();
                    Debug.Log($"Delete {e.Relationship} EventReceived");
                };

                //FriendsService.Instance.NotificationsConnectivityChanged += e =>
                //{
                //    if (m_current_state == FriendsEventConnectionState.Subscribed && m_current_state != e.State)
                //    {
                //        m_LocalPlayerView.Refresh(m_LoggedPlayerProfile.Name,
                //            "Connectivity Problems",
                //            Availability.Offline);
                //    }

                //    if (m_current_state == FriendsEventConnectionState.Unsynced &&
                //        e.State == FriendsEventConnectionState.Subscribed)
                //    {
                //        SetPresenceAsync((Availability.Online, "Back Online"));
                //    }
                //    Debug.Log($"Change of state in notification system from {m_current_state} to {e.State}");
                //    m_current_state = e.State;
                //};
            }
            catch (FriendsServiceException e)
            {
                Debug.Log("An error occurred while performing the action. HttpCode: " + e.StatusCode + ", FriendsErrorCode: " + e.ErrorCode + ", Message: " + e.Message);
            }
        }

        #endregion
    }

}