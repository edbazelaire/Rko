using Assets;
using Assets.Scripts.Game;
using Game.GameManagers.Interfaces;
using Assets.Scripts.Managers.Sound;
using Assets.Scripts.Tools;
using Data;
using Data.DataStructures.CharacterSubStructures;
using Enums;
using Externals;
using Game.GameManagers.ArenaModules;
using Game.GameManagers.Components;
using Game.Loaders;
using Game.Spells;
using Managers;
using Network;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tools;
using Tools.Helpers;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Game
{
    public class GameManager : MNetworkBehavior
    {
        #region Members

        static GameManager s_Instance;

        public const string TIME_WRAPPER_ID = "Game";

        public const int BOT_CLIENT_ID = 100;
        public const int SPAWN_CLIENT_ID = 1000;
        public const int N_LOADING_STEPS = 3;
        public const int DEFAULT_PVP_LEVEL = 9;

        // ===================================================================================
        // ACTIONS
        public static Action GameStartedEvent;
        public static Action TimerEndedEvent;
        public static Action GameEndEvent;

        // ===================================================================================
        // Modules
        private List<IGameModule> m_Modules;
        private ArenaModule m_ArenaModule;

        // ===================================================================================
        // GameObjects & Components
        private GameAnalyticsManager    m_GameAnalyticsManager;
        private InGameDebugger          m_InGameDebugger;

        // ===================================================================================
        // PRIVATE VARIABLES 
        // -- Network Variables
        /// <summary> current state of the Game </summary>
        NetworkVariable<EGameState> m_State = new NetworkVariable<EGameState>(EGameState.None);
        /// <summary> percentage of the game preparation at start </summary>
        NetworkVariable<float> m_ProgressGameStart = new NetworkVariable<float>(0f);
        /// <summary> expected number of players in the game </summary>
        NetworkVariable<int> m_NPlayers = new NetworkVariable<int>(-1);

        // -- Game Data
        public EGameMode GameMode { get; private set; }
        public int WinningTeam { get; private set; } = -1;

        // -- Player Data
        /// <summary> [SERVER] number of player data expected to be received (includes bot's PlayerData) </summary>
        protected int m_NPlayerDataExpected => 2;
        /// <summary> [SERVER] dict matching a client id to player data </summary>
        protected Dictionary<ulong, SPlayerData> m_PlayersData = new();
        /// <summary> [CLIENT/SERVER] dict matching a client id to a player controller </summary>
        protected Dictionary<ulong, Controller> m_Controllers = new();
        /// <summary> [CLIENT/SERVER] dict matching a client id to a spawn controller </summary>
        protected Dictionary<ulong, Controller> m_Spawns = new();
        /// <summary> [SERVER] current BOT extra id (to add to base BOT_CLIENT_ID) </summary>
        protected int m_BotId = 0;
        /// <summary> [SERVER] current SPAWN extra id (to add to base SPAWN_CLIENT_ID) </summary>
        protected int m_SpawnId = 0;

        // -- Initialization
        /// <summary> [CLIENT/SERVER] has the GameManager current Instance been initialized ? </summary>
        protected bool m_Initialized = false;
        /// <summary> [SERVER] list of clientId who have return that their initialization was finalized </summary>
        List<ulong> m_ClientsInitialized = new();
        /// <summary> [CLIENT] used to check if the initialization is completed on the client side (to avoid sending multiple time the validation to the server) </summary>
        bool m_InitOnClientSide = false;
        /// <summary> is this a TUTORIAL game ? </summary>
        bool m_IsTuto = false;

        // ===================================================================================
        // LOCAL CLIENT DATA
        public ulong MyId { get; private set;}
        public SPlayerData MyPlayerData { get; private set;}


        // ===================================================================================
        // PUBLIC ACCESSORS 
        public ArenaModule                      ArenaModule             => m_ArenaModule;
        public GameAnalyticsManager             GameAnalyticsManager    => m_GameAnalyticsManager;
        public Dictionary<ulong, Controller>    Controllers             => m_Controllers;
        public Dictionary<ulong, Controller>    Spawns                  => m_Spawns;
        public NetworkVariable<float>           ProgressGameStart       => m_ProgressGameStart;
        public NetworkVariable<EGameState>      State                   => m_State;

        /// <summary> check if GameManager exists, if has an Instance or the game object exists in the scene </summary>
        public static bool Exists => s_Instance != null || FindAnyObjectByType<GameManager>() != null;
        /// <summary> is the game mode an "offline" mode ? e.g : not a ranked mode NOT filled with bots </summary>
        //public bool IsOfflineMode => false;
        public bool IsOfflineMode => ! (GameMode == EGameMode.Ranked && !LobbyHandler.Instance.IsFilledWithBots);
        /// <summary> intro starting : game fully loaded </summary>
        public bool IsGameLoaded => m_State.Value >= EGameState.Intro;
        /// <summary> intro completed : game starts </summary>
        public bool IsGameStarted => m_State.Value > EGameState.Intro;
        /// <summary> game is over </summary>
        public static bool IsGameOver => s_Instance == null || Instance.m_State.Value >= EGameState.GameOver || ErrorHandler.IsExiting;
        /// <summary> is game currently running ? </summary>
        public static bool IsGameRunning => Instance.IsGameStarted && ! IsGameOver;
        #endregion


        #region Inherited Manipulators

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_ClientsInitialized    = new();
            m_PlayersData           = new();
            m_Controllers           = new Dictionary<ulong, Controller>();
            m_GameAnalyticsManager  = Finder.FindComponent<GameAnalyticsManager>(gameObject);
            m_InGameDebugger        = Finder.FindComponent<InGameDebugger>(gameObject);

            s_Instance = this;

            if (! GameUIManager.Initialized)
                GameUIManager.Instance.Initialize();

#if !UNITY_EDITOR
            m_InGameDebugger.enabled = false;
#endif
            AttachDebugMethods();
        }
#endregion


        #region Debug

        void AttachDebugMethods()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("Debug_NamedMessage", (senderClientId, reader) =>
            {
                Debug.Log($"[NETWORK DEBUG] Named Message Received from {senderClientId}");
            });
        }

        #endregion


        #region Initialization & End

        public void Initialize()
        {
            ErrorHandler.Log("Initialize()", ELogTag.GameSystem);

            // avoid re-initialization
            if (m_Initialized)
                return;

            m_Controllers               = new Dictionary<ulong, Controller>();
            m_InitOnClientSide          = false;
            GameMode                    = LobbyHandler.Instance.GameMode;
            m_IsTuto                    = LobbyHandler.Instance.IsTuto;

            // instantiate listeners
            m_ProgressGameStart.OnValueChanged  += OnProgressGameStartChanged;
            m_State.OnValueChanged              += OnStateValueChanged;

            // set number of max players equal to number of players in the lobby
            m_NPlayers.Value = 2;

            StartCoroutine(CheckInitialized());

            return;
        }

        void InitializeModules()
        {
            if (!IsServer)
                return;

            m_Modules = new();

            if (LobbyHandler.Instance.GameMode == EGameMode.Arena)
            {
                switch (LobbyHandler.Instance.ArenaType)
                {
                    case EArenaType.EternalMenagerie:
                        m_ArenaModule = gameObject.AddComponent<EternalMenagerieModule>();
                        break;

                    default:
                        m_ArenaModule = gameObject.AddComponent<ArenaModule>();
                        break;
                }

                m_ArenaModule.Initialize(LobbyHandler.Instance.ArenaType, ProgressionCloudData.CurrentArena.GetArenaDifficulty(), ProgressionCloudData.CurrentArena.GetExtraDifficulty());
                m_Modules.Add(m_ArenaModule);
            }
        }

        IEnumerator CheckInitialized()
        {
            TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 30f, OnInitializingTimeLimit);

            while (GameUIManager.Instance == null || ! GameUIManager.Initialized)
            {
                yield return null;
            }

            // start initializing modules
            InitializeModules();

            TimeErrorWrapper.Instance.Cancel(TIME_WRAPPER_ID);

            // SERVER    -----------------------------------
            if (!IsServer)
                yield break;

            // GameManager is Initialized and ready to receive connections
            SetState(EGameState.WaitingForConnection);
        }

        /// <summary>
        /// Reset Instance, unregister all listeners, destroy game object
        /// </summary>
        public void Shutdown()
        {
            StopAllCoroutines();

            // unregister from each events
            m_State.OnValueChanged -= OnStateValueChanged;
            StateEffect.StateEffectEvent = null;    // reset all registeries to the StateEffect static event
            Spell.OnSpellSpawn = null;              // reset all registeries to the Spell static event

            // cancel methods in TimeWrapper
            TimeErrorWrapper.Instance.Cancel(TIME_WRAPPER_ID);

            // reset value of static Instance, so the Initialize() would be re-called
            s_Instance = null;

            if (m_IsTuto)
                Destroy(TutoGameManager.Instance.gameObject);

            // destroy this GameManager
            Destroy(gameObject);
        }

        #endregion


        #region Modules

        bool CheckModules()
        {
            foreach (var module in m_Modules)
            {
                if (!module.CheckIsValid())
                    return false;
            }

            return true;
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

            ErrorHandler.Log("AddPlayerDataServerRPC + clientId " + clientId + " with character " + playerData.BuildData.Character.ToString(), ELogTag.GameSystem);
            m_PlayersData.Add(clientId, playerData);

            m_ProgressGameStart.Value += 1f / ((float)LobbyHandler.Instance.MaxPlayers * N_LOADING_STEPS);

            // check if all players are there
            if (CheckConnectionDone() && CheckModules())
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

            GameObject playerPrefab = CharacterLoader.GetPrefab(playerData.BuildData.Character.ToString(), playerData.IsPlayer, m_IsTuto);
            if (playerData.IsPlayer)
            {
                // create player prefab and spawn it
                playerPrefab = Instantiate(playerPrefab, ArenaManager.Instance.Spawns[team][0].position, Quaternion.identity, ArenaManager.Instance.transform);
                playerPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            }
            else
            {
                // create an AI prefab and spawn it
                playerPrefab = Instantiate(playerPrefab, ArenaManager.Instance.transform);
                playerPrefab.GetComponent<NetworkObject>().Spawn(true);
            }
           
            // add player to list of player controllers
            Controller controller = Finder.FindComponent<Controller>(playerPrefab);

            // postprocess player data if needed
            UpdatePlayerData(ref playerData);

            // initialize player data
            controller.Initialize(
                playerData: playerData,
                team: team,
                isPlayer: playerData.IsPlayer
            );

            // add event listener to the player's hp
            controller.Life.OnDeathEvent += OnPlayerDied;
        }

        /// <summary>
        /// Update player data depending on game mode.
        /// [Ranked || Arena]
        ///     No changes
        /// 
        /// [Training]
        ///     For test purpuses, all data (char and spells) are set to level 9
        /// </summary>
        /// <param name="playerData"></param>
        void UpdatePlayerData(ref SPlayerData playerData)
        {
            if (GameMode != EGameMode.Training)
                return;

            playerData.BuildData.CharacterLevel = DEFAULT_PVP_LEVEL;
            for (int i = 0; i < playerData.BuildData.SpellLevels.Length; i++)
            {
                playerData.BuildData.SpellLevels[i] = DEFAULT_PVP_LEVEL;
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
        /// [LOCAL] Link client id to controllers
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="controller"></param>
        public void AddSpawnController(ulong clientId, Controller controller)
        {
            if (m_Spawns.ContainsKey(clientId))
            {
                ErrorHandler.Warning("Trying to add Spawn with id " + clientId + " but this id is already in list of controllers");
                return;
            }
            m_Spawns.Add(clientId, controller);

            controller.OnDestroyedEvent += () => m_Spawns.Remove(controller.PlayerId);
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
                if (CheckIsFilledWithBots())
                    break;

                CheckInitializedClientRPC();
                yield return null;
            }

            while (!CheckModules())
                yield return null;

            // once every one is initialized, setup the UI 
            SetupUIClientRPC();

            // goto intro
            SetState(EGameState.Intro);
        }

        bool CheckIsFilledWithBots()
        {
            if (GameMode != EGameMode.Ranked)
                return false;

            int nBots = 1; // 1 because Host is server side so it count has one initialized on Server side
            foreach (Controller controller in m_Controllers.Values)
            {
                if (! controller.IsPlayer)
                    nBots++;
            }

            return nBots == LobbyHandler.Instance.MaxPlayers;
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
            // adjust Camera
            if (IsOwner)
            {
                var cameraAdjuster = Finder.FindComponent<CameraAdjuster>(Camera.main.gameObject);
                if (cameraAdjuster == null)
                {
                    ErrorHandler.Error("No CameraAdjuster was found for client " + m_PlayerId);
                    return;
                }

                cameraAdjuster.Initialize();
            }
           
            foreach (Controller controller in m_Controllers.Values)
            {
                controller.InitializeUI();
            }

            ErrorHandler.Log("SetupUIClientRPC()", ELogTag.GameSystem);
            GameUIManager.Instance.SetUpIntroScreen();
        }

        void SetUpTimer()
        {
            // display or not the Timer
            if (LobbyHandler.Instance.GameMode == EGameMode.Ranked)
            {
                GameUIManager.GameTimerUI.Initialize(240);
                TimerEndedEvent += OnTimerEnd;
            }
                
            else if (LobbyHandler.Instance.GameMode == EGameMode.Arena)
            {
                m_ArenaModule.SetUpTimer();
            }

            else
                GameUIManager.GameTimerUI.Initialize(0);
        }

        public ulong GetNextBotId()
        {
            m_BotId++;  
            return (ulong)(BOT_CLIENT_ID + m_BotId);
        }

        public ulong GetNextSpawnId()
        {
            m_SpawnId++;  
            return (ulong)(SPAWN_CLIENT_ID + m_SpawnId);
        }

        public bool IsBotId(ulong clientId)
        {
            return BOT_CLIENT_ID <= clientId && clientId < SPAWN_CLIENT_ID;
        }

        public bool IsSpawnId(ulong clientId)
        {
            return SPAWN_CLIENT_ID <= clientId;
        }

        #endregion


        #region [STATE] Intro

        void StartIntro()
        {
            LobbyHandler.Instance.LeaveLobby();

            if (!IsServer)
                return;

            if (m_IsTuto)
                StartTuto();
            else
                StartCoroutine(PlayIntro());
        }

        void StartTuto()
        {
            // deactivate INTRO UI
            GameUIManager.IntroGameUI.gameObject.SetActive(false);

            // activate TUTO
            if (IsServer)
                TutoGameManager.Instance.Activate(m_Controllers[0], m_Controllers[BOT_CLIENT_ID + 1]);
        }

        IEnumerator PlayIntro()
        {
            GameUIManager.IntroGameUI.gameObject.SetActive(true);

            // call clients to start intro animation
            PlayIntroAnimationClientRPC();
            yield return new WaitForSeconds(2.5f);

            // call clients to start intro countdown
            PlayCountDownClientRPC();
            yield return new WaitForSeconds(3);

            // wait for modules to validate the end of the Intro
            while (! CheckModules())
                yield return null;

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
            // save the winning team
            WinningTeam = team;

            // call that game is over
            SetState(EGameState.GameOver);

            // shutdown on server side
            ShutDownControllersServerSide();

            // collect analytics from server before shutodown
            GameAnalyticsManager.Instance.SendGameAnalytics();

            // display gameOver ui
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

            // shut down controllers activity
            ShutDownControllers(team);

            // destroy DisconnectionHandler
            DisconnectionHandler.End();

            // setup the UI for end of the game
            GameUIManager.Instance.SetUpGameOver(GetGameResult(team));
        }

        EGameResult GetGameResult(int team)
        {
            if (team < 0)
                return EGameResult.Draw;

            if (team == Instance.Owner.Team)
                return EGameResult.Win;

            return EGameResult.Loss;
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
                GameOver(teamCtr[0]);
            }
        }

        public static void RefundGame()
        {
            switch (LobbyHandler.Instance.GameMode)
            {
                case EGameMode.Arena:
                    ErrorHandler.Log("RefundGame() : Loading Arena Data : " + PlayerPrefsHandler.GetArenaType().ToString(), ELogTag.GameSystem);
                    ProgressionCloudData.AddArenaLoss(-1, true);
                    break;

                case EGameMode.Ranked:
                    ErrorHandler.Log("RefundGame() : Ranked game", ELogTag.GameSystem);
                    ProgressionCloudData.UpdateLeagueValue(true);
                    break;

                // no progression on training game
                case EGameMode.Training:
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + LobbyHandler.Instance.GameMode);
                    break;
            }
        }

        #endregion


        #region Music & Sound

        [ClientRpc]
        public void PlaySoundClientRPC(string spellName, ESpellEvent spellAction)
        {
            var spellData = SpellLoader.GetSpellData(spellName, destroy: true);
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
        public void PlayCastSoundClientRPC(string spellName)
        {
            var spellData = SpellLoader.GetSpellData(spellName, destroy: true);
            AudioClip audioClip = spellData.CastSoundFX != null ? spellData.CastSoundFX : SoundFXManager.DefaultCastSoundFX;
            SoundFXManager.PlayOnce(audioClip);
        }

        [ClientRpc]
        public void PlayCastWaveSoundClientRPC(string spellName)
        {
            try
            {
                var spellData = (MultiProjectilesData)SpellLoader.GetSpellData(spellName, destroy: true);
                if (spellData.OnCastWaveSoundFX == null)
                    return;
                SoundFXManager.PlayOnce(spellData.OnCastWaveSoundFX);
            }
            catch
            {
                ErrorHandler.Error("Unable to convert " + spellName + " as MultiProjectilesData");
            }
        }

        [ClientRpc]
        public void PlayCastProjectileSoundClientRPC(string spellName)
        {
            try
            {
                var spellData = (MultiProjectilesData)SpellLoader.GetSpellData(spellName, destroy: true);
                if (spellData.OnCastProjectileSoundFX == null)
                    return;
                SoundFXManager.PlayOnce(spellData.OnCastProjectileSoundFX);
            } catch 
            {
                ErrorHandler.Error("Unable to convert " + spellName + " as MultiProjectilesData");
            }
        }

        [ClientRpc]
        void PlayStateMusicClientRPC(EGameState state)
        {
            string context = "";
            if (LobbyHandler.Instance.GameMode == EGameMode.Arena && ProgressionCloudData.CurrentArena.IsBoss())
                context = "Boss";

            SoundFXManager.PlayGameStateMusic(state, LobbyHandler.Instance.GameMode, context);
        }

        #endregion


        #region Target

        public Controller GetPlayer(ulong clientId)
        {
            if (m_Controllers.ContainsKey(clientId))
                return m_Controllers[clientId];

            if (m_Spawns.ContainsKey(clientId))
                return m_Spawns[clientId];

            ErrorHandler.Error("Unable to find controller with client id : " + clientId);
            return null;
        }

        public bool IsOnlySpawnEnemies(int team)
        {
            if (m_Controllers.Values.Any(t => t != null && t.IsPlayer && t.Team != team && t.IsTargettable))
                return false;

            return m_Spawns.Values.Any(t => t != null && t.Team != team && t.IsTargettable);
        }

        public Controller GetFirstEnemy(int team)
        {
            Controller returnedController = null;

            var controllers = GetAllTauntingEnemies(team, spawnIncluded: true);
            if (controllers.Count > 0)
                return controllers[0];

            foreach (Controller controller in m_Controllers.Values)
            {
                // CHECK : controller null or deactivated
                if (controller == null || ! controller.gameObject.activeInHierarchy || ! controller.IsActive)
                    continue;

                // CHECK : same team
                if (controller.Team == team)
                    continue;

                // CHECK : no alive
                if (!controller.Life.IsAlive)
                    continue;

                // return this controller if can be targetted
                if (! controller.StateHandler.IsUnTargetable)
                    return controller;

                // save this as current returned controller but keep looking for a better fit
                returnedController = controller;
            }

            // get first targetable spawn
            var spawnController = GetFirstSpawn(team, ally: false);
            if (spawnController != null && ! spawnController.StateHandler.IsUnTargetable) 
            {
                returnedController = spawnController;
            }

            return returnedController;
        }

        public Controller GetFirstAlly(int team, ulong slefId)
        {
            // find first non self ally
            foreach (Controller controller in m_Controllers.Values)
                if (controller.Team == team && controller.PlayerId != slefId)
                    return controller;

            // none found -> return self
            return GetPlayer(slefId);
        }

        public List<Controller> GetAllEnemies(int team, bool spawnIncluded = true)
        {
            var controllers = m_Controllers.Values.Where(controller => controller.Team != team).ToList();
            if (spawnIncluded)
                controllers.AddRange(m_Spawns.Values.Where(controller => controller.Team != team).ToList());

            return controllers;
        }

        public List<Controller> GetAllTauntingEnemies(int team, bool spawnIncluded = true)
        {
            var controllers = GetAllEnemies(team, spawnIncluded);
            if (controllers.Count == 0) 
                return controllers;

            return controllers.Where(controller => controller.StateHandler.IsTaunting && ! controller.StateHandler.IsUnTargetable).ToList();
        }

        public List<Controller> GetAllAllies(int team)
        {
            return m_Controllers.Values.Where(controller => controller.Team == team).ToList();
        }

        public bool HasPlayer(ulong clientId)
        {
            return GetPlayer(clientId) != null;
        }

        public List<Controller> GetAllSpawns(int team, bool ally = false)
        {
            return m_Spawns.Values.Where(controller => ally == (controller.Team == team)).ToList();
        }

        public Controller GetFirstSpawn(int team, bool ally = false)
        {
            var spawns = GetAllSpawns(team, ally);
            if (spawns == null || spawns.Count == 0)
                return null;

            return spawns[0];
        }

        public bool TryFindSpellInArena(string spellName, out Spell spell, Controller controller = null)
        {
            spell = null;

            // Finds all active Spell components in the scene
            List<Spell> spells = FindObjectsByType<Spell>(FindObjectsSortMode.InstanceID).ToList();
            spells = spells
                .Where(s => s.SpellData != null && s.SpellData.Name == spellName)
                .ToList();

            if (spells.Count == 0)
                return false;

            if (controller == null)
            {
                spell = spells.First(); 
                return true;
            }

            foreach (Spell tempSpell in spells)
            {
                if (tempSpell.Caster == controller)
                {
                    spell = tempSpell;
                    return true;
                }
            }

            return false;
        }


        #endregion


        #region State Manipulators

        /// <summary>
        /// Set the state of the game
        /// </summary>
        /// <param name="state"></param>
        public void SetState(EGameState state)
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
            // Host already had the event spawned by server
            if (IsHost)
                return;

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
                }

                return s_Instance;
            }
        }

        public static bool FindInstance(bool checkSpawned = false)
        {
            var instance = s_Instance;
            if (instance == null)
                instance = FindAnyObjectByType<GameManager>();

            if (instance == null)
                return false;

            if (checkSpawned && !instance.IsSpawned)
                return false;

            s_Instance = instance;
            return true;
        }

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

        }

        void OnStateValueChanged(EGameState oldValue, EGameState newState)
        {
            ErrorHandler.Log("New state : " + newState, ELogTag.GameSystem);

            switch (newState)
            {
                case EGameState.WaitingForConnection:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 60f, OnPreparingGameTimeLimit);
                    break;

                case EGameState.PreparingGame:
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 90f, OnPreparingGameTimeLimit);
                    SetUpTimer();
                    StartCoroutine(WaitClientInitialized());
                    SpawnPlayers();
                    break;

                case EGameState.Intro:
                    float timer = ProfileCloudData.TutoDone ? 45f : 300f;
                    TimeErrorWrapper.Instance.New(TIME_WRAPPER_ID, 45f, OnGameRunningTimeLimit);
                    StartIntro();
                    break;

                case EGameState.GameRunning:
                    var controller = m_Controllers.ElementAt(1).Value;
                    // initialize BT debugger if enemy is bot
                    if (!controller.IsPlayer)
                    {
                        if (IsServer && ProfileCloudData.IsAdmin && PlayerPrefsHandler.GetDebug(EDebugOption.DebugBots))
                        {
                            GameUIManager.BTDebugger.Initialize(controller);
                            GameUIManager.BTDebugger.gameObject.SetActive(true);
                        }
                    }

                    TimeErrorWrapper.Instance.Cancel(TIME_WRAPPER_ID);
                    break;

                case EGameState.GameOver:
                    TimeErrorWrapper.Instance.Cancel(TIME_WRAPPER_ID);
                    break;
            }
        }

        void OnPlayerDied()
        {
            // END of the game is handled by the Tutorial Manager
            if (m_IsTuto)
                return;

            CheckGameEnd();
        }

        public void OnTimerEnd()
        {
            if (!IsServer)
                return;

            GameOver(-1);
        }

        #endregion


        #region Error Management

        public static void ExitWithError(string message, bool refund = true)
        {
            // set up error Message display
            ErrorHandler.Error(message);
            Main.AddStoredEvent(EAppState.MainMenu, () => Main.SetPopUp(EPopUpState.MessagePopUp, message));

            // check if game has started and needs refund
            if (refund && GameUIManager.Instance.PreventiveLossApplied)
                RefundGame();

            // shutdown GameManager
            if (Instance != null)
                GameManager.Instance.Shutdown();

            // exit Lobby
            if (LobbyHandler.Instance != null && LobbyHandler.Instance.IsActive) 
                LobbyHandler.Instance.LeaveLobby();
            
            // load MainMenuy scene
            SceneLoader.Instance.LoadScene("MainMenu");
        }

        void OnInitializingTimeLimit()
        {
            ExitWithError(
                "An error has occured while creating " + LobbyHandler.Instance.GameMode.ToString() + " game mode : "
                    + "\n   + Game State : " + m_State.Value.ToString()
                    + "\n   + Reason : Initializing game has reached time limit"
            );
        }

        void OnPreparingGameTimeLimit()
        {
            ExitWithError(
                "An error has occured while creating " + LobbyHandler.Instance.GameMode.ToString() + " game mode : "
                    + "\n   + Game State : " + m_State.Value.ToString()
                    + "\n   + Reason : Preparing game has reached time limit"
            );
        }

        void OnGameRunningTimeLimit()
        {
            ExitWithError(
                "An error has occured while playing " + LobbyHandler.Instance.GameMode.ToString() + " game mode : "
                    + "\n   + Game State : " + m_State.Value.ToString()
                    + "\n   + Reason : Game has reached its safety time limit"
            );
        }

        void OnGameOverTimeLimit()
        {
            SceneLoader.Instance.LoadScene("MainMenu");
        }


        #endregion


        #region Debug Callbacks

        public bool CheckSpecialCommands(string command)
        {
            if (CheckAddStat(command))
                return true;

            if (CheckAddStateEffect(command))
                return true;

            return false;
        }

        [Command(KeyCode.N)]
        public void AutoWin()
        {
            GameOver(Owner.Team);
        }

        [Command(KeyCode.B)]
        public void AutoLoss()
        {
            GameOver((Owner.Team + 1) % 2);
        }

        [Command(KeyCode.M)]
        public void HitSelf()
        {
            Owner.Life.Hit(500, 999, "Debug", EDamageCategory.Physical, EHitCategory.Direct, true);
        }

        [Command(KeyCode.L)]
        public void Hit()
        {
            GetFirstEnemy(Owner.Team).Life.Hit(1000, 0, "Debug", EDamageCategory.Physical, EHitCategory.Direct, true);
        }

        /// <summary>
        /// Toggle activation of AIs (for tests)
        /// </summary>
        [Command(KeyCode.P)]
        public void ToggleEnemy()
        {
            foreach (Controller controller in m_Controllers.Values)
            {
                // skip self
                if (controller.PlayerId == Owner.PlayerId)
                    continue;

                if (controller.IsPlayer)
                {
                    controller.AutoAttackHandler.Activate(!controller.AutoAttackHandler.isActiveAndEnabled);
                }
                else
                {
                    controller.BehaviorTree.Activate(!controller.BehaviorTree.IsActivated);
                }
            }
        }

        [Command(KeyCode.O)]
        public void ToggleAutoAttack()
        {
            var controller = GetPlayer(Owner.PlayerId);
            controller.AutoAttackHandler.Activate(! controller.AutoAttackHandler.isActiveAndEnabled);
        }

        [Command(KeyCode.I)]
        public void InvulnerableSelf()
        {
            var controller = GetPlayer(Owner.PlayerId);
            if (controller.StateHandler.HasState(EStateEffect.Invulnerable))
                controller.StateHandler.RemoveStateEffect(EStateEffect.Invulnerable);
            else
                controller.StateHandler.AddStateEffect(EStateEffect.Invulnerable.ToString(), Owner, 1, "Debug");
        }

        [Command(KeyCode.U)]
        public void InvulnerableEnemy()
        {
            var controller = GetFirstEnemy(Owner.Team);
            if (controller.StateHandler.HasState(EStateEffect.Invulnerable))
                controller.StateHandler.RemoveStateEffect(EStateEffect.Invulnerable);
            else
                controller.StateHandler.AddStateEffect(EStateEffect.Invulnerable.ToString(), Owner, 1, "Debug");
        }

        [Command(KeyCode.K)]
        public void GiveEnergy()
        {
            GetFirstEnemy(Owner.Team).EnergyHandler.AddEnergy(100);
        }

        [Command(KeyCode.J)]
        public void IncreaseDamage()
        {
            Owner.StateHandler.CharacterData.AddBonusStats(new List<SCharacterStatScaling>() { 
                new SCharacterStatScaling(EStateEffectProperty.BonusDamage, 100f, 0f, 0f) 
            });
        }

        [Command(KeyCode.T)]
        public void StunEnemy()
        {
            var stun = new SStateEffectData(EStateEffect.Stun, overridingProperties: new List<SStateEffectProperty>() { new SStateEffectProperty(EStateEffectProperty.Duration, 3) });
            GetFirstEnemy(Owner.Team).StateHandler.AddStateEffect(stun, Owner, 1, "Debug");
        }

        [Command(KeyCode.Y)]
        public void ToogleInterface()
        {
            GameUIManager.Instance.ToogleInterface();
        }

        [Command(KeyCode.Space)]
        public void Recharge()
        {
            Owner.EnergyHandler.AddEnergy(100);
            Owner.SpellHandler.ResetCooldowns();
        }

        /// <summary>
        /// Add a state effect on a Target : 
        ///     command: {StateEffect} -t {Target} -l {Level}
        ///     
        /// - {StateEffect} : (string)  name of a EStateEffect
        /// - {Target}      : (string)  "e" for enemy, "s" for self
        /// - {Level}       : (int)     level of the effect
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool CheckAddStateEffect(string command)
        {
            // Regex : nom de l’effet, puis options -t et -l
            Regex regex = new Regex(
                @"^(?<effect>\w+)(?:\s+-t\s+(?<target>[se]))?(?:\s+-l\s+(?<level>\d+))?",
                RegexOptions.IgnoreCase
            );

            Match match = regex.Match(command);
            if (!match.Success)
            {
                return false;
            }

            // Nom de l’effet
            string effectName = match.Groups["effect"].Value;

            if (!Enum.TryParse(effectName, true, out EStateEffect effect))
            {
                return false;
            }

            // Target
            EStateEffectTarget target = EStateEffectTarget.Self;
            if (match.Groups["target"].Success)
            {
                target = match.Groups["target"].Value.ToLower() == "e"
                    ? EStateEffectTarget.Enemy
                    : EStateEffectTarget.Self;
            }

            // get target controller
            Controller targetController = TargetHelper.GetTargetController(Owner.PlayerId, target);
            if (targetController == null)
                return false;

            // Level
            int level = 0;
            if (match.Groups["level"].Success)
            {
                level = int.Parse(match.Groups["level"].Value);
            }

            targetController.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(effect.ToString(), level), Owner);
            return true;
        }

        /// <summary>
        /// Parses a command string and applies a bonus stat to a target controller.
        /// 
        /// Example command:
        ///     "+10 BonusDamage -t s -dc Magical,Physic -hc Direct,Dot -sc Fireball"
        /// 
        /// Supported tokens:
        ///   + / -         → sign of the value
        ///   <number>      → numeric value (float)
        ///   <stat>        → stat name (EStateEffectProperty)
        ///   -t <s|e>      → target (Self or Enemy)
        ///   -dc <list>    → DamageCategories (comma separated)
        ///   -hc <list>    → HitCategories (comma separated)
        ///   -sc <list>    → SpecialConditions (comma separated)
        /// </summary>
        /// <param name="command">Full string command to parse</param>
        /// <returns>True if the stat was successfully added; false otherwise</returns>
        public bool CheckAddStat(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // Regex to parse sign, value, stat, and optional parameters
            Regex regex = new Regex(
                @"^(?<sign>[+-])(?<value>\d+(?:[.,]\d+)?)\s+(?<stat>[A-Za-z0-9_]+)" +
                @"(?:\s+-t\s+(?<target>[se]))?" +
                @"(?:\s+-dc\s+(?<damageCats>[A-Za-z0-9_,]+))?" +
                @"(?:\s+-hc\s+(?<hitCats>[A-Za-z0-9_,]+))?" +
                @"(?:\s+-sc\s+(?<specialConds>[A-Za-z0-9_,]+))?",
                RegexOptions.IgnoreCase
            );

            Match match = regex.Match(command);
            if (!match.Success)
            {
                Debug.LogWarning($"Invalid AddStat command: {command}");
                return false;
            }

            // --- Extract sign and numeric value
            string sign = match.Groups["sign"].Value;
            if (!float.TryParse(match.Groups["value"].Value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                Debug.LogError($"Invalid numeric value in command: {command}");
                return false;
            }

            if (sign == "-") value *= -1f;

            // --- Extract stat property
            string statName = match.Groups["stat"].Value;
            if (!Enum.TryParse(statName, true, out EStateEffectProperty stat))
            {
                Debug.LogError($"Unknown stat '{statName}'. Must match EStateEffectProperty.");
                return false;
            }

            // --- Target (default: Self)
            EStateEffectTarget target = EStateEffectTarget.Self;
            if (match.Groups["target"].Success)
            {
                string targetValue = match.Groups["target"].Value.ToLower();
                target = targetValue == "e" ? EStateEffectTarget.Enemy : EStateEffectTarget.Self;
            }

            // --- Damage categories
            List<EDamageCategory> damageCats = null;
            if (match.Groups["damageCats"].Success)
            {
                damageCats = match.Groups["damageCats"].Value
                    .Split(',')
                    .Select(x => Enum.TryParse(x.Trim(), true, out EDamageCategory cat) ? cat : default)
                    .Where(c => !EqualityComparer<EDamageCategory>.Default.Equals(c, default))
                    .ToList();
            }

            // --- Hit categories
            List<EHitCategory> hitCats = null;
            if (match.Groups["hitCats"].Success)
            {
                hitCats = match.Groups["hitCats"].Value
                    .Split(',')
                    .Select(x => Enum.TryParse(x.Trim(), true, out EHitCategory cat) ? cat : default)
                    .Where(c => !EqualityComparer<EHitCategory>.Default.Equals(c, default))
                    .ToList();
            }

            // --- Special conditions
            List<string> specialConds = null;
            if (match.Groups["specialConds"].Success)
            {
                specialConds = match.Groups["specialConds"].Value
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();
            }

            // --- Retrieve the correct target controller
            Controller targetController = TargetHelper.GetTargetController(Owner.PlayerId, target);
            if (targetController == null)
            {
                Debug.LogWarning($"No valid target found for AddStat (target={target})");
                return false;
            }

            // --- Apply the bonus stat using the improved AddBonusStat signature
            targetController.StateHandler.CharacterData.AddBonusStat(stat, value, damageCats, hitCats, specialConds);
            targetController.StateHandler.RecalculateBonus();

            // ✅ Log success
            Debug.Log($"[AddStat] {value} {stat} | Target={target} | DamageCats={string.Join(",", damageCats ?? new())} | HitCats={string.Join(",", hitCats ?? new())} | Special={string.Join(",", specialConds ?? new())}");
            return true;
        }

        #endregion

    }
}
