using Assets.Scripts.Network;
using Enums;
using Game.Loaders;
using Menu.PopUps;
using Save;
using System;
using System.Collections;
using System.Threading.Tasks;
using Tools;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Data;
using System.Linq;
using Data.GameManagement;
using System.Collections.Generic;
using Scripts.Menu.PopUps;
using Unity.Services.Core.Environments;
using Network;
using Assets.Scripts.Tools;
using Data.DataStructures;
using Assets.Scripts.Managers;
using Managers.Friends;
using Menu.PopUps.PopUps.MessagePopUps;
using Unity.Services.Friends.Models;
using UnityEngine.SceneManagement;
using Menu.PopUps.OverlayScreens;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets
{
    public class Main : MonoBehaviour
    {
        #region Members

        static Main s_Instance;
        static int NReloading;

        public const int MAX_RELOADING = 0;

        // ==========================================================================================================
        // SERIALIZED MEMBERS
        [SerializeField] Canvas m_Canvas;
        [SerializeField] CloudSaveManager m_CloudSaveManager;
        [SerializeField] LeagueDataConfig m_LeagueDataConfig;
        [SerializeField] bool m_ActivateSaveOnClose;

        [Header("Debug Section")]
        [SerializeField] bool m_ForceIsNewPlayer;
        [SerializeField] bool m_StopPreventiveLoss;
        [SerializeField] List<ELogTag> m_LogTags;

        // ==========================================================================================================
        // EVENTS
        public static event Action<EAppState> StateChangedEvent;
        public static event Action InitializationCompletedEvent;
        public static event Action ApplicationQuitEvent;

        // ==========================================================================================================
        // PRIVATE MEMBERS
        EAppState m_State = EAppState.Release;
        bool m_SignedIn = false;
        /// <summary> Events to store until reaching a specific AppState </summary>
        Dictionary<EAppState, List<Action>> m_StoredEvents = new();

        // ==========================================================================================================
        // PUBLIC DEPENDENT STATIC MEMBERS
        public static Main              Instance                => s_Instance;
        public static CloudSaveManager  CloudSaveManager        => Instance.m_CloudSaveManager;
        public static LeagueDataConfig  LeagueDataConfig        => Instance.m_LeagueDataConfig;
        public static EAppState         State                   => Instance.m_State;
        public static Canvas            Canvas                  => Instance.m_Canvas;
        public static bool              ActivateSaveOnClose     => Instance.m_ActivateSaveOnClose;
        public static bool              ForceIsNewPlayer        => Instance.m_ForceIsNewPlayer;
        public static bool              IsNewPlayer             => ForceIsNewPlayer || !ProfileCloudData.TutoDone;
        public static List<ELogTag>     LogTags                 => s_Instance != null ? Instance.m_LogTags : new List<ELogTag>();
        public static bool StopPreventiveLoss
        {
            get
            {
#if UNITY_EDITOR
                // only works in EDITOR mode
                return Instance.m_StopPreventiveLoss;
#else
                return false;
#endif
            }
        }

#endregion


        #region Initialization 

        // Use this for initialization
        async void Awake()
        {
            s_Instance = this;

            await Initialize();

            DontDestroyOnLoad(this);
            DontDestroyOnLoad(m_Canvas);
        }

        async Task Initialize()
        {
            Debug.Log("Initialize");

            try
            {
                ErrorHandler.IsActivated = true;
                
                // initialize Managers & Loaders
                PlayerPrefsHandler.Initialize();
                AchievementLoader.Initialize();
                SpellLoader.Initialize();
                ItemLoader.Initialize();
                RSDManager.Intialize();

                // init settings
                Debug.Log("InitializeSettings");
                InitializeSettings();

                // register to state changes 
                StateChangedEvent += OnStateChanged;

                // register on initliazitation completed check by the Coroutine
                InitializationCompletedEvent += OnInitializationCompleted;

                StartCoroutine(CheckInitialization());

                // initialize Unity Services 
                var options = new InitializationOptions();
#if UNITY_EDITOR
                options.SetEnvironmentName("beta");
#else
                options.SetEnvironmentName("dev");
#endif
                await UnityServices.InitializeAsync(options);

                // listen to Auth Service and try to signe in anonymously
                if (! AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignedIn += OnSignedIn;
                    AuthenticationService.Instance.SignedIn += m_CloudSaveManager.LoadSave;
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                } else
                {
                    m_CloudSaveManager.LoadSave();
                    OnSignedIn();
                }
            }

            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                Instance.ReloadGame();
                return;
            }

#if UNITY_EDITOR
            ErrorHandler.Log("UNITY EDITOR MODE", ELogTag.System);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ApplicationQuitEvent += m_CloudSaveManager.OnApplicationQuit;
#endif
        }

        List<bool> InitializedElements => new List<bool>()
        {
            CharacterLoader.Instance            != null,
            TimeErrorWrapper.Instance           != null,
            SceneLoader.Instance                != null,
            LootManagementData.Instance         != null,
            ItemLoader.ChestRewardData          != null,
            AchievementLoader.Achievements      != null,
            LobbyHandler.Instance               != null,
            SpellLoader.Initialized,
            RelayHandler.Initialized,
            FriendsHandler.Initialized,
            RSDManager.LoadingCompleted,
            m_CloudSaveManager.LoadingCompleted,
            m_SignedIn
        };

        /// <summary>
        /// Calculate percentage of initialization based on number of elements initialized
        /// </summary>
        /// <returns></returns>
        float CalculateInitializationPercentage()
        {
            List<bool> initializedElements = InitializedElements;
            if (initializedElements.Count == 0)
            {
                ErrorHandler.Error("InitializedElements count is 0");
                ReloadGame();
                return 0;  // Avoid division by zero
            }

            return Mathf.Round(100 * (float)initializedElements.Count(element => element) / initializedElements.Count) / 100;
        }

        /// <summary>
        /// Coroutine checking that avery component of the app are propertly loaded
        /// </summary>
        /// <returns></returns>
        IEnumerator CheckInitialization()
        {
            ErrorHandler.Log("CheckInitialization()");

            // set a timer of 30s to avoid inf loop
            if (TimeErrorWrapper.Instance != null)
                TimeErrorWrapper.Instance.New("App Initialization", 20f, ReloadGame);      

            float percInit = 0f;    // init percentage of initialization
            do
            {
                float newPercInit = CalculateInitializationPercentage();
                if (newPercInit == percInit)
                {
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                if (newPercInit < percInit)
                    ErrorHandler.Warning("Percentage of initialization decreased, from " + percInit + " to " + newPercInit);

                percInit = newPercInit;
                SceneLoader.UpdateProgress(percInit, GetInitializationInfoText());
                yield return null;

            } while (percInit < 1);

            SceneLoader.UpdateProgress(1f);
            TimeErrorWrapper.Instance.Cancel("App Initialization");
            InitializationCompletedEvent?.Invoke();
        }

        void InitializeSettings()
        {
            QualitySettings.vSyncCount  = 0;            // Disable V-Sync
            Application.targetFrameRate = 120;          // Set desired frame rate
        }

        void ReloadGame()
        {
            NReloading++;

            Debug.LogWarning("RELOADING - " + NReloading);

            // Stop all background processes if needed
            StopAllCoroutines();

            if (NReloading > MAX_RELOADING)
            {
                Time.timeScale = 0;
                
                if (!ConsoleUI.Instance.gameObject.activeInHierarchy)
                    ConsoleUI.Instance.Hide();

                Debugger.Instance.DisplayErrors();
                //Application.Quit();
                return;
            }

            // Reload the active scene to restart from scratch
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        string GetInitializationInfoText()
        {
            string infoText = "[";
            foreach (bool isInit in InitializedElements)
            {
                infoText += isInit ? "<color=#00FF11>+</color>" : "<color=#FF0000>x</color>";
            }
            infoText += "]";

            return infoText;
        }

#endregion


        #region State Management

        public static void SetState(EAppState state)
        {
            if (state == Instance.m_State) 
                return;

            UpdateAvailabilityToState(state);

            Instance.m_State = state;
            StateChangedEvent?.Invoke(state);
        }

        /// <summary>
        /// Check Appstate to setup players availibility
        /// </summary>
        /// <param name="state"></param>
        public static async void UpdateAvailabilityToState(EAppState state)
        {
            Availability availability;
            string task = "";
            switch (state)
            {
                // loading screen does not change the current state
                case EAppState.Release:
                case EAppState.LoadingScreen:
                    return;

                case EAppState.MainMenu:
                    availability = Availability.Online;
                    break;

                case EAppState.Lobby:
                case EAppState.InGame:
                    availability = Availability.Busy;
                    task = "In Game";
                    break;

                default:
                    ErrorHandler.Error("Unhandled app state : " + state);
                    return;
            }

            // ===============================================================
            // SPECIAL CASE (remove when this system is replaced by real friends system) 
            // keep people waiting for ranked games as ONLINE
            if (state == EAppState.Lobby && LobbyHandler.Instance.GameMode == EGameMode.Ranked)
                availability = Availability.Online;
            // ===============================================================

            if (FriendsHandler.CurrentActivity != null && availability.ToString() == FriendsHandler.CurrentActivity.Status)
                return;

            await FriendsHandler.Instance.SetPresence(availability, task);
        }

        public static void AddStoredEvent(EAppState state, Action action)
        {
            // state is current state : fire event right away
            if (Instance.m_State == state)
            {
                action?.Invoke();
                return;
            }

            // init if doesnt exists
            if (!Instance.m_StoredEvents.ContainsKey(state))
                Instance.m_StoredEvents.Add(state, new List<Action>());

            // store the event
            Instance.m_StoredEvents[state].Add(action);
        }

        #endregion


        #region PopUp & Screens

        public static void SetPopUp(EPopUpState popUpState, params object[] args)
        {
            ScreenManager.SetPopUp(popUpState, args);
        }

        public static void SetCollectableSelectionPopUp<TEnum>(Action<TEnum> onCollectableClicked, bool unlockedOnly = true)
        {
            var popup = Instantiate(AssetLoader.Load<CollectableSelectionPopUp>(AssetLoader.c_PopUpsPath)); 
            popup.Initialize(typeof(TEnum), (Enum value) =>
            {
            if (!Enum.TryParse(typeof(TEnum), value.ToString(), true, out object result))
                {
                    ErrorHandler.Error("Unable to parse " + value + " as " + typeof(TEnum).ToString());
                    return;
                }
                onCollectableClicked((TEnum)result);
            }, unlockedOnly);
        }

        public static void DisplayRewards(SRewardsData rewardsData, string context, Action OnRewardCollected = null, string title = null)
        {
            Action callback = () => Main.SetPopUp(EPopUpState.RewardsScreen, rewardsData, context, OnRewardCollected, title);
            if (ScreenManager.HasScreen(EPopUpState.RewardsScreen))
            {
                ScreenManager.StoreEvent(EPopUpState.MainMenuScreen, callback);
                return;
            }

            callback?.Invoke();
        }
     
        public static void DisplayAchievementRewards(List<SAchievementReward> rewardsData)
        {
            Main.SetPopUp(EPopUpState.AchievementRewardScreen, rewardsData);
        }

        public static void ConfirmPopUp(string message, string title = "", Action onValidate = null, Action onCancel = null)
        {
            Main.SetPopUp(EPopUpState.ConfirmPopUp, message, title, onValidate, onCancel);
        }

        /// <summary>
        /// Set ConfirmBuyRewards() method for a singular collectable
        /// </summary>
        /// <param name="priceData"></param>
        /// <param name="collectable"></param>
        /// <param name="qty"></param>
        /// <param name="OnPurchase"></param>
        public static void ConfirmBuyCollectable(SPriceData priceData, Enum collectable, int qty, Action<bool> OnPurchase)
        {
            if (! CollectablesManagementData.TryGetCollectableType(collectable, out var collectableType))
                return;

            SRewardsData rewardsData = new SRewardsData(collectableRewards: new List<SCollectableReward>() { new SCollectableReward(collectableType, collectable.ToString(), qty) }) ;
            ConfirmBuyRewards(collectable.ToString(), priceData, rewardsData, OnPurchase);
        }

        /// <summary>
        /// Confirm purchase of an item or a bundle of items (currency, chests, collectables)
        /// </summary>
        /// <param name="priceData"></param>
        /// <param name="rewardsData"></param>
        /// <param name="OnPurchase"></param>
        public static void ConfirmBuyRewards(string itemName, SPriceData priceData, SRewardsData rewardsData, Action<bool> OnPurchase)
        {
            if (rewardsData.Rewards.Count == 0)
            {
                ErrorHandler.Error("Call reward popup with no rewards in list");
                return;
            }

            Action onValidate = () => OnPurchase(true);
            Action onCancel = () => OnPurchase(false);

            if (rewardsData.Rewards.Count == 1 && rewardsData.Collectables != null && rewardsData.Collectables.Count == 1)
            {
                if (! Enum.TryParse(rewardsData.Rewards[0].RewardType, rewardsData.Rewards[0].RewardName, out object item))
                {
                    ErrorHandler.Error("Unable to parse " + rewardsData.Rewards[0].RewardName + " as " + rewardsData.Rewards[0].RewardType);
                    return;
                }

                Main.SetPopUp(EPopUpState.ConfirmBuyItemPopUp, priceData, item, rewardsData.Rewards[0].Qty, onValidate, onCancel);
                return;
            }

            Main.SetPopUp(EPopUpState.ConfirmBuyBundlePopUp, itemName, priceData, rewardsData, onValidate, onCancel);
        }

        public static void SetMessagePopUp(string message, string title = "")
        {
            SetPopUp(EPopUpState.MessagePopUp, message, title);
        }

        public static void ErrorMessagePopUp(string message)
        {
            Debug.LogWarning(message);
            SetPopUp(EPopUpState.MessagePopUp, message);
        }

        public static void StateEffectPopUp(SStateEffectData stateEffectData, int level)
        {
            SetPopUp(EPopUpState.StateEffectPopUp, stateEffectData, level);
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check if pseudo needs to be changed
        /// </summary>
        public static void CheckPseudoPopUp()
        {
            // pseudo already changed : no need to proc the popup
            if (ProfileCloudData.PseudoChanged && ! ProfileCloudData.HasDefaultPseudo)
                return;

            // store the change of the display PseudoPopUp for when the user will reach the MainMenu
            Main.AddStoredEvent(EAppState.MainMenu, () => SetPopUp(EPopUpState.PseudoPopUp));
        }

        public static void CheckRegion()
        {
            if (ProfileCloudData.Region != "")
                return;

            ResetRegion();
        }

        public static async void ResetRegion()
        {
            string region = await RelayHandler.Instance.FindRegion();

            if (region != "")
            {
                ProfileCloudData.SetRegion(region);
                return;
            }

            ErrorHandler.Error("Unable to setup Region for the player");
        }

        public void CheckCurrentMessage()
        {
            // pseudo not changed : this a new player no need to reset
            if (! ProfileCloudData.PseudoChanged)
            {
                PlayerPrefs.SetInt("Message_01", 1);
            }

            // already seen
            if (PlayerPrefs.GetInt("Message_01", 0) == 1)
                return;

            // store the change of the display PseudoPopUp for when the user will reach the MainMenu
            Main.AddStoredEvent(EAppState.MainMenu, () => SetMessagePopUp("Please be inform that your data have been reseted. Checkout the Discord PatchNote for more information.", title: "Your data have been reseted"));

            // reset all data (except token and pseudo)
            string pseudo = ProfileCloudData.GamerTag;
            m_CloudSaveManager.ResetAll();
            ProfileCloudData.SetGamerTag(pseudo);

            PlayerPrefs.SetInt("Message_01", 1);
        }

        bool AuthorizeAccess()
        {
            return true;
            //// pseudo not changed : authorize access to pseudo popup
            //if (!ProfileCloudData.PseudoChanged)
            //    return true;

            //return TokensRSD.IsTokenAuthorized(ProfileCloudData.Token);
        }

        #endregion


        #region Listeners

        void OnSignedIn()
        {
            ErrorHandler.Log("SIGNED ID : " + AuthenticationService.Instance.PlayerId, ELogTag.System);
            m_SignedIn = true;

            // setup analytics
            MAnalytics.Initialize();
            FriendsHandler.Instance.Initialize();
        }

        private void OnInitializationCompleted()
        {
            Debug.Log("OnInitializationCompleted()");

            if (SceneLoader.Instance == null)
                ErrorHandler.Log("SceneLoader is null", ELogTag.System);

            // loading MainMenu
            if (! AuthorizeAccess())
            {
                Main.SetPopUp(EPopUpState.MessagePopUp, "Unauthorized Access");
                return;
            } 

            ErrorHandler.Log("Initialization of the data completed : loading MainMenu", ELogTag.System);

            // at the end of the initialization - check if PseudoPopUp should be displayed
            CheckPseudoPopUp();

            // check that current version matches the last played version for the player (apply changes if needed)
            UpdateManager.CheckUpdates();

            // check that region has been provided
            CheckRegion();

            // check if a current message needs to be displayed to the user before loading the scene 
            CheckCurrentMessage();

            // set error handler active depending on DebugOption settings
            ErrorHandler.IsActivated = PlayerPrefsHandler.GetDebug(EDebugOption.ErrorHandler);

            if (IsNewPlayer)
                LoadTutorial();
            else 
                SceneLoader.Instance.LoadScene("MainMenu");
        }

        private void OnStateChanged(EAppState state)
        {
            ErrorHandler.Log("New state : " + state.ToString(), ELogTag.System);

            if (!m_StoredEvents.ContainsKey(state))
                return;
            
            foreach (Action action in m_StoredEvents[state])
            {
                action?.Invoke();
            }

            m_StoredEvents[state] = new List<Action>();
        }

#if UNITY_EDITOR
        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // call manual event that the application is quitting
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ErrorHandler.IsExiting = true;
                ApplicationQuitEvent?.Invoke();
            }
        }
#endif

        /// <summary>
        /// When new player join the game, directly lead them to the tutorial
        /// </summary>
        async void LoadTutorial()
        {
            LobbyHandler.Instance.GameMode = EGameMode.Training;
            int i = 0;
            do
            {
                bool success = await LobbyHandler.Instance.QuickJoinLobby();
                if (success)
                    return;
            } while (++i < 3);
        }

        #endregion
    }
}