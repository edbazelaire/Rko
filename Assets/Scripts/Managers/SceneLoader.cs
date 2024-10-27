using Assets;
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
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;


public class SceneLoader : MonoBehaviour
{ 
    static SceneLoader s_Instance;

    [SerializeField] LoadingScreen m_LoadingScreen;
    [SerializeField] GameManager m_GameManager;

    string m_SceneLoading = "";
    public string SceneLoading => m_SceneLoading;
    public bool IsLoading => m_SceneLoading != "";
    public GameManager GameManager => m_GameManager;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }


    #region Scene Loading

    public void LoadScene(string sceneName)
    {
        // can not load scene while an other scene is loading
        if (m_SceneLoading != "")
        {
            ErrorHandler.Warning("Trying to load scene " + sceneName + " while a scene is already loading : " + m_SceneLoading);
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        m_SceneLoading = sceneName;

        if (Main.State != EAppState.Release)
            SoundFXManager.PlayStateMusic(EAppState.LoadingScreen);

        Main.SetState(EAppState.LoadingScreen);

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
        Main.Canvas.worldCamera = Camera.main;
        m_SceneLoading = "";
    }

    #endregion


    #region Display Loading Screen only

    public static void Display(bool activate, float atPercentage = 0f)
    {
        Instance.m_LoadingScreen.Display(activate);
        Instance.m_LoadingScreen.SetProgress(atPercentage);
    }

    public static void UpdateProgress(float progress)
    {
        Instance.m_LoadingScreen.SetProgress(progress);
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
