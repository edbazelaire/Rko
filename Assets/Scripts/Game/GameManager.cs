using Assets;
using Assets.Scripts.Managers.Sound;
using Assets.Scripts.Tools;
using Enums;
using Externals;
using Game.Loaders;
using Managers;
using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Game
{
    public class GameManager : OvNetworkBehavior
    {
        #region Members

        static GameManager s_Instance;

        public const string TIME_WRAPPER_ID = "Game";

        public const int BOT_CLIENT_ID = 999;
        public const int N_LOADING_STEPS = 3;
        public const int DEFAULT_PVP_LEVEL = 9;

        // ===================================================================================
        // ACTIONS
        public static Action GameStartedEvent;
        public static Action GameEndEvent;

        // ===================================================================================
        // PRIVATE VARIABLES 
        // -- Network Variables
        /// <summary> current state of the Game </summary>
        NetworkVariable<EGameState> m_State = new NetworkVariable<EGameState>(EGameState.None);
        /// <summary> percentage of the game preparation at start </summary>
        NetworkVariable<float> m_ProgressGameStart = new NetworkVariable<float>(0f);
        /// <summary> expected number of players in the game </summary>
        NetworkVariable<int> m_NPlayers = new NetworkVariable<int>(-1);

        // -- Player Data
        /// <summary> [SERVER] number of player data expected to be received (includes bot's PlayerData) </summary>
        protected int m_NPlayerDataExpected => 2;
        /// <summary> [SERVER] dict matching a client id to player data </summary>
        protected Dictionary<ulong, SPlayerData> m_PlayersData = new();
        /// <summary> [CLIENT/SERVER] dict matching a client id to a player controller </summary>
        protected Dictionary<ulong, Controller> m_Controllers = new();


        // -- Initialization
        /// <summary> [CLIENT/SERVER] has the GameManager current Instance been initialized ? </summary>
        protected bool m_Initialized = false;
        /// <summary> [SERVER] list of clientId who have return that their initialization was finalized </summary>
        List<ulong> m_ClientsInitialized = new();
        /// <summary> [CLIENT] used to check if the initialization is completed on the client side (to avoid sending multiple time the validation to the server) </summary>
        bool m_InitOnClientSide = false;

        // ===================================================================================
        // PUBLIC ACCESSORS 
        public Dictionary<ulong, Controller> Controllers => m_Controllers;
        public NetworkVariable<float> ProgressGameStart => m_ProgressGameStart;
        public NetworkVariable<EGameState> State => m_State;
        /// <summary> intro starting : game fully loaded </summary>
        public bool IsGameLoaded => m_State.Value >= EGameState.Intro;
        /// <summary> intro completed : game starts </summary>
        public bool IsGameStarted => m_State.Value > EGameState.Intro;
        /// <summary> game is over </summary>
        public static bool IsGameOver => Instance == null || Instance.m_State.Value >= EGameState.GameOver;

        #endregion


        #region Inherited Manipulators

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_ClientsInitialized    = new();
            m_PlayersData           = new();
            m_Controllers           = new Dictionary<ulong, Controller>();

            s_Instance = this;
        }

        #endregion


        #region Initialization & End

        void Initialize()
        {
            ErrorHandler.Log("Initialize()", ELogTag.GameSystem);

            // avoid re-initialization
            if (m_Initialized)
                return;

            m_Controllers = new Dictionary<ulong, Controller>();
            m_InitOnClientSide = false;

            // instantiate listeners
            m_ProgressGameStart.OnValueChanged  += OnProgressGameStartChanged;
            m_State.OnValueChanged              += OnStateValueChanged;

            // set number of max players equal to number of players in the lobby
            m_NPlayers.Value = 2;

            // set that the GameManager is initialized to avoid re-initialization
            m_Initialized = true;

            // set GameManager is Instantiated and is waiting for players connections
            SetState(EGameState.WaitingForConnection);

            return;
        }

        /// <summary>
        /// Reset Instance, unregister all listeners, destroy game object
        /// </summary>
        public void Shutdown()
        {
            // unregister from each events
            m_State.OnValueChanged -= OnStateValueChanged;

            if (IsServer)
            {
                m_ProgressGameStart.Value = 0f;
            }

            // cancel methods in TimeWrapper
            TimeErrorWrapper.Instance.Cancel(TIME_WRAPPER_ID);

            // reset value of static Instance, so the Initialize() would be re-called
            s_Instance = null;

            // destroy this GameManager
            Destroy(gameObject);
        }

        #endregion


        #region [STATE] Waiting For Connection 

        /// <summary>
        /// Check that all players are there, loaded, and ready to play
        /// </summary>
        /// <returns></returns>
        bool CheckConnectionDone()
        {
            return m_PlayersData.Count == m_NPlayerDataExpected;
        }

        /// <summary>
        /// Add the data of a player to the list of players data
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="character"></param>
        [ServerRpc(RequireOwnership = false)]
        public void AddPlayerDataServerRPC(ulong clientId, SPlayerData playerData)
        {
            if (!IsServer)
                return;

            ErrorHandler.Log("AddPlayerDataServerRPC + clientId " + clientId + " with character " + playerData.Character.ToString(), ELogTag.GameSystem);
            m_PlayersData.Add(clientId, playerData);

            m_ProgressGameStart.Value += 1f / ((float)LobbyHandler.Instance.MaxPlayers * N_LOADING_STEPS);

            // check if all players are there
            if (CheckConnectionDone())
                SetState(EGameState.PreparingGame);
        }

        #endregion


        #region [STATE] Preparing Game

        /// <summary>
        /// Spawn all players
        /// </summary>
        void SpawnPlayers()
        {
            if (!IsServer)
                return;

            foreach (var item in m_PlayersData)
            {
                SpawnPlayer(item.Key, item.Value);
                m_ProgressGameStart.Value += 1f / (LobbyHandler.Instance.MaxPlayers * N_LOADING_STEPS);
            }
        }

        /// <summary>
        /// Spawn a player
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="character"></param>
        void SpawnPlayer(ulong clientId, SPlayerData playerData)
        {
            if (!IsServer)
                return;

            int team = m_Controllers.Count;

            GameObject playerPrefab;
            if (playerData.IsPlayer)
            {
                // create player prefab and spawn it
                playerPrefab = Instantiate(CharacterLoader.Instance.PlayerPrefab, ArenaManager.Instance.transform);
                playerPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            }
            else
            {
                // create an AI prefab and spawn it
                playerPrefab = Instantiate(CharacterLoader.Instance.PlayerAIPrefab, ArenaManager.Instance.transform);
                playerPrefab.GetComponent<NetworkObject>().Spawn();
            }
           
            // add player to list of player controllers
            Controller controller = Finder.FindComponent<Controller>(playerPrefab);

            UpdatePlayerData(ref playerData);

            // initialize player data
            controller.Initialize(
                playerData: playerData,
                team: team,
                isPlayer: playerData.IsPlayer
            );

            // add event listener to the player's hp
            controller.Life.DiedEvent += OnPlayerDied;
        }

        /// <summary>
        /// Update player data depending on game mode.
        /// [Arena]
        ///     No changes
        /// 
        /// [Ranked || Test]
        ///     For test purpuses, all data (char and spells) are set to level 9
        /// </summary>
        /// <param name="playerData"></param>
        void UpdatePlayerData(ref SPlayerData playerData)
        {
            if (LobbyHandler.Instance.GameMode == EGameMode.Arena)
                return;

            playerData.CharacterLevel = DEFAULT_PVP_LEVEL;
            for (int i = 0; i < playerData.SpellLevels.Length; i++)
            {
                playerData.SpellLevels[i] = DEFAULT_PVP_LEVEL;
            }
        }

        /// <summary>
        /// [LOCAL] Link client id to controllers
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="controller"></param>
        public void AddController(ulong clientId, Controller controller)
        {
            if (m_Controllers.ContainsKey(clientId))
            {
                ErrorHandler.Warning("Trying to add Controller for client " + clientId + " but this client is already in list of controllers");
                return;
            }
            m_Controllers.Add(clientId, controller);
        }

        /// <summary>
        /// Wait for all clients to return that every controller are properly initilized
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitClientInitialized()
        {
            if (!IsServer)
                yield break;

            // call clients to check if they are 
            while (m_ClientsInitialized.Count != LobbyHandler.Instance.MaxPlayers)
            {
                CheckInitializedClientRPC();
                yield return null;
            }

            // once every one is initialized, setup the UI 
            SetupUIClientRPC();

            // do a little shake of player position to be sure that everything is synchronized
            ShakePlayers();

            // goto intro
            SetState(EGameState.Intro);
        }

        /// <summary>
        /// Check on client that all controllers have been set
        /// </summary>
        /// <returns></returns>
        [ClientRpc]
        void CheckInitializedClientRPC()
        {
            // check if already known as init
            if (m_InitOnClientSide)
                return;

            if (m_Controllers.Count != 2)
                return;

            m_InitOnClientSide = true;

            // tell server that is ready
            SetClientIntializedServerRPC(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Tell the server that the client is ready
        /// </summary>
        /// <param name="clientId"></param>
        [ServerRpc(RequireOwnership = false)]
        void SetClientIntializedServerRPC(ulong clientId)
        {
            ErrorHandler.Log("Client Initialized : " + clientId, ELogTag.GameSystem);

            if (!m_ClientsInitialized.Contains(clientId))
                m_ClientsInitialized.Add(clientId);
        }

        /// <summary>
        /// Call all clients to setup UI for each controller
        /// </summary>
        [ClientRpc]
        void SetupUIClientRPC()
        {
            foreach (Controller controller in m_Controllers.Values)
            {
                controller.InitializeUI();
            }

            ErrorHandler.Log("SetupUIClientRPC()", ELogTag.GameSystem);
            GameUIManager.Instance.SetUpIntroScreen();
        }

        /// <summary>
        /// Do a little position shake to make sure everything is synchronized for clients
        /// </summary>
        void ShakePlayers()
        {
            if (!IsServer)
                return;

            foreach (Controller player in m_Controllers.Values)
            {
                player.Movement.Shake();
            }
        }

        #endregion


        #region [STATE] Intro

        void StartIntro()
        {
            LobbyHandler.Instance.LeaveLobby();

            if (!IsServer)
                return;

            StartCoroutine(PlayIntro());
        }

        IEnumerator PlayIntro()
        {
            // call clients to start intro animation
            PlayIntroAnimationClientRPC();
            yield return new WaitForSeconds(2.5f);

            // call clients to start intro countdown
            PlayCountDownClientRPC();
            yield return new WaitForSeconds(3);

            // set state that game is running
            SetState(EGameState.GameRunning);
        }

        [ClientRpc]
        void PlayIntroAnimationClientRPC()
        {
            ErrorHandler.Log("Play Intro Animation");
            GameUIManager.IntroGameUI.PlayEnterAnimation();
        }

        [ClientRpc]
        void PlayCountDownClientRPC()
        {
            GameUIManager.IntroGameUI.PlayExitAnimation();
        }

        #endregion

        
        #region [STATE] Game Over

        public void GameOver(int team)
        {
            ShutDownControllersServerSide();
            GameOverClientRPC(team);
        }

        /// <summary>
        /// Call the game over on clients
        /// </summary>
        /// <param name="team"></param>
        [ClientRpc]
        public void GameOverClientRPC(int team)
        {
            ErrorHandler.Log("GameOverClientRPC", ELogTag.GameSystem);

            if (! Instance.Owner.IsPlayer)
                return;

            ShutDownControllers(team);
            GameUIManager.Instance.SetUpGameOver(team == Instance.Owner.Team);
        }

        void ShutDownControllers(int team)
        {
            // set "Game Ended" mode for each player
            foreach (Controller controller in m_Controllers.Values)
            {
                if (controller.IsDestroyed())
                    continue;

                controller.OnGameEnded(team == controller.Team);
            }
        }

        void ShutDownControllersServerSide()
        {
            if (!IsServer)
                return;

            // set "Game Ended" mode for each player
            foreach (Controller controller in m_Controllers.Values)
            {
                if (controller.IsDestroyed())
                    continue;

                controller.ActivateActionComponent(false);
            }
        }

        void CheckGameEnd()
        {
            var teamCtr = new List<int>();
            foreach (var item in m_Controllers)
            {
                Controller controller = item.Value;
                if (controller.Life.IsAlive && ! teamCtr.Contains(controller.Team))
                    teamCtr.Add(controller.Team);
            }

            if (teamCtr.Count == 1)
            {
                SetState(EGameState.GameOver);
                GameOver(teamCtr[0]);
            }
        }

        #endregion


        #region Music & Sound

        [ClientRpc]
        public void PlaySoundClientRPC(string spellName, ESpellEvent spellAction)
        {
            var spellData = SpellLoader.GetSpellData(spellName);
            AudioClip audioClip = null;

            switch (spellAction)
            {
                case ESpellEvent.OnStartCast:
                    audioClip = spellData.AnimationSoundFX;
                    break;

                case ESpellEvent.OnCast:
                    audioClip = spellData.CastSoundFX != null ? spellData.CastSoundFX : SoundFXManager.DefaultCastSoundFX;
                    break;

                case ESpellEvent.OnHit:
                    audioClip = spellData.OnHitSoundFX != null ? spellData.OnHitSoundFX : SoundFXManager.DefaultOnHitSoundFX;
                    break;

                case ESpellEvent.OnEnd:
                    audioClip = spellData.OnEndSoundFX;
                    break;
            }

            if (audioClip == null)
                return;

            SoundFXManager.PlayOnce(audioClip);
        }


        [ClientRpc]
        void PlayStateMusicClientRPC(EGameState state)
        {
            SoundFXManager.PlayStateMusic(state);
        }

        #endregion


        #region Public Manipulators

        public Controller GetPlayer(ulong clientId)
        {
            if (!m_Controllers.ContainsKey(clientId))
                ErrorHandler.FatalError("Unable to find controller with client id : " + clientId);

            return m_Controllers[clientId];
        }

        public Controller GetFirstEnemy(int team)
        {
            foreach (Controller controller in m_Controllers.Values)
                if (controller.Team != team)
                    return controller;

            return null;
        }

        public Controller GetFirstAlly(int team, ulong slefId)
        {
            // find first non self ally
            foreach (Controller controller in m_Controllers.Values)
                if (controller.Team == team && controller.OwnerClientId != slefId)
                    return controller;

            // none found -> return self
            return GetPlayer(slefId);
        }

        public bool HasPlayer(ulong clientId)
        {
            return GetPlayer(clientId) != null;
        }


        #endregion


        #region State Manipulators

        /// <summary>
        /// Set the state of the game
        /// </summary>
        /// <param name="state"></param>
        void SetState(EGameState state)
        {
            if (!IsServer)
            {
                ErrorHandler.Warning("Trying to set state from a non Server machine");
                return;
            }

            PlayStateMusicClientRPC(state);

            // fire event that game has started if state becomes GameRunning
            if (state == EGameState.GameRunning && m_State.Value != EGameState.GameRunning)
            {
                // fire event that game has started (for Server)
                GameStartedEvent?.Invoke();

                // fire event that game has started (for Clients)
                GameStartedEventClientRPC();
            }

            m_State.Value = state;
        }

        [ClientRpc]
        void GameStartedEventClientRPC()
        {
            GameStartedEvent?.Invoke();
        }

        #endregion


        #region Dependent Members

        public static GameManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    // check in scene
                    s_Instance = FindAnyObjectByType<GameManager>();

                    // not found : create a new one
                    if (s_Instance == null)
                    {
                        ErrorHandler.Error("GameManager not found");
                        return null;
                    }

                    s_Instance.Initialize();
                }
                return s_Instance;
            }
        }

        public static bool FindInstance(bool checkSpawned = false)
        {
            var instance = FindAnyObjectByType<GameManager>();
            if (instance == null)
                return false;

            if (checkSpawned && !instance.IsSpawned)
                return false;

            instance.Initialize();
            s_Instance = instance;
            return true;
        }

        public static bool Exists => s_Instance != null || FindAnyObjectByType<GameManager>() == null;

        /// <summary>
        /// Controller of the local player
        /// </summary>
        public Controller Owner
        {
            get
            {
                return GetPlayer(NetworkManager.Singleton.LocalClientId);
            }
        }

        public virtual ulong m_PlayerId => NetworkManager.Singleton.LocalClientId;

        #endregion


        #region Listeners

        void OnProgressGameStartChanged(float oldValuen, float progress)
        {
            //Debug.Log("Progress : " + progress);
        }

        void OnStateValueChanged(EGameState oldValue, EGameState newState)
        {
            ErrorHandler.Log("New state : " + newState, ELogTag.GameSystem);

            switch (newState)
            {
                case EGameState.WaitingForConnection:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 30f, OnPreparingGameTimeLimit);
                    break;

                case EGameState.PreparingGame:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 30f, OnPreparingGameTimeLimit);
                    StartCoroutine(WaitClientInitialized());
                    SpawnPlayers();
                    break;

                case EGameState.Intro:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 30f, OnGameRunningTimeLimit);
                    StartIntro();
                    break;

                case EGameState.GameRunning:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 5*60f, OnGameRunningTimeLimit);
                    break;

                case EGameState.GameOver:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 15f, OnGameOverTimeLimit);
                    break;
            }
        }

        void OnPlayerDied()
        {
            CheckGameEnd();
        }

        #endregion


        #region Error Management

        void ExitWithError(string message)
        {
            Main.AddStoredEvent(EAppState.MainMenu, () => Main.SetPopUp(EPopUpState.MessagePopUp, message));
            GameManager.Instance.Shutdown();
            SceneLoader.Instance.LoadScene("MainMenu");
        }

        void OnPreparingGameTimeLimit()
        {
            ExitWithError(
                "An error has occured while creating " + LobbyHandler.Instance.GameMode + " game mode : "
                    + "\n   + Game State : " + m_State
                    + "\n   + Reason : Preparing game has reached time limit"
            );
        }

        void OnGameRunningTimeLimit()
        {
            ExitWithError(
                "An error has occured while playing " + LobbyHandler.Instance.GameMode + " game mode : "
                    + "\n   + Game State : " + m_State
                    + "\n   + Reason : Game has reached its safety time limit"
            );
        }

        void OnGameOverTimeLimit()
        {
            SceneLoader.Instance.LoadScene("MainMenu");
        }


        #endregion


        #region Debug Callbacks

        [Command(KeyCode.N)]
        public void AutoWin()
        {
            GameOverClientRPC(Owner.Team);
        }

        [Command(KeyCode.B)]
        public void AutoLoss()
        {
            GameOverClientRPC((Owner.Team + 1) % 2);
        }

        [Command(KeyCode.M)]
        public void RemoveLife()
        {
            Owner.Life.Hit(50);
        }

        [Command(KeyCode.L)]
        public void RemoveLifeEnemy()
        {
            GetFirstEnemy(Owner.Team).Life.Hit(50);
        }

        /// <summary>
        /// Toggle activation of AIs (for tests)
        /// </summary>
        [Command(KeyCode.P)]
        public void ToggleAI()
        {
            foreach (Controller controller in m_Controllers.Values)
            {
                if (controller.IsPlayer)
                    continue;

                controller.BehaviorTree.Activate(!controller.BehaviorTree.IsActivated);
            }
        }

        [Command(KeyCode.Space)]
        public void Recharge()
        {
            Owner.EnergyHandler.AddEnergy(100);
            Owner.SpellHandler.ResetCooldowns();
        }

        /// <summary>
        /// Rescale all characters after changing size factor from the DebugSettings 
        /// </summary>
        public void RescaleCharacters()
        {
            foreach (Controller controller in m_Controllers.Values)
            {
                controller.SetSize();
            }
        }

        #endregion

    }
}
