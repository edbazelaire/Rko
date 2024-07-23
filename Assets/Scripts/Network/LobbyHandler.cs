using Assets;
using Assets.Scripts.Network;
using Assets.Scripts.Tools;
using Data.GameManagement;
using Enums;
using Game;
using Managers;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tools;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Network
{
    public enum ELobbyState
    {
        Inactive,
        Joining,
        WaitingLobbyFull,
        SceneLoading,
        WaitingRelayCode,
        JoiningRelay,
        WaitingGameManager,
        SendingPlayerData,
        Ready,
    }

    public class LobbyHandler : MonoBehaviour
    {
        #region Members

        static LobbyHandler s_Instance;
        public static LobbyHandler Instance { 
            get 
            { 
                if (s_Instance != null) 
                    return s_Instance;

                var gameObject = new GameObject("LobbyHandler");
                gameObject.AddComponent<LobbyHandler>();
                s_Instance = gameObject.GetComponent<LobbyHandler>();
                s_Instance.Initialize();
                return s_Instance;
            } 
        }

        // Error management
        const string LOBBY_TIME_WRAPPER_ID  = "Lobby";
        const float LOBBY_ERROR_TIMER       = 15f;

        // Update & Heartbeat management
        const string KEY_GAME_MODE          = "GameMode";
        const string KEY_RELAY_CODE         = "RelayCode";
        const float HEARTBEAT_TIMER         = 15f;
        const float UPDATE_LOBBY_TIMER      = 1.5f;

        public Action<ulong, ECharacter> OnRelayJoined;

        private ELobbyState m_State;
        private Lobby       m_HostLobby;
        private Lobby       m_JoinedLobby;
        private string      m_RelayCode;

        private EGameMode m_GameMode = EGameMode.Arena;
        private EArenaType m_ArenaType = EArenaType.FireArena;

        private float m_HeartbeatTimer    = 0.0f;
        private float m_UpdateLobbyTimer  = 0.0f;

        bool m_RequestInProgress = false;
        private Coroutine m_CurrentCoroutine;

        bool IsHost => m_HostLobby != null && m_JoinedLobby != null && m_HostLobby.Id == m_JoinedLobby.Id;
        int m_MaxPlayers => m_GameMode == EGameMode.Ranked ? 2 : 1;

        public EGameMode        GameMode    { get => m_GameMode; set => m_GameMode = value; }
        public EArenaType       ArenaType   { get => m_ArenaType; set => m_ArenaType = value; }
        public int              NPlayers    => m_JoinedLobby != null ? m_JoinedLobby.Players.Count : 0;
        public int              MaxPlayers  => m_MaxPlayers;
        public ELobbyState      State       => m_State;

        #endregion


        #region Initialize & End

        private void Initialize()
        {
            PlayerPrefsHandler.GameModeChangedEvent     += OnGameModeChanged;
            PlayerPrefsHandler.ArenaTypeChangedEvent    += OnArenaTypeChanged;

            m_GameMode      = PlayerPrefsHandler.GetGameMode();
            m_ArenaType     = PlayerPrefsHandler.GetArenaType();

            DontDestroyOnLoad(gameObject);
        }

        
        /// <summary>
        /// Leave current lobby and reset parameters
        /// </summary>
        void ResetLobby()
        {
            m_HostLobby = null;
            m_JoinedLobby = null;
            m_RelayCode = "";

            StopAllCoroutines();

            m_CurrentCoroutine = null;

            SetState(ELobbyState.Inactive);
        }

        #endregion


        #region Update Methods

        void Update()
        {
            //HandleLobbyState();

            if (m_State == ELobbyState.Inactive)
                return;

            if (m_RequestInProgress)
                return;

            // send heartbeat to maintain the lobby alive
            HandleLobbyHeartbeat();
        }

        /// <summary>
        /// Check lobby data updates
        /// </summary>
        async void HandleLobbyState()
        {
            try
            {
                switch (m_State)
                {
                    case ELobbyState.Inactive:
                        return;

                    case ELobbyState.Joining:
                        if (m_JoinedLobby == null)
                            return;

                        SetRequestInProgress();

                        m_JoinedLobby = await LobbyService.Instance.GetLobbyAsync(m_JoinedLobby.Id);

                        NextState();
                        return;

                    case ELobbyState.WaitingLobbyFull:
                        UpdateLobbyData();

                        m_CurrentCoroutine = StartCoroutine(WaitLobbyFullCoroutine());
                        return;

                    case ELobbyState.SceneLoading:
                        await SceneLoader.Instance.SceneLoadingAsync();

                        if (!IsHost)
                        {
                            NextState();
                            return;
                        }

                        // Host creates the relay and instantly go to state "SendingPlayerData"
                        SetRequestInProgress();

                        await Retry(CreateRelay);

                        UpdateLobbyRelayCode(m_RelayCode);

                        // spawn the GameManager on Server
                        m_CurrentCoroutine = StartCoroutine(WaitGameManagerCoroutine());
                        return;

                    case ELobbyState.WaitingRelayCode:
                        m_CurrentCoroutine = StartCoroutine(WaitRelayCodeCoroutine());
                        return;

                    case ELobbyState.JoiningRelay:
                        SetRequestInProgress();

                        await Retry(JoinRelay);

                        NextState();
                        return;

                    case ELobbyState.WaitingGameManager:
                        m_CurrentCoroutine = StartCoroutine(WaitGameManagerCoroutine());
                        return;

                    case ELobbyState.SendingPlayerData:
                        GameManager.Instance.AddPlayerDataServerRPC(
                            NetworkManager.Singleton.LocalClientId,
                            StaticPlayerData.ToStruct()
                        );

                        SendBotsData();

                        NextState();
                        return;

                    case ELobbyState.Ready:
                        TimeErrorWrapper.Instance.Cancel(LOBBY_TIME_WRAPPER_ID);
                        return;


                    default:
                        OnErrorCallback("Unhandled LobbyState : " + m_State).Invoke();
                        return;
                }
            } 
            catch (Exception e)
            {
                OnErrorCallback("Unhandled LobbyState : " + m_State, e.Message).Invoke();
            }
        }

        IEnumerator WaitLobbyFullCoroutine()
        {
            while (m_JoinedLobby.Players.Count != m_JoinedLobby.MaxPlayers)
            {
                UpdateLobbyData();
                yield return null;
            }

            SceneLoader.Instance.LoadScene("Arena");
            NextState();
        }

        IEnumerator WaitGameManagerCoroutine()
        {
            while (!GameManager.FindInstance(true))
                yield return null;

            SetState(ELobbyState.SendingPlayerData);
        }

        IEnumerator WaitRelayCodeCoroutine()
        {
            // if relay code not provided yet : return
            while (m_JoinedLobby.Data[KEY_RELAY_CODE].Value == "")
            {
                UpdateLobbyData();
                yield return null;
            }

            NextState();
        }

        /// <summary>
        /// Send Heartbeat to the lobby to maintain it alive
        /// </summary>
        async void HandleLobbyHeartbeat()
        {
            if (m_HostLobby == null)
                return;

            m_HeartbeatTimer -= Time.deltaTime;
            if (m_HeartbeatTimer > 0.0f)
                return;

            // reset heatbeat
            m_HeartbeatTimer = HEARTBEAT_TIMER;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(m_HostLobby.Id);

            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning(e);
                m_HeartbeatTimer = 0f;        // set timer back to 0 to send an other one
            }


        }

        async void UpdateLobbyData()
        {
            if (m_JoinedLobby == null)
                return;

            if (m_RequestInProgress)
                return;

            m_UpdateLobbyTimer -= Time.deltaTime;
            if (m_UpdateLobbyTimer > 0.0f)
                return;

            m_UpdateLobbyTimer = UPDATE_LOBBY_TIMER;

            SetRequestInProgress();

            var task = new Func<Task>(async () => m_JoinedLobby =  await LobbyService.Instance.GetLobbyAsync(m_JoinedLobby.Id));
            await Retry(task);

            SetRequestInProgress(false);
        }

        #endregion


        #region Join & Exit Lobby

        public async Task<bool> QuickJoinLobby()
        {
            Main.SetState(EAppState.Lobby);

            bool success = await JoinLobby();

            if (!success)
                success = await CreateLobby();

            if (!success)
            {
                OnErrorCallback("Failed to join or create lobby").Invoke();
                return false;
            }

            SetState(ELobbyState.Joining);

            return success;
        }

        public async Task<bool> CreateLobby()
        {
            try
            {
                string lobbyName = "Lobby_" + UnityEngine.Random.Range(0, 99);

                CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = new Player
                    {
                        Data = StaticPlayerData.ToPlayerDataObject()
                    },
                    Data = new Dictionary<string, DataObject> {
                        { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, m_GameMode.ToString(), DataObject.IndexOptions.S1) },
                        { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, "", DataObject.IndexOptions.S2) }
                    }
                };

                m_HostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, m_MaxPlayers, createLobbyOptions);
                m_JoinedLobby = m_HostLobby;

                Debug.Log("Lobby created: " + m_HostLobby.Id);

                return true;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log("Failed to create lobby: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Join the first lobby found
        /// </summary>
        public async Task<bool> JoinLobby()
        {
            try
            {
                var lobbies = await ListLobbies();
                if (lobbies.Count == 0)
                    return false;

                JoinLobbyByIdOptions lobbyOptions = new JoinLobbyByIdOptions
                {
                    Player = new Player
                    {
                        Data = StaticPlayerData.ToPlayerDataObject()
                    }
                };

                m_JoinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbies[0].Id, lobbyOptions);
                return true;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log("Failed to join lobby: " + e.Message);
            }

            return false;
        }
        
        /// <summary>
        /// Leave the current joined lobby
        /// </summary>
        public async void LeaveLobby()
        {
            if (LobbyService.Instance != null && m_JoinedLobby != null)
                await LobbyService.Instance.RemovePlayerAsync(m_JoinedLobby.Id, AuthenticationService.Instance.PlayerId);

            ResetLobby();

            ErrorHandler.Log("Lobby left", ELogTag.Lobby);
        }

        /// <summary>
        /// Delete lobby from Host
        /// </summary>
        async void DeleteHostLobby()
        {
            if (m_HostLobby == null)
                return;

            try
            {
                await Lobbies.Instance.DeleteLobbyAsync(m_HostLobby.Id);
                m_HostLobby = null;
            }
            catch (LobbyServiceException e)
            {
                ErrorHandler.Error("Failed to delete lobby: " + e.Message);
            }
        }

        #endregion


        #region Relay

        async Task CreateRelay()
        {
            m_RelayCode = await RelayHandler.Instance.CreateRelay();
        }

        async Task JoinRelay()
        {
            await RelayHandler.Instance.JoinRelay(m_JoinedLobby.Data[KEY_RELAY_CODE].Value);
        }

        #endregion


        #region Send Data

        void SendBotsData()
        {
            // no AI in game mode (for now)
            if (GameMode == EGameMode.Ranked)
                return;

            GameManager.Instance.AddPlayerDataServerRPC(
                GameManager.BOT_CLIENT_ID,
                GetAIPlayerData()
            );
        }

        /// <summary>
        /// Convert AI data into player data
        /// </summary>
        /// <returns></returns>
        SPlayerData GetAIPlayerData()
        {
            switch (GameMode)
            {
                // ================================================================================================
                // ARENA MODE : based on current stage values
                case EGameMode.Arena:
                    ArenaData arenaData = AssetLoader.LoadArenaData(LobbyHandler.Instance.ArenaType);
                    return arenaData.CreatePlayerData();

                // ================================================================================================
                // TRAINING MODE : based on provided one in the Training tab
                case EGameMode.Training:
                    ECharacter trainingCharacter = PlayerPrefsHandler.GetString<ECharacter>(EPlayerPref.TrainingCharacter);

                    return new SPlayerData(
                        trainingCharacter.ToString(),
                        9,
                        trainingCharacter,
                        PlayerPrefsHandler.GetString<ERune>(EPlayerPref.TrainingRune),
                        PlayerPrefsHandler.GetTrainingSpells(),
                        new int[] { 9, 9, 9, 9 },
                        new SProfileCurrentData(gamerTag: trainingCharacter.ToString()).AsNetworkSerializable(),
                        isPlayer: false,
                        botData: new SBotData(PlayerPrefs.GetFloat(EPlayerPref.TrainingDecisionRefresh.ToString(), 0.05f), PlayerPrefs.GetFloat(EPlayerPref.TrainingRandomness.ToString(), 0f))
                    );

                // ================================================================================================
                // RANKED MODE : random
                case EGameMode.Ranked:
                    ECharacter character = ECharacter.Alexander;
                  
                    return new SPlayerData(
                        character.ToString(),
                        1,
                        character,
                        ERune.None,
                        new ESpell[] { ESpell.Heal, ESpell.RockShower },
                        new int[] { 1, 1 },
                        new SProfileCurrentData(
                            gamerTag: character.ToString()
                        ).AsNetworkSerializable(),
                        isPlayer: false
                    );

                // ================================================================================================
                default:
                    ErrorHandler.Error("Unhandled mode : " + GameMode);
                    return default;
            }
        }

        #endregion


        #region State Methods

        void SetState(ELobbyState state)
        {
            ErrorHandler.Log("NEW LOBBY STATE : " + state, ELogTag.Lobby);

            // if state changes : reset request in progress
            SetRequestInProgress(false);

            // set new state
            m_State = state;

            if (m_GameMode != EGameMode.Ranked)
            {
                if (m_State == ELobbyState.Ready || m_State == ELobbyState.Inactive)
                    TimeErrorWrapper.Instance.Cancel(LOBBY_TIME_WRAPPER_ID);
                else
                    TimeErrorWrapper.Instance.New(LOBBY_TIME_WRAPPER_ID, LOBBY_ERROR_TIMER, OnErrorCallback());
            }

            HandleLobbyState();
        }

        void NextState()
        {
            SetState(m_State + 1);
        }

        void SetRequestInProgress(bool inProgress = true) 
        {
            // reset timers on reseting request in progress (to avoid spam the server)
            if (m_RequestInProgress && inProgress == false)
            {
                m_HeartbeatTimer = HEARTBEAT_TIMER;
                m_UpdateLobbyTimer = UPDATE_LOBBY_TIMER;
            }

            m_RequestInProgress = inProgress; 
        }

        #endregion


        #region Update Lobby Data Methods 

        async void UpdateLobbyRelayCode(string relayCode)
        {
            try
            {
                m_HostLobby = await Lobbies.Instance.UpdateLobbyAsync(
                    m_HostLobby.Id, 
                    new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject> {
                            { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                        }
                    }
                );
            } catch (LobbyServiceException e)
            {
                Debug.Log("Failed to update lobby: " + e.Message);
            }
        }

        async void UpdateLobbyGameMode(string gameMode)
        {
            try
            {
                m_HostLobby = await Lobbies.Instance.UpdateLobbyAsync(
                    m_HostLobby.Id, 
                    new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject> {
                            { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
                        }
                    }
                );
            } catch (LobbyServiceException e)
            {
                Debug.Log("Failed to update lobby: " + e.Message);
            }
        }

        async void UpdatePlayerData(string playerName = "", ECharacter character = ECharacter.Count)
        {
            var data = new Dictionary<string, PlayerDataObject>();
            if (playerName != "")
                data.Add(StaticPlayerData.KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName));
            if ( character != ECharacter.Count )
                data.Add(StaticPlayerData.KEY_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ((int)character).ToString()));

            try
            {
                await LobbyService.Instance.UpdatePlayerAsync(
                    m_JoinedLobby.Id, 
                    AuthenticationService.Instance.PlayerId, 
                    new UpdatePlayerOptions { Data = data }
                );
            } catch (LobbyServiceException e)
            {
                ErrorHandler.Error("Failed to update player name: " + e.Message);
            }
        }

        #endregion


        #region Helpers

        async Task<bool> Retry(Func<Task> method, int nTimes = 3)
        {
            try
            {
                await method();
            }
            catch (Exception e)
            {
                nTimes--;

                if (nTimes > 0)
                {
                    ErrorHandler.Error(m_State + " (try left " + nTimes + ") : " + e.Message);
                    return await Retry(method, nTimes);
                } 
                
                ErrorHandler.Error(m_State + " : unable to get throught this stage - returning on the MainMenu");
                OnErrorCallback("too many retries", e.Message)?.Invoke();
                return false;
            }

            return true;
        }

        #endregion


        #region Dependent Accessors

        Dictionary<string, PlayerDataObject> m_PlayerData
        {
            get
            {
                foreach (var player in m_JoinedLobby.Players)
                {
                    if (player.Id == AuthenticationService.Instance.PlayerId)
                    {
                        return player.Data;
                    }
                }

                return null;
            }
        }

        #endregion


        #region Tools Methods
        public async Task<List<Lobby>> ListLobbies()
        {
            try
            {
                QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter> {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.S1, m_GameMode.ToString(), QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder> {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };

                QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

                ErrorHandler.Log("Lobbies found: " + queryResponse.Results.Count, ELogTag.Lobby);

                foreach (var lobby in queryResponse.Results)
                {
                    ErrorHandler.Log("Lobby: " + lobby.Name + " - MaxPlayers : " + lobby.MaxPlayers, ELogTag.Lobby);
                }

                return queryResponse.Results;

            }
            catch (LobbyServiceException e)
            {
                ErrorHandler.Error("Failed to list lobbies: " + e.Message);
                return new List<Lobby>();
            }
        }

        /// <summary>
        /// Change Host of the Lobby
        /// </summary>
        async void MigradeLobbyHost()
        {
            try
            {
                m_HostLobby = await Lobbies.Instance.UpdateLobbyAsync(m_HostLobby.Id, new UpdateLobbyOptions
                {
                    HostId = m_JoinedLobby.Players[1].Id
                });
            }
            catch (LobbyServiceException e)
            {
                ErrorHandler.Error("Failed to migrate lobby host: " + e.Message);
            }
        }

        void PrintPlayers(Lobby lobby)
        {
            Debug.Log("Players in lobby : " + lobby.Id);
            foreach (var player in lobby.Players)
            {
                Debug.Log("Player: " + player.Id + " with name " + player.Data["PlayerName"].Value);
            }
        }

        #endregion


        #region Listeners

        void OnGameModeChanged(EGameMode gameMode)
        {
            Instance.GameMode = gameMode;
        }

        void OnArenaTypeChanged(EArenaType arenaType)
        {
            Instance.ArenaType = arenaType;
        }

        Action OnErrorCallback(string reason = "", string exceptionMessage = "")
        {
            return () =>
            {
                string message =
                "An error has occured while creating " + m_GameMode + " game mode : "
                    + "\n   + Lobby State : " + m_State
                    + (reason.Length > 0 ? "\n   + Reason : " + reason : "")
                    + (exceptionMessage.Length > 0 ? "\n   + Exception : " + exceptionMessage : "");

                Main.AddStoredEvent(EAppState.MainMenu, () => Main.SetPopUp(EPopUpState.MessagePopUp, message));
                LeaveLobby();
                SceneLoader.Instance.LoadScene("MainMenu");
            };
        }

        #endregion
    }
}