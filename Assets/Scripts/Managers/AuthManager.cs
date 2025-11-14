using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Enums;
using Managers.MainMenu;
using MyBox;
using Save;
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class AuthManager : MonoBehaviour
    {
        #region Members

        // ===================================================================================
        const string KEY_AUTH_SERVICE = "AuthService";
        const string KEY_AUTH_TOKEN = "AuthToken";
        public (string token, EAuthServices authService) DEFAULT_AUTH => ("", EAuthServices.Anonymous);

        // ===================================================================================
        // Events
        public static Action<bool> LoginEvent;
        public static Action<string> LoginFailedEvent;

        // ===================================================================================
        // Instance
        static AuthManager s_Instance;
        public static AuthManager Instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;

                s_Instance = FindAnyObjectByType<AuthManager>();
                if (s_Instance != null)
                    return s_Instance;

                var gameObject = new GameObject("AuthManager");
                s_Instance = gameObject.AddComponent<AuthManager>();
                return s_Instance;
            }
        }

        // ===================================================================================
        // Private Accessors
        IAppleAuthManager m_AppleAuthManager;

        // ===================================================================================
        // Public accessors
        public EAuthServices AuthService
        {
            get
            {
                if (Enum.TryParse(PlayerPrefs.GetString(KEY_AUTH_SERVICE, ""), out EAuthServices authService))
                        return authService;
                return EAuthServices.Anonymous;
            }
        }

        public string   Token           => PlayerPrefs.GetString(KEY_AUTH_TOKEN, "");
        public string   Error           { get; private set; }

        public bool IsLoggedIn
        {
            get
            {
                return ! Token.IsNullOrEmpty() && AuthService != EAuthServices.Anonymous;
            }
        }

        #endregion


        #region SingIn / Login

        public async void SignIn(string authId)
        {
            await SignInAnonymously(false);
        }

        public async void Login()
        {
            await PlayerAccountService.Instance.StartSignInAsync();

            // await for player account to be signed in to link current unity id and save auth data (token, service)
            StartCoroutine(AwaitLoginCouroutine());
        }

        #endregion


        #region Signout / Logout

        public async void DeleteAccountAndUnlink()
        {
            // Delete Cloud Data
            try
            {
                await CloudSaveManager.Instance.DeleteAccount();
                Debug.Log("Cloud data deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error deleting cloud data: " + ex.Message);
            }

            // Unlink from authentication
            try
            {
                switch (AuthService)
                {
                    case EAuthServices.Apple:
                        await AuthenticationService.Instance.UnlinkAppleAsync();
                        Debug.Log("Apple account unlinked successfully.");
                        break;

                    case EAuthServices.UnityPlayerAccount:
                        await AuthenticationService.Instance.UnlinkUnityAsync();
                        Debug.Log("Unity Player Account unlinked successfully.");
                        break;

                     case EAuthServices.Google:
                        await AuthenticationService.Instance.UnlinkGoogleAsync();
                        Debug.Log("Unity Player Account unlinked successfully.");
                        break;

                    case EAuthServices.Anonymous:
                        Debug.LogWarning("User is anonymous, nothing to unlink.");
                        break;

                    default:
                        Debug.LogWarning("Unhandled AuthService: " + AuthService);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error unlinking account: " + ex.Message);
            }

            // Logout
            Logout(); 
        }


        /// <summary>
        /// Reset all Auth Settings and clear unity Session Token in the local files -> new account
        /// </summary>
        public void Logout()
        {
            // signe out from Authentication Service
            AuthenticationService.Instance.SignOut(true);

            // reset AUTH data
            SetAuth("", EAuthServices.Anonymous);

            // reload Release scene
            SceneLoader.Instance.LoadScene("Release");
        }


        #endregion


        #region Update

        public void Update()
        {
            if (m_AppleAuthManager != null)
            {
                m_AppleAuthManager.Update();
            }
        }

        #endregion


        #region Anonymous 

        /// <summary>
        /// Sign in anonymously for new players.
        /// </summary>
        private async Task SignInAnonymously(bool saveAuth = true)
        {
            if (AuthenticationService.Instance.IsSignedIn)
                return;

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in anonymously. PlayerID: {AuthenticationService.Instance.PlayerId}");

                // save token / service
                if (saveAuth)
                    SetAuth("", EAuthServices.Anonymous);
            }
            catch (AuthenticationException e)
            {
                Debug.LogError($"Anonymous sign-in failed: {e.Message}");
            }
        }

        #endregion


        #region Unity

        async Task<bool> SignInWithUnityAsync(string accessToken)
        {
            try
            {
                // signin
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);

                // save token / service
                SetAuth(accessToken, EAuthServices.UnityPlayerAccount);
                return true;
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }

            return false;
        }

        IEnumerator AwaitLoginCouroutine()
        {
            float timer = 60f;

            // Wait until SignedIn
            while (!PlayerAccountService.Instance.IsSignedIn)
            {
                timer -= Time.deltaTime;

                if (timer < 0)
                {
                    ErrorHandler.Error("Unable to login within 30 seconds - exiting the coroutine");
                    LoginFailedEvent?.Invoke("Unable to login within 30 seconds - exiting the coroutine");
                    yield break;
                }

                yield return null;
            }

            // save auth data locally for next connection
            SetAuth(PlayerAccountService.Instance.AccessToken, EAuthServices.UnityPlayerAccount);

            // save locally that login popup is no longer required
            RecurrentPopupManager.Instance.Diseable(EPopUpState.LoginPopUp);

            // try link account with this id (fails if account already linked)
            LinkWithUnityAsync(PlayerAccountService.Instance.AccessToken);

            // call event that login is completed
            LoginEvent?.Invoke(true);
        }

        async void LinkWithUnityAsync(string accessToken)
        {
            try
            {
                await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
                Debug.Log("Link is successful.");
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                // Account already linked -> reload data
                // -- delete current data
                await CloudSaveManager.Instance.DeleteAccount();

                // -- signout of current auth
                AuthenticationService.Instance.SignOut();

                // -- sign in with access token
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);

                // -- reload Release scene to reload all data
                SceneLoader.Instance.LoadScene("Release");
            }

            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
        }

        #endregion


        #region Apple

        /// <summary>
        /// Login to apple and create signin Token
        /// </summary>
        public void LoginToApple()
        {
            // Initialize the Apple Auth Manager
            if (m_AppleAuthManager == null)
            {
                m_AppleAuthManager = new AppleAuthManager(new PayloadDeserializer());
            }

            // Set the login arguments
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            // Perform the login
            m_AppleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    var appleIDCredential = credential as IAppleIDCredential;
                    if (appleIDCredential != null)
                    {
                        var idToken = Encoding.UTF8.GetString(
                            appleIDCredential.IdentityToken,
                            0,
                            appleIDCredential.IdentityToken.Length);

                        Debug.Log("Sign-in with Apple successfully done. IDToken: " + idToken);

                        // save token in local data for future logins
                        SetAuth(idToken, EAuthServices.Apple);
                       
                        // link current account with current token
                        LinkWithAppleAsync(idToken);
                    }
                    else
                    {
                        Debug.Log("Sign-in with Apple error. Message: appleIDCredential is null");
                        Error = "Retrieving Apple Id Token failed.";
                    }
                },
                error =>
                {
                    Debug.Log("Sign-in with Apple error. Message: " + error);
                    Error = "Retrieving Apple Id Token failed.";
                }
            );
        }

        /// <summary>
        /// Use token to SignIn
        /// </summary>
        /// <param name="idToken"></param>
        /// <returns></returns>
        async Task SignInWithAppleAsync(string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithAppleAsync(idToken);
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // TODO : Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // TODO : Notify the player with the proper error message
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Link current anonymous account with the Apple account
        /// </summary>
        /// <param name="idToken"></param>
        async void LinkWithAppleAsync(string idToken)
        {
            try
            {
                await AuthenticationService.Instance.LinkWithAppleAsync(idToken);
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                // Prompt the player with an error message.
                Debug.LogError("This user is already linked with another account. Log in instead.");
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // TODO : Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // TODO : Notify the player with the proper error message
                Debug.LogException(ex);
            }
        }

        async Task UnlinkAppleAsync()
        {
            try
            {
                await AuthenticationService.Instance.UnlinkAppleAsync();
                Debug.Log("Unlink is successful.");
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
        }

        #endregion


        #region Helpers

        void SetAuth(string token, EAuthServices authService)
        {
            Debug.Log("SetAuth - " + authService + " | " + token);
            PlayerPrefs.SetString(KEY_AUTH_TOKEN, token);
            PlayerPrefs.SetString(KEY_AUTH_SERVICE, authService.ToString());
        }

        public string GetAuthFormated()
        {
            return FormatAuth(Token, AuthService);
        }

        public string FormatAuth(string token, EAuthServices authService)
        {
            return authService + " | " + token;
        }

        public (string token, EAuthServices authService) UnFormatAuth(string authId)
        {
            var split = authId.Split(" | ");
            if (split.Length != 2)
            {
                ErrorHandler.Error("Unable to UnFormat auth : " + authId);
                return DEFAULT_AUTH;
            }

            if (! Enum.TryParse(split[0], out EAuthServices authService))
            {
                ErrorHandler.Error("Unknown auth service : " + split[0]);
                return DEFAULT_AUTH;
            }

            return (split[1], authService);
        }

        #endregion
    }
}