using Assets.Scripts.Managers.Sound;
using Enums;
using Game;
using Game.Backgroud;
using Game.UI;
using Managers;
using Network;
using Save;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    #region Members

    static GameUIManager s_Instance;

    [SerializeField] Canvas m_BackgroundCanvas;

    private IntroGameUI    m_IntroGameUI;
    private EndGameUI      m_EndGameUI;
    private ErrorGameUI    m_ErrorGameUI;

    GameObject m_Background;
    //[SerializeField] private Image          m_Background;

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
    /// <summary> container for SpellItemUI(s) </summary>
    GameObject          m_SpellContainer;
    /// <summary> container for SpellItemUI(s) of linked spells </summary>
    GameObject          m_LinkedSpellsContainer;
    /// <summary> containers for PlayerUI(s) </summary>
    List<GameObject>    m_PlayerUIContainers;
    /// <summary> list of instantiated SpellItemUI(s) </summary>
    List<SpellItemUI>   m_SpellItems;

    // ==============================================================================================================
    // Data
    bool                m_LeftMovementButtonPressed;
    bool                m_RightMovementButtonPressed;
    bool                m_PreventiveLossApplied;

    // ==============================================================================================================
    // Public Accessors
    public static IntroGameUI IntroGameUI               => Instance.m_IntroGameUI;
    public static ErrorGameUI ErrorGameUI               => Instance.m_ErrorGameUI;
    public static bool LeftMovementButtonPressed        => Instance.m_LeftMovementButtonPressed;
    public static bool RightMovementButtonPressed       => Instance.m_RightMovementButtonPressed;
   
    #endregion


    #region Initialization

    void FindComponents()
    {
        m_IntroGameUI   = Finder.FindComponent<IntroGameUI>(transform.parent.gameObject, "IntroGameUI");
        m_EndGameUI     = Finder.FindComponent<EndGameUI>(transform.parent.gameObject, "EndGameUI");
        m_ErrorGameUI   = Finder.FindComponent<ErrorGameUI>(transform.parent.gameObject, "ErrorGameUI");

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
        m_ErrorGameUI.gameObject.SetActive(false);

        SetUpBackground();
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

        // link pressed button bools to pressed envents
        Finder.FindComponent<MovementButtonsContainer>(container).MovementInputEvent += (int moveX) => { m_LeftMovementButtonPressed = moveX == -1; m_RightMovementButtonPressed = moveX == 1; };
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


    #region GUI Manipulators

    void SetUpBackground()
    {
        UIHelper.CleanContent(m_BackgroundCanvas.gameObject);
        m_Background = Instantiate(AssetLoader.Load<GameObject>("NightSkyBackground", AssetLoader.c_ArenaBackgroundsPath), m_BackgroundCanvas.transform);
        m_Background.GetComponent<NightSkyBackground>().Initialize();
    }

    Sprite GetBackgroundImage()
    {
        if (LobbyHandler.Instance.GameMode == EGameMode.Arena)
        {
            var sprite =  AssetLoader.Load<Sprite>(LobbyHandler.Instance.ArenaType.ToString(), AssetLoader.c_ArenaBackgroundsImagePath);
            if (sprite != null) 
                return sprite;
        }

        return AssetLoader.Load<Sprite>("Default", AssetLoader.c_ArenaBackgroundsImagePath);
    }

    #endregion


    #region Public Manipulators

    /// <summary>
    /// Set the health bar for this controller
    /// </summary>
    public void SetPlayersUI(ulong ClientId, int team)
    {
        bool ally = GameManager.Instance.Owner.Team == team;

        PlayerUI playerUI = Finder.FindComponent<PlayerUI>(Instantiate(m_PlayerUITemplate, m_PlayerUIContainers[ally ? 0 : 1].transform));
        playerUI.Initialize(ClientId);
    }

    /// <summary>
    /// add a SpellItemUI to the spell container
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="spell"></param>
    public void CreateSpellTemplate(ESpell spell, int level)
    {
        SpellItemUI spellItem = Finder.FindComponent<SpellItemUI>(GameObject.Instantiate(SpellTemplate, m_SpellContainer.transform));
        spellItem.Initialize(spell, level);
        m_SpellItems.Add(spellItem);
    }

    /// <summary>
    /// add a SpellItemUI to the spell container
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="spell"></param>
    public void CreateLinkedSpellTemplate(ESpell spell, int level)
    {
        SpellItemUI spellItem = Finder.FindComponent<SpellItemUI>(GameObject.Instantiate(SpellTemplate, m_LinkedSpellsContainer.transform));
        spellItem.Initialize(spell, level);
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
    public SpellItemUI GetSpellUIItem(ESpell spell) 
    {
        foreach (SpellItemUI spellItem in m_SpellItems)
        {
            if (spellItem.Spell == spell)
                return spellItem;
        }
        return null;
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

    #endregion


    #region GameOver

    public void SetUpGameOver(bool win)
    {
        SoundFXManager.PlayOnce(win ? SoundFXManager.WinSoundFX : SoundFXManager.LossSoundFX);

        m_EndGameUI.Activate(win, m_PreventiveLossApplied);

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
