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
using Save.RSDs;
using Assets.Scripts.Tools;
using Data.DataStructures;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets
{
    public class Main : MonoBehaviour
    {
        #region Members

        static Main s_Instance;

        // ==========================================================================================================
        // SERIALIZED MEMBERS
        [SerializeField] Canvas                 m_Canvas;
        [SerializeField] CloudSaveManager       m_CloudSaveManager;
        [SerializeField] LeagueDataConfig       m_LeagueDataConfig;
        [SerializeField] bool                   m_ActivateSaveOnClose; 
        [SerializeField] List<ELogTag>          m_LogTags;

        // ==========================================================================================================
        // EVENTS
        public static event Action<EAppState>   StateChangedEvent;
        public static event Action              InitializationCompletedEvent;
        public static event Action              ApplicationQuitEvent;

        // ==========================================================================================================
        // PRIVATE MEMBERS
        EAppState                               m_State                 = EAppState.Release;
        bool                                    m_SignedIn              = false;
        /// <summary> Events to store until reaching a specific AppState </summary>
        Dictionary<EAppState, List<Action>>     m_StoredEvents          = new();

        // ==========================================================================================================
        // PUBLIC DEPENDENT STATIC MEMBERS
        public static Main Instance                             => s_Instance;
        public static CloudSaveManager CloudSaveManager         => Instance.m_CloudSaveManager;
        public static LeagueDataConfig LeagueDataConfig         => Instance.m_LeagueDataConfig;
        public static EAppState State                           => Instance.m_State;
        public static Canvas Canvas                             => Instance.m_Canvas;
        public static bool ActivateSaveOnClose                  => Instance.m_ActivateSaveOnClose;
        public static List<ELogTag> LogTags                     => s_Instance != null ? Instance.m_LogTags : new List<ELogTag>();

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
            try
            {
                // initialize Managers & Loaders
                PlayerPrefsHandler.Initialize();
                AchievementLoader.Initialize();
                ItemLoader.Initialize();
                RSDManager.Intialize();

                // init settings
                InitializeSettings();

                // register to state changes 
                StateChangedEvent += OnStateChanged;

                // register on initliazitation completed check by the Coroutine
                InitializationCompletedEvent += OnInitializationCompleted;

                StartCoroutine(CheckInitialization());

                // initialize Unity Services
                var options = new InitializationOptions();
                options.SetEnvironmentName("dev");
                await UnityServices.InitializeAsync(options);

                // listen to Auth Service and try to signe in anonymously
                AuthenticationService.Instance.SignedIn += OnSignedIn;
                AuthenticationService.Instance.SignedIn += m_CloudSaveManager.LoadSave;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            }
            catch (Exception ex)
            {
                ErrorHandler.Error(ex.Message);
            }

#if UNITY_EDITOR
            ErrorHandler.Log("UNITY EDITOR MODE", ELogTag.System);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            ApplicationQuitEvent += m_CloudSaveManager.OnApplicationQuit;
#endif
        }

        /// <summary>
        /// Coroutine checking that avery component of the app are propertly loaded
        /// </summary>
        /// <returns></returns>
        IEnumerator CheckInitialization()
        {
            float timer = 30f; // set a timer of 30s to avoid inf loop
            while (
                CharacterLoader.Instance            == null
                || TimeErrorWrapper.Instance        == null
                || SpellLoader.Instance             == null
                || SceneLoader.Instance             == null
                || ItemLoader.ChestRewardData       == null
                || AchievementLoader.Achievements   == null
                || LobbyHandler.Instance            == null
                || ! RelayHandler.Initialized
                || ! RSDManager.LoadingCompleted
                || ! m_CloudSaveManager.LoadingCompleted
                || ! m_SignedIn
            )
            {
                timer -= Time.deltaTime;

                if (timer <= 0)
                    ErrorHandler.FatalError("Unable to initilaize the App in less than 30 seconds");

                yield return null;
            }

            InitializationCompletedEvent?.Invoke();
        }

        void InitializeSettings()
        {
            QualitySettings.vSyncCount = 0;         // Disable V-Sync
            Application.targetFrameRate = 120;      // Set desired frame rate
        }

        #endregion


        #region State Management

        public static void SetState(EAppState state)
        {
            if (state == Instance.m_State) 
                return;

            Instance.m_State = state;
            StateChangedEvent?.Invoke(state);
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
            var popUpPath = "";
            if (popUpState.ToString().EndsWith("Screen"))
            {
                popUpPath = AssetLoader.c_OverlayPath;
            } else if (popUpState.ToString().EndsWith("PopUp"))
            {
                popUpPath = AssetLoader.c_PopUpsPath;
            } else
            {
                ErrorHandler.Error("Unknown PopUp type " + popUpState.ToString() + " : unable to find adequate path");
            }

            // instantiate object in the canvas
            var obj = Instantiate(AssetLoader.Load<GameObject>(popUpPath + popUpState.ToString()));
            if (obj == null)
            {
                ErrorHandler.Error("Unable to find popup : " + popUpState.ToString());
                return;
            }

            // setup initalization depending on the popup state
            switch (popUpState) {

                // MESSAGE POP UPS -------------------------------------------------------
                case EPopUpState.MessagePopUp:
                    obj.GetComponent<MessagePopUp>().Initialize((string)args[0], args.Count() > 1 ? (string)args[1] : "");
                    break;

                case EPopUpState.ConfirmBuyPopUp:
                    obj.GetComponent<ConfirmBuyPopUp>().Initialize((SPriceData)args[0], (SRewardsData)args[1], (Action)args[2], (Action)args[3]);
                    break;

                case EPopUpState.ConfirmBuyItemPopUp:
                    obj.GetComponent<ConfirmBuyItemPopUp>().Initialize((SPriceData)args[0], (Enum)args[1], (int)args[2], (Action)args[3], (Action)args[4]);
                    break;

                case EPopUpState.ConfirmBuyBundlePopUp:
                    obj.GetComponent<ConfirmBuyBundlePopUp>().Initialize((SPriceData)args[0], (SRewardsData)args[1], (Action)args[2], (Action)args[3]);
                    break;


                // SCREENS -------------------------------------------------------
                case EPopUpState.RewardsScreen:
                    obj.GetComponent<RewardsScreen>().Initialize((SRewardsData)args[0], (string)args[1], args.Length > 2 ? (Action)args[2] : null);
                    break;

                case EPopUpState.AchievementRewardScreen:
                    obj.GetComponent<AchievementRewardScreen>().Initialize((List<SAchievementReward>)args[0]);
                    break;

                case EPopUpState.ArenaPathScreen:
                    obj.GetComponent<ArenaPathScreen>().Initialize((EArenaType)args[0]);
                    break;

                case EPopUpState.LevelUpScreen:
                    obj.GetComponent<LevelUpScreen>().Initialize((ECharacter)args[0]);
                    break;

                // INFO POP UPS -------------------------------------------------------
                case EPopUpState.SpellInfoPopUp:
                    bool infoOnly = args.Length > 2 && (bool)args[2];
                    obj.GetComponent<SpellInfoPopUp>().Initialize((ESpell)args[0], (int)args[1], infoOnly);
                    break;

                case EPopUpState.CollectableInfoPopUp:
                case EPopUpState.CharacterInfoPopUp:
                    obj.GetComponent<CollectableInfoPopUp>().Initialize((ECharacter)args[0], (int)args[1]);
                    break;

                case EPopUpState.StateEffectPopUp:
                    obj.GetComponent<StateEffectPopUp>().Initialize((SStateEffectData)args[0], (int)args[1]);
                    break;

                case EPopUpState.RuneSelectionPopUp:
                    obj.GetComponent<RuneSelectionPopUp>().Initialize();
                    break;

                case EPopUpState.TriggerEffectPopUp:
                    obj.GetComponent<TriggerEffectPopUp>().Initialize((STriggerEffect)args[0]);
                    break;

                // SETTINGS & OPTIONS -------------------------------------------------------
                case EPopUpState.SettingsPopUp:
                    obj.GetComponent<SettingsPopUp>().Initialize();
                    break;

                default:
                    obj.GetComponent<OverlayScreen>().Initialize();
                    break;
            }
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

        public static void DisplayRewards(SRewardsData rewardsData, ERewardContext context, Action OnRewardCollected = null)
        {
            Main.SetPopUp(EPopUpState.RewardsScreen, rewardsData, context.ToString(), OnRewardCollected);
        }
     
        public static void DisplayAchievementRewards(List<SAchievementReward> rewardsData)
        {
            Main.SetPopUp(EPopUpState.AchievementRewardScreen, rewardsData);
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
            ConfirmBuyRewards(priceData, rewardsData, OnPurchase);
        }

        /// <summary>
        /// Confirm purchase of an item or a bundle of items (currency, chests, collectables)
        /// </summary>
        /// <param name="priceData"></param>
        /// <param name="rewardsData"></param>
        /// <param name="OnPurchase"></param>
        public static void ConfirmBuyRewards(SPriceData priceData, SRewardsData rewardsData, Action<bool> OnPurchase)
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

            Main.SetPopUp(EPopUpState.ConfirmBuyBundlePopUp, priceData, rewardsData, onValidate, onCancel);
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
            if (ProfileCloudData.PseudoChanged)
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
            // pseudo not changed : authorize access to pseudo popup
            if (!ProfileCloudData.PseudoChanged)
                return true;

            return TokensRSD.IsTokenAuthorized(ProfileCloudData.Token);
        }

        #endregion


        #region Listeners

        void OnSignedIn()
        {
            ErrorHandler.Log("SIGNED ID : " + AuthenticationService.Instance.PlayerId, ELogTag.System);
            m_SignedIn = true;

            // setup analytics
            MAnalytics.Initialize();
        }

        private void OnInitializationCompleted()
        {
            if (SceneLoader.Instance == null)
                ErrorHandler.Log("SceneLoader is null", ELogTag.System);

            // at the end of the initialization - check if PseudoPopUp should be displayed
            CheckPseudoPopUp();

            // loading MainMenu
            if (AuthorizeAccess())
            {
                ErrorHandler.Log("Initialization of the data completed : loading MainMenu", ELogTag.System);

                // check that region has been provided
                CheckRegion();

                // check if a current message needs to be dislayed to the user before loading the scene 
                CheckCurrentMessage();

                SceneLoader.Instance.LoadScene("MainMenu");
            } 
            else
            {
                Main.SetPopUp(EPopUpState.MessagePopUp, "Unauthorized Access");
            }
            
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
                ApplicationQuitEvent?.Invoke();
        }
#endif
        #endregion
    }
}