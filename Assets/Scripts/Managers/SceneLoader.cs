using Assets;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Sound;
using Assets.Scripts.UI;
using Enums;
using Game;
using Network;
using System;
using System.Collections;
using System.Threading.Tasks;
using Tools;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoader : MonoBehaviour
{
    public static Action<bool> SceneLoadedEvent;

    static SceneLoader s_Instance;

    [SerializeField] LoadingScreen m_DefaultLoadingScreen;
    [SerializeField] GameManager m_GameManager;

    LoadingScreen m_SpecialLoadingScreen = null;
    LoadingScreen m_LoadingScreen => m_SpecialLoadingScreen == null ? m_DefaultLoadingScreen : m_SpecialLoadingScreen;
    string m_SceneLoading = "";
    public string SceneLoading => m_SceneLoading;
    public bool IsLoading => m_SceneLoading != "";
    public GameManager GameManager => m_GameManager;

    private void Awake()
    {
        // Check if another instance of this object already exists
        if (s_Instance != null && s_Instance.gameObject != gameObject)
        {
            Destroy(gameObject);
            return; // Stop further execution to avoid duplicates
        }

        DontDestroyOnLoad(this);
    }


    #region Scene Loading

    public void LoadScene(string sceneName, Action onSuccessCallback = null)
    {
        // can not load scene while an other scene is loading
        if (m_SceneLoading != "")
        {
            ErrorHandler.Warning("Trying to load scene " + sceneName + " while a scene is already loading : " + m_SceneLoading);
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneName, onSuccessCallback));
    }

    IEnumerator LoadSceneAsync(string sceneName, Action onSuccessCallback = null)
    {
        m_SceneLoading = sceneName;

        if (Main.State != EAppState.Release)
            SoundFXManager.PlayStateMusic(EAppState.LoadingScreen);

        Main.SetState(EAppState.LoadingScreen);

        SetLoadingScreen(sceneName);
        if (m_LoadingScreen == null)
            ErrorHandler.FatalError("Loading screen not set");

        m_LoadingScreen.Display(true);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        float progress;
        float nSteps = sceneName == "Arena" ? 2f : 1f;

        while (!asyncLoad.isDone)
        {
            progress = Mathf.Clamp01(asyncLoad.progress / (nSteps * 0.9f));
            m_LoadingScreen.SetProgress(progress);
            yield return null;
        }

        // call actions that happens when a scene is done loading 
        OnSceneLoaded();

        // when laoding arena, the switch to Game State
        if (sceneName == "Arena")
        {
            Main.SetState(EAppState.InGame);
        }
        else
        {
            if (!Enum.TryParse(sceneName, out EAppState appState))
                ErrorHandler.FatalError("Unable to find scene " + sceneName + " as EAppState");

            Main.SetState(appState);
        }

        switch (sceneName)
        {
            case "MainMenu":
                // wait one frame
                yield return null;
                break;

            case "Arena":
                // waiting for GameManager to be created
                while (LobbyHandler.Instance.State <= ELobbyState.WaitingGameManager)
                {
                    yield return null;
                }

                while (!GameManager.Instance.IsGameLoaded)
                {
                    progress = 1 / nSteps + GameManager.Instance.ProgressGameStart.Value / nSteps;
                    m_LoadingScreen.SetProgress(Mathf.Clamp01(progress));
                    yield return null;
                }
                break;
        }

        SoundFXManager.PlayStateMusic(Main.State);
        m_LoadingScreen.Display(false);

        // call success callback if any
        onSuccessCallback?.Invoke();
    }

    public async Task SceneLoadingAsync()
    {
        while (IsLoading)
            await Task.Delay(1000);

        return;
    }

    /// <summary>
    /// Reset name, setup MainCanvas with MainCamera
    /// </summary>
    void OnSceneLoaded()
    {
        // set world camera as current camera
        Main.Canvas.worldCamera = Camera.main;
        // clean current scene loading name
        m_SceneLoading = "";
        // clear current scene manager
        ScreenManager.Clear();

        // send event that scene was loaded 
        SceneLoadedEvent?.Invoke(true);
    }

    #endregion


    #region Helpers

    void SetLoadingScreen(string sceneName)
    {
        if (m_SpecialLoadingScreen != null)
        {
            Destroy(m_SpecialLoadingScreen);
            m_SpecialLoadingScreen = null;
        }

        switch (sceneName)
        {
            case "Arena":
                if (LobbyHandler.Instance.GameMode == EGameMode.Arena)
                {
                    if (AssetLoader.TryLoadLoadingScreen(LobbyHandler.Instance.ArenaType.ToString(), out LoadingScreen screen))
                    {
                        m_SpecialLoadingScreen = GameObject.Instantiate(screen, m_DefaultLoadingScreen.transform.parent);
                        return;
                    }
                    ErrorHandler.Warning("Unable to find loading screen for arena : " + LobbyHandler.Instance.ArenaType);
                }

                else if (LobbyHandler.Instance.GameMode == EGameMode.Ranked)
                {
                    // TODO : Ranked loading screen
                }

                else if (LobbyHandler.Instance.GameMode == EGameMode.Training)
                {
                    // TODO : Training loading screen
                }

                break;

            default:
                m_SpecialLoadingScreen = null;
                break;
        }
    }

    #endregion


    #region Display Loading Screen only

    public static void Display(bool activate, float atPercentage = 0f)
    {
        Instance.m_LoadingScreen.Display(activate);
        Instance.m_LoadingScreen.SetProgress(atPercentage);
    }

    public static void UpdateProgress(float progress, string infoText = "")
    {
        Instance.m_LoadingScreen.SetProgress(progress);
        Instance.m_LoadingScreen.SetInfoText(infoText);
    }

    #endregion

    public static SceneLoader Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindFirstObjectByType<SceneLoader>();
                if (s_Instance == null)
                    ErrorHandler.FatalError("Unable to find SceneLoader");

                Display(true, 0f);
            }
            return s_Instance;
        }
    }
}
