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
using System.Linq;
using System.Threading.Tasks;
using Tools;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Services.Core;
using Managers.Bots;
using Game.AI.BehaviorTrees;

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
        CreateGameManager,
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
        const string LOBBY_TIME_WRAPPER_ID      = "Lobby";
        const float LOBBY_ERROR_TIMER           = 15f;

        // Update & Heartbeat management
        const string    KEY_GAME_MODE               = "GameMode";
        const string    KEY_REGION                  = "Region";
        const string    KEY_SUB_REGION              = "SubRegion";
        const string    KEY_RELAY_CODE              = "RelayCode";
        const float     HEARTBEAT_TIMER             = 15f;
        const float     UPDATE_LOBBY_TIMER          = 1.5f;

        public Action<ulong, ECharacter> OnRelayJoined;

        private ELobbyState m_State;
        private Lobby       m_HostLobby;
        private Lobby       m_JoinedLobby;
        private string      m_RelayCode;
        private bool        m_CancelRetry;
        private bool        m_IsTuto;
        private bool        m_FillWithBots = false;

        private EGameMode m_GameMode            = EGameMode.Arena;
        private EArenaType m_ArenaType          = EArenaType.FrostArena;

        private float m_HeartbeatTimer          = 0.0f;
        private float m_UpdateLobbyTimer        = 0.0f;

        bool m_RequestInProgress = false;
        private Coroutine m_CurrentCoroutine;

        bool IsHost => m_HostLobby != null && m_JoinedLobby != null && m_HostLobby.Id == m_JoinedLobby.Id;
        int m_MaxPlayers => m_GameMode == EGameMode.Ranked ? 2 : 1;

        public EGameMode        GameMode            { get => m_GameMode; set => m_GameMode = value; }
        public EArenaType       ArenaType           { get => m_ArenaType; set => m_ArenaType = value; }
        public string           RelayCode           => m_RelayCode;
        public int              NPlayers            => m_JoinedLobby != null ? m_JoinedLobby.Players.Count : 0;
        public int              MaxPlayers          => m_MaxPlayers;
        public ELobbyState      State               => m_State;
        public bool             IsActive            => m_State != ELobbyState.Inactive;
        public bool             IsTuto              { get => m_IsTuto; set => m_IsTuto = value; }

        /// <summary> time waiting in ranked before filling with bots </summary>
        public float WaitForPlayerDuration => Main.SkipWaitingRanked ? 0f : UnityEngine.Random.Range(1f, 5f * ((int)ProgressionCloudData.CurrentLeague + 1)); 

        #endregion


        #region Initialize

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
            m_CancelRetry = false;

            StopAllCoroutines();

            m_CurrentCoroutine = null;

            SetState(ELobbyState.Inactive);
        }

        #endregion


        #region End & Exit

        void ExitScene()
        {
            ResetLobby();
            SceneLoader.Instance.LoadScene("MainMenu");
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
                        SceneLoader.Instance.LoadScene("Arena");

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

                        // Create game manager
                        SetState(ELobbyState.CreateGameManager);
                        return;

                    case ELobbyState.WaitingRelayCode:
                        m_CurrentCoroutine = StartCoroutine(WaitRelayCodeCoroutine());
                        return;

                    case ELobbyState.JoiningRelay:
                        SetRequestInProgress();

                        await Retry(JoinRelay);

                        SetState(ELobbyState.WaitingGameManager);
                        return;

                    case ELobbyState.CreateGameManager:
                        m_CurrentCoroutine = StartCoroutine(CreateGameManager());
                        return;

                    case ELobbyState.WaitingGameManager:
                        m_CurrentCoroutine = StartCoroutine(WaitGameManagerCoroutine());
                        return;

                    case ELobbyState.SendingPlayerData:
                        var playerData = StaticPlayerData.ToStruct();

                        // TUTO : overwrite data
                        if (IsTuto)
                        {
                            playerData.BuildData.Character = CharacterBuildsCloudData.DEFAULT_CHARACTER.ToString();
                            playerData.BuildData.Spells = CharacterBuildsCloudData.DEFAULT_BUILD;
                            playerData.BuildData.Runes = new ERune[3];
                        }
                        
                        // ADD ARENA POWER UPS
                        if (GameMode == EGameMode.Arena)
                        {
                            playerData.SetPowerUps(ProgressionCloudData.CurrentArena.GetActivePowerUps());
                            // if a specific build data was locked for this run, use it. Otherwise, use current selected build
                            playerData.SetBuild(ProgressionCloudData.CurrentArena.HasBuildData() ? ProgressionCloudData.CurrentArena.BuildData : CharacterBuildsCloudData.CurrentBuild);
                        }

                        // send self data to the GameManager
                        GameManager.Instance.AddPlayerDataServerRPC(
                            NetworkManager.Singleton.LocalClientId,
                            playerData
                        );

                        // if has to fill game with bots - send bots data
                        SendBotsData();
                    
                        // go to next state
                        NextState();
                        return;

                    case ELobbyState.Ready:
                        // save id of the lobby
                        if (m_GameMode == EGameMode.Ranked)
                            PlayerPrefs.SetString(EPlayerPref.CurrentGameId.ToString(), m_JoinedLobby.Id);

                        TimeErrorWrapper.Instance.Cancel(LOBBY_TIME_WRAPPER_ID);
                        return;

                    default:
                        OnErrorCallback("Unhandled LobbyState : " + m_State).Invoke();
                        return;
                }
            } 
            catch (Exception e)
            {
                Debug.LogException(e);
                OnErrorCallback("Error at LobbyState : " + m_State, e.Message).Invoke();
            }
        }

        IEnumerator WaitLobbyFullCoroutine()
        {
            float timer = WaitForPlayerDuration;

            m_FillWithBots = false;
            while (m_JoinedLobby.Players.Count != m_JoinedLobby.MaxPlayers)
            {
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    m_FillWithBots = true;
                    break;
                }

                UpdateLobbyData();
                yield return null;
            }

            NextState();
        }

        IEnumerator CreateGameManager(int nRetry = 0)
        {
            var gameManager = Instantiate(SceneLoader.Instance.GameManager);
            Finder.FindComponent<NetworkObject>(gameManager.gameObject).Spawn();

            float timer = 15f;
            while (! gameManager.IsSpawned && timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            if (! gameManager.IsSpawned)
            {
                ErrorHandler.Warning("Unable to spawn GameManager. NRetry left : " + nRetry);
                gameManager.Shutdown();

                if (--nRetry > 0)
                    m_CurrentCoroutine = StartCoroutine(CreateGameManager(nRetry));

                yield break;
            }

            gameManager.Initialize();
            ErrorHandler.Log("CREATED : GameManager");

            // spawn the GameManager on Server
            SetState(ELobbyState.WaitingGameManager);
        }

        IEnumerator WaitGameManagerCoroutine()
        {
            // waiting for game manager to be initialized and spawned
            while (! GameManager.FindInstance(true))
                yield return null;

            // waiting for game manager to be ready to receive connections
            while (GameManager.Instance.State.Value < EGameState.WaitingForConnection)
                yield return null;

            // send my player data
            SetState(ELobbyState.SendingPlayerData);
        }

        IEnumerator WaitRelayCodeCoroutine()
        {
            // if relay code not provided yet : return
            while (m_JoinedLobby.Data[KEY_RELAY_CODE].Value == "" || m_JoinedLobby.Data[KEY_RELAY_CODE].Value == m_RelayCode)
            {
                UpdateLobbyData();
                yield return null;
            }

            m_RelayCode = m_JoinedLobby.Data[KEY_RELAY_CODE].Value;
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

            bool success = await JoinFirstLobby();

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
                        { KEY_RELAY_CODE,   new DataObject(DataObject.VisibilityOptions.Member, "", DataObject.IndexOptions.S1) },
                        { KEY_GAME_MODE,    new DataObject(DataObject.VisibilityOptions.Public, m_GameMode.ToString(), DataObject.IndexOptions.S2) },
                        { KEY_REGION,       new DataObject(DataObject.VisibilityOptions.Public, RelayHandler.TrimRegion(ProfileCloudData.Region), DataObject.IndexOptions.S3) },
                        { KEY_SUB_REGION,   new DataObject(DataObject.VisibilityOptions.Public, ProfileCloudData.Region, DataObject.IndexOptions.S4) },
                    }
                };

                m_HostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, m_MaxPlayers, createLobbyOptions);
                m_JoinedLobby = m_HostLobby;

                Debug.Log("Lobby created: " + m_HostLobby.Id);
                Debug.Log("     - Region : " + ProfileCloudData.Region);

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
        public async Task<bool> JoinFirstLobby()
        {
            try
            {
                var lobbies = await ListLobbies();
                if (lobbies.Count == 0)
                    return false;

                return await JoinLobby(lobbies[0].Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log("Failed to join lobby: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Join the first lobby found
        /// </summary>
        public async Task<bool> JoinLobby(string id)
        {
            try
            {
                JoinLobbyByIdOptions lobbyOptions = new JoinLobbyByIdOptions
                {
                    Player = new Player
                    {
                        Data = StaticPlayerData.ToPlayerDataObject()
                    }
                };

                m_JoinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(id, lobbyOptions);
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
            // stop all current coroutines
            StopAllCoroutines();
            m_CurrentCoroutine = null;

            // if has lobby : remove player from this lobby
            if (LobbyService.Instance != null && m_JoinedLobby != null)
                await LobbyService.Instance.RemovePlayerAsync(m_JoinedLobby.Id, AuthenticationService.Instance.PlayerId);

            // reset value of the lobby
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

        async Task<bool> JoinRelay()
        {
            try
            {
                await RelayHandler.Instance.JoinRelay(m_JoinedLobby.Data[KEY_RELAY_CODE].Value);
                return true;
            }

            catch (RelayServiceException e)
            {
                Debug.LogError(e.Message);
                switch(e.ErrorCode)
                {
                    // GET BACK to previous stage
                    case CommonErrorCodes.TransportError:
                    case CommonErrorCodes.TokenExpired:
                    case CommonErrorCodes.RequestRejected:
                    case CommonErrorCodes.NotFound:
                        m_CancelRetry = true;
                        SetState(ELobbyState.WaitingRelayCode);
                        return false;

                    // RETRY
                    case CommonErrorCodes.ServiceUnavailable:
                    case CommonErrorCodes.Timeout:
                    case CommonErrorCodes.ApiMissing:
                    case CommonErrorCodes.TooManyRequests:
                        return false;

                    // ERROR - exit
                    case CommonErrorCodes.Forbidden:
                    case CommonErrorCodes.InvalidRequest:
                    case CommonErrorCodes.ProjectPolicyAccessDenied:
                    case CommonErrorCodes.PlayerPolicyAccessDenied:
                    case CommonErrorCodes.Conflict:
                        OnErrorCallback("Lobby Error ("+ e.ErrorCode + ") : Unable to join relay", e.Message)?.Invoke();
                        return false;

                    default:
                        ErrorHandler.Error("Unhandled error code : " + e.ErrorCode + " - " + e.Message);
                        OnErrorCallback("Lobby Error ("+ e.ErrorCode + ") : Unable to join relay", e.Message)?.Invoke();
                        return false;
                }
            }
        }

        #endregion


        #region Send Data

        void SendBotsData()
        {
            // check if requires AI bot
            if (GameMode == EGameMode.Ranked && !m_FillWithBots)
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
                    ArenaData arenaData = AssetLoader.LoadArenaData(ProgressionCloudData.CurrentArena.ArenaType, ProgressionCloudData.CurrentArena.SArenaDifficulty);
                    return arenaData.CreatePlayerData();

                // ================================================================================================
                // TRAINING MODE : based on provided one in the Training tab
                case EGameMode.Training:
                    if (IsTuto)
                    {
                        return new SPlayerData(
                            ECharacter.Kahnan.ToString(),
                            1,
                            ECharacter.Kahnan.ToString(),
                            default,
                            new int[] { 1, 1, 1 },
                            new ESpell[] { ESpell.FireBarrage, ESpell.FireBomb },
                            new int[] { 1, 1 },
                            new SProfileCurrentData(accountLevel: 1, gamerTag: ECharacter.Kahnan.ToString()).AsNetworkSerializable(),
                            isPlayer: false,
                            botData: new SBotData(EArenaDifficulty.Normal.ToString(), 1f, 1f)
                        );
                    }

                    string trainingCharacter = PlayerPrefsHandler.GetString<ECharacter>(EPlayerPref.TrainingCharacter).ToString();
                    return new SPlayerData(
                        TextHandler.SplitCamelCase(trainingCharacter),
                        9,
                        trainingCharacter,
                        PlayerPrefsHandler.GetTrainingRunes(),
                        new int[] { 9, 9, 9 },
                        PlayerPrefsHandler.GetTrainingSpells(),
                        new int[] { 9, 9, 9, 9 },
                        new SProfileCurrentData(accountLevel: 9, gamerTag: trainingCharacter.ToString()).AsNetworkSerializable(),
                        isPlayer: false,
                        botData: new SBotData(
                            difficulty:         PlayerPrefs.GetString(EPlayerPref.TrainingDifficulty.ToString(), ELeague.Silver.ToString()), 
                            decisionRefresh:    PlayerPrefs.GetFloat(EPlayerPref.TrainingDecisionRefresh.ToString(), 0.05f), 
                            randomness:         PlayerPrefs.GetFloat(EPlayerPref.TrainingRandomness.ToString(), 0f),
                            reactionTime:       (PlayerPrefs.GetFloat(EPlayerPref.TrainingReactionTime.ToString() + "Min", 0f), PlayerPrefs.GetFloat(EPlayerPref.TrainingReactionTime.ToString() + "Max", 0.5f)),
                            movementTime:       (PlayerPrefs.GetFloat(EPlayerPref.TrainingMovementTime.ToString() + "Min", 0f), PlayerPrefs.GetFloat(EPlayerPref.TrainingMovementTime.ToString() + "Max", 1f)),
                            movementRefresh:    (PlayerPrefs.GetFloat(EPlayerPref.TrainingMovementRefresh.ToString() + "Min", 0f), PlayerPrefs.GetFloat(EPlayerPref.TrainingMovementRefresh.ToString() + "Max", 0.5f)),
                            extraVariables:     GetTrainingExtraVariables()
                        )
                    );

                // ================================================================================================
                // RANKED MODE : random
                case EGameMode.Ranked:
                    return BotBuilder.GenerateBot(ProgressionCloudData.LeagueCloudData);

                // ================================================================================================
                default:
                    ErrorHandler.Error("Unhandled mode : " + GameMode);
                    return default;
            }
        }

        Dictionary<string, float> GetTrainingExtraVariables()
        {
            var dict = new Dictionary<string, float>();
            if (!Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.TrainingDifficulty.ToString()), out ELeague league))
            {
                ErrorHandler.Error("Unable to parse " + PlayerPrefs.GetString(EPlayerPref.TrainingDifficulty.ToString()) + " as League");
                return dict;
            }

            List<string> variables = DefaultBotBT.GetExtraVariables(league);
            foreach (string varName in variables)
            {
                dict[varName] = PlayerPrefs.GetFloat(varName, 0f);
            }

            return dict;
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

        async void UpdatePlayerData(string playerName = "", ECharacter character = ECharacter.None)
        {
            var data = new Dictionary<string, PlayerDataObject>();
            if (playerName != "")
                data.Add(StaticPlayerData.KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName));

            if ( character != ECharacter.None)
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
            m_CancelRetry = false;

            try
            {
                await method();
            }
            catch (Exception e)
            {
                // if state is back to inactive or a cancel of retry has been asked => stop retrying
                if (m_CancelRetry || m_State == ELobbyState.Inactive)
                    return false;

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
            List<Lobby> lobbies = new List<Lobby>();
            try
            {
                // NO REGION: return empty lobbies
                if (string.IsNullOrEmpty(ProfileCloudData.Region))
                    return lobbies;

                QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter> {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.S2, m_GameMode.ToString(), QueryFilter.OpOptions.EQ),
                        new QueryFilter(QueryFilter.FieldOptions.S3, RelayHandler.TrimRegion(ProfileCloudData.Region), QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder> {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };

                QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

                ErrorHandler.Log("Lobbies found: " + queryResponse.Results.Count, ELogTag.Lobby);

                // Filter and sort the results
                lobbies = queryResponse.Results
                    .OrderByDescending(lobby => lobby.Data[KEY_SUB_REGION].Value == ProfileCloudData.Region)
                    .ThenBy(lobby => lobby.Created)
                    .ToList();
            }
            catch (LobbyServiceException e)
            {
                ErrorHandler.Error("Failed to list lobbies: " + e.Message);
            }

            return lobbies;
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
                "An error has occured while creating " + m_GameMode.ToString() + " game mode : "
                    + "\n   + Lobby State : " + m_State.ToString()
                    + (reason.Length > 0 ? "\n   + Reason : " + reason : "")
                    + (exceptionMessage.Length > 0 ? "\n   + Exception : " + exceptionMessage : "");

                Main.AddStoredEvent(EAppState.MainMenu, () => Main.SetPopUp(EPopUpState.MessagePopUp, message));
                LeaveLobby();
                ExitScene();
            };
        }

        #endregion
    }
}