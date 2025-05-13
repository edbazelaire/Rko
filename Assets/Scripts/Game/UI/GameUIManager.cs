using Assets;
using Assets.Scripts.Game;
using Assets.Scripts.Managers.Sound;
using Enums;
using Game;
using Game.SpellGFXs;
using Game.Spells;
using Game.UI;
using Game.UI.GameUI;
using Managers;
using Menu.Common.Buttons;
using Network;
using Save;
using System.Collections.Generic;
using Tools;
using Tools.Debugs.BT;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    #region Members

    static GameUIManager s_Instance;
    private bool m_Initialized;
    public static bool Initialized => s_Instance != null && s_Instance.m_Initialized;

    private IntroGameUI     m_IntroGameUI;
    private EndGameUI       m_EndGameUI;
    private ErrorGameUI     m_ErrorGameUI;
    private TutoGameUI      m_TutoGameUI;
    private BTDebugger      m_BTDebugger;
    private EmotsSectionUI  m_EmotsSectionUI;
    private GameTimerUI     m_GameTimerUI;

    const string        c_PlayerUIContainerPrefix   = "PlayerUIContainer_";
    const string        c_SpellsContainer           = "SpellsContainer";
    const int           NUM_TEAMS                   = 2;
    
    // ==============================================================================================================
    // Templates
    /// <summary> Template of a PlayerUI to create on Character Instantiation </summary>
    public GameObject   m_PlayerUITemplate;
    /// <summary> Spell item template to instantiate on Character Instantiation </summary>
    public GameObject   SpellTemplate;

    // ==============================================================================================================
    // Game Objects & Components
    /// <summary> container for MovementButtons </summary>
    MovementButtonsContainer m_MovementButtonsContainer;
    /// <summary> container for SpellItemUI(s) </summary>
    GameObject          m_SpellContainer;
    /// <summary> container for SpellItemUI(s) of linked spells </summary>
    GameObject          m_LinkedSpellsContainer;
    /// <summary> containers for PlayerUI(s) of each team (using team as index) </summary>
    List<GameObject>    m_PlayerUIContainers;
    /// <summary> containers for PlayerUI(s) of each player (using clientId as index) </summary>
    Dictionary<ulong, PlayerUI> m_PlayerUIs;
    /// <summary> list of instantiated SpellItemUI(s) </summary>
    List<SpellItemUI>   m_SpellItems;

    // ==============================================================================================================
    // Data
    bool                m_LeftMovementButtonPressed;
    bool                m_RightMovementButtonPressed;
    bool                m_PreventiveLossApplied;

    public bool PreventiveLossApplied => m_PreventiveLossApplied;

    // DEBUG - Remove (?)
    public int PreviousStage;
    // DEBUG - Remove (?)

    // ==============================================================================================================
    // Public Accessors
    public static IntroGameUI IntroGameUI                   => Instance.m_IntroGameUI;
    public static ErrorGameUI ErrorGameUI                   => Instance.m_ErrorGameUI;
    public static EndGameUI EndGameUI                       => Instance.m_EndGameUI;
    public static TutoGameUI TutoGameUI                     => Instance.m_TutoGameUI;
    public static BTDebugger BTDebugger                     => Instance.m_BTDebugger;
    public static EmotsSectionUI EmotsSectionUI             => Instance.m_EmotsSectionUI;
    public static GameTimerUI GameTimerUI                   => Instance.m_GameTimerUI;
    public static HitDisplayUI DamageDisplayManager         => HitDisplayUI.Instance;
    public static List<SpellItemUI> SpellItems              => Instance.m_SpellItems;
    public static MovementButtonsContainer MovementButtonsContainer => Instance.m_MovementButtonsContainer;
    public static bool LeftMovementButtonPressed            => Instance.m_LeftMovementButtonPressed;
    public static bool RightMovementButtonPressed           => Instance.m_RightMovementButtonPressed;

    #endregion


    #region Initialization

    void FindComponents()
    {
        m_IntroGameUI       = Finder.FindComponent<IntroGameUI>(transform.parent.gameObject,    "IntroGameUI");
        m_EndGameUI         = Finder.FindComponent<EndGameUI>(transform.parent.gameObject,      "EndGameUI");
        m_ErrorGameUI       = Finder.FindComponent<ErrorGameUI>(transform.parent.gameObject,    "ErrorGameUI");
        m_TutoGameUI        = Finder.FindComponent<TutoGameUI>(transform.parent.gameObject,     "TutoGameUI");
        m_BTDebugger        = Finder.FindComponent<BTDebugger>(transform.parent.gameObject,     "BTDebugger");
        m_EmotsSectionUI    = Finder.FindComponent<EmotsSectionUI>(gameObject,                  "EmotsSectionUI");
        m_GameTimerUI       = Finder.FindComponent<GameTimerUI>(gameObject,                     "GameTimerUI");

        FindMovementButtons();
        FindPlayerUIContainers();
        FindSpellsContainers();
    }

    public void Initialize()
    {
        FindComponents();

        GameManager.Instance.State.OnValueChanged += OnGameStateChanged;

        m_SpellItems = new List<SpellItemUI>();

        m_EndGameUI.Initialize();
        m_EndGameUI.gameObject.SetActive(false);
        m_ErrorGameUI.gameObject.SetActive(false);
        m_TutoGameUI.gameObject.SetActive(false);
        m_BTDebugger.gameObject.SetActive(false);
        m_PlayerUIs = new Dictionary<ulong, PlayerUI> { };

        // display or not the Timer
        if (LobbyHandler.Instance.GameMode != EGameMode.Ranked)
            Destroy(m_GameTimerUI.gameObject);
        else
            m_GameTimerUI.Initialize();

        LoadArena();

        m_Initialized = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void FindPlayerUIContainers()
    {
        m_PlayerUIContainers = new List<GameObject>();
        for (int i = 0; i < NUM_TEAMS; i++)
        {
            // get container game object
            GameObject container = GameObject.Find(GetPlayerUIContainerName(i));

            // remove all childs in container
            UIHelper.CleanContent(container);

            // add container as container for team "i"
            m_PlayerUIContainers.Add(container);
        }
    }

    /// <summary>
    /// Get movement buttons
    /// </summary>
    void FindMovementButtons()
    {
        var container = Finder.Find(gameObject, "MovementButtonsContainer");
        m_MovementButtonsContainer = Finder.FindComponent<MovementButtonsContainer>(container);

        // link pressed button bools to pressed envents
        m_MovementButtonsContainer.MovementInputEvent += (int moveX) => { m_LeftMovementButtonPressed = moveX == -1; m_RightMovementButtonPressed = moveX == 1; };
    }

    /// <summary>
    /// 
    /// </summary>
    void FindSpellsContainers()
    {
        m_SpellContainer = Finder.Find(gameObject, c_SpellsContainer);
        m_LinkedSpellsContainer = Finder.Find(gameObject, "LinkedSpellsContainer");

        ClearSpells();
    }

    void DeleteGameUI()
    {
        Destroy(gameObject);
    }

    #endregion


    #region Cleaners

    public static void ClearAllSpells()
    {
        if (! GameManager.Instance.IsServer)
            return;

        var allSpells = FindObjectsByType<Spell>(FindObjectsSortMode.None);
        var allSpellGfxs = FindObjectsByType<SpellGFX>(FindObjectsSortMode.None);

        foreach (var temp in allSpells)
            Destroy(temp);

        foreach (var temp in allSpellGfxs)
            Destroy(temp);
    }

    #endregion


    #region Public Manipulators

    /// <summary>
    /// Set the health bar for this controller
    /// </summary>
    public void SetPlayersUI(ulong clientId, int team)
    {
        if (m_PlayerUIs == null)
            m_PlayerUIs = new Dictionary<ulong, PlayerUI> { };

        m_PlayerUIs.Add(clientId, Finder.FindComponent<PlayerUI>(Instantiate(m_PlayerUITemplate, m_PlayerUIContainers[GameManager.Instance.Owner.Team == team ? 0 : 1].transform)));
        m_PlayerUIs[clientId].Initialize(clientId);
    }

    public PlayerUI GetPlayerUI(ulong clientId)
    {
        return m_PlayerUIs[clientId];
    }

    /// <summary>
    /// add a SpellItemUI to the spell container
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="spell"></param>
    public void CreateSpellTemplate(ESpell spell, int level, int index)
    {
        SpellItemUI spellItem = Finder.FindComponent<SpellItemUI>(GameObject.Instantiate(SpellTemplate, m_SpellContainer.transform));
        spellItem.Initialize(spell, level, index);
        m_SpellItems.Add(spellItem);
    }

    /// <summary>
    /// add a SpellItemUI to the spell container
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="spell"></param>
    public void CreateLinkedSpellTemplate(ESpell spell, int level, int index)
    {
        SpellItemUI spellItem = Finder.FindComponent<SpellItemUI>(GameObject.Instantiate(SpellTemplate, m_LinkedSpellsContainer.transform));
        spellItem.Initialize(spell, level, index);
        m_SpellItems.Add(spellItem);
    }

    /// <summary>
    /// remove all SpellItemUI from the spell container
    /// </summary>
    public void ClearSpells()
    {
        UIHelper.CleanContent(m_SpellContainer);
        UIHelper.CleanContent(m_LinkedSpellsContainer);
    }

    /// <summary>
    /// Get the SpellItemUI of the given spell
    /// </summary>
    /// <param name="spell"></param>
    /// <returns></returns>
    public SpellItemUI GetSpellItemUI(ESpell spell) 
    {
        foreach (SpellItemUI spellItem in m_SpellItems)
        {
            if (spellItem.Spell == spell)
                return spellItem;
        }
        return null;
    }

    public void LockAllSpellItems()
    {
        foreach (SpellItemUI spellItem in m_SpellItems)
        {
            spellItem.SetState(EButtonState.Locked);
        }
    }

    public void UnlockAllSpellItems()
    {
        foreach (SpellItemUI spellItem in m_SpellItems)
        {
            spellItem.SetState(EButtonState.Normal);
        }
    }

    #endregion


    #region Intro UI

    public void SetUpIntroScreen()
    {
        if (m_IntroGameUI == null)
        {
            ErrorHandler.Error("IntroGameUI not provided");
            return;
        }

        Dictionary<ulong, SPlayerData> playerData = FetchPlayerData();

        m_IntroGameUI.Initialize(playerData);
    }

    Dictionary<ulong, SPlayerData> FetchPlayerData()
    {
        var playerDataDict = new Dictionary<ulong, SPlayerData>();
        foreach (Controller controller in GameManager.Instance.Controllers.Values)
        {
            playerDataDict.Add(controller.PlayerId, controller.PlayerData);
        }

        return playerDataDict;
    }

    public void LoadArena()
    {
        // clean previous arena manager if any
        ArenaManager.Clear();

        ArenaManager arenaManager;
        switch (LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                arenaManager = AssetLoader.LoadArena(LobbyHandler.Instance.ArenaType.ToString(), ProgressionCloudData.CurrentArena.IsBoss());
                break;

            case EGameMode.Ranked:
                arenaManager = AssetLoader.LoadArena("VoidArena");
                break;

            default:
                arenaManager = AssetLoader.LoadArena("DefaultArena");
                break;
        }

        if (arenaManager == null)
            return;

        arenaManager = Instantiate(arenaManager);
        arenaManager.Initialize();
    }

    #endregion


    #region GameOver

    public void SetUpGameOver(EGameResult gameResult)
    {
        if (m_BTDebugger != null)
            Destroy(m_BTDebugger.gameObject);

        SoundFXManager.PlayOnce(gameResult == EGameResult.Win ? SoundFXManager.WinSoundFX : SoundFXManager.LossSoundFX);

        m_EndGameUI.Activate(gameResult, m_PreventiveLossApplied);

        // destroy self
        DeleteGameUI();
    }

    #endregion


    #region Listeners

    void OnGameStateChanged(EGameState oldState, EGameState newState)
    {
        // security
        if (newState <= oldState)
            return;

        // on game starts : apply preventive loss
        if (newState == EGameState.GameRunning)
        {
            if (m_PreventiveLossApplied)
                return;

            switch (LobbyHandler.Instance.GameMode)
            {
                case EGameMode.Arena:
                    PreviousStage = ProgressionCloudData.CurrentArena.Stage;
                    break;

                case EGameMode.Ranked:
                    PreviousStage = ProgressionCloudData.CurrentLeagueStage;
                    break;

                case EGameMode.Training:
                    PreviousStage = -1;
                    break;

                default:
                    ErrorHandler.Warning("Unahandled case : " + LobbyHandler.Instance.GameMode);
                    PreviousStage = -1;
                    break;
            }

            // apply a loss at start to handle potential disconnections
            m_PreventiveLossApplied = ProgressionCloudData.ApplyPreventiveLoss(LobbyHandler.Instance.GameMode);
        }
    }

    #endregion


    #region Static Members

    public static GameUIManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindFirstObjectByType<GameUIManager>();
                if (s_Instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(GameUIManager).Name;
                    s_Instance = obj.AddComponent<GameUIManager>();
                }

                s_Instance.Initialize();
            }

            return s_Instance;
        }
    }

    public static string GetPlayerUIContainerName(int team)
    {
        return c_PlayerUIContainerPrefix + team;
    }

    #endregion
}
