using UnityEngine;
using UnityEngine.Advertisements;


namespace Managers.Ads
{
    public enum EPlatform
    {
        Android,
        IOS,
    }


    public class AdManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        public static AdManager Instance;

        [Header("Unity Ads Settings")]
        [SerializeField] private string m_IosGameId     = "5886864";
        [SerializeField] private string m_AndroidGameId = "5886865";
        [SerializeField] private bool m_IsTestMode      = true;

        [Header("Placement IDs")]
        [SerializeField] private string m_InterstitalIOSId          = "Interstitial_iOS";
        [SerializeField] private string m_RewardedIOSId             = "Rewarded_iOS";
        [SerializeField] private string m_InterstitalsAndroidId     = "Interstitial_Android";
        [SerializeField] private string m_RewardedAndroidId         = "Rewarded_Android";

        // Local data
        private EPlatform m_Platform;
        private System.Action m_OnRewardedSuccessCallback;
        
        private string m_GameId => m_Platform == EPlatform.Android ? m_AndroidGameId : m_IosGameId;
        private string m_InterstitialId => m_Platform == EPlatform.Android ? m_InterstitalsAndroidId : m_InterstitalIOSId;
        private string m_RewardedId => m_Platform == EPlatform.Android ? m_RewardedAndroidId : m_RewardedIOSId;


        private void Awake()
        {
            // singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // create Instance of the object
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // start initialization
            InitializeAds();
        }

        private void InitializeAds()
        {
#if UNITY_IOS
        m_Platform = EPlatform.IOS;
#else
            m_Platform = EPlatform.Android;
#endif

            Advertisement.Initialize(m_GameId, m_IsTestMode, this);
        }

        #region Public Ad Methods

        public void ShowInterstitial(System.Action onSuccess)
        {
            m_OnRewardedSuccessCallback = onSuccess;
            Advertisement.Show(m_InterstitialId, this);
        }

        public void ShowRewarded(System.Action onSuccess)
        {
            m_OnRewardedSuccessCallback = onSuccess;
            Advertisement.Show(m_RewardedId, this);
        }

        #endregion

        #region Unity Ads Callbacks

        public void OnInitializationComplete()
        {
            Debug.Log("Unity Ads initialization complete.");
            Advertisement.Load(m_InterstitialId, this);
            Advertisement.Load(m_RewardedId, this);
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            if (placementId == m_RewardedId && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("Rewarded ad completed successfully.");
                m_OnRewardedSuccessCallback?.Invoke();
            }
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogError($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            Debug.Log($"Ad Loaded: {placementId}");
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogError($"Ad Load Failed ({placementId}): {error} - {message}");
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.LogError($"Ad Show Failed ({placementId}): {error} - {message}");
        }

        public void OnUnityAdsShowStart(string placementId) { }

        public void OnUnityAdsShowClick(string placementId) { }

        #endregion
    }
}
