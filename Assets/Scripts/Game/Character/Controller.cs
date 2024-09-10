using AI;
using Data;
using Data.DataStructures;
using Data.GameManagement;
using Enums;
using Game;
using Game.Character;
using Game.Loaders;
using Managers;
using Save;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Controller : NetworkBehaviour
{
    #region Members      

    [SerializeField] Collider2D m_Collider;

    // ===================================================================================
    // PRIVATE VARIABLES 
    // -- Network Variables
    NetworkVariable<FixedString64Bytes>     m_PlayerName        = new NetworkVariable<FixedString64Bytes>("");
    NetworkVariable<SPlayerData>            m_PlayerData        = new NetworkVariable<SPlayerData>();
    NetworkVariable<ECharacter>             m_Character         = new NetworkVariable<ECharacter>(ECharacter.Count);
    NetworkVariable<int>                    m_CharacterLevel    = new NetworkVariable<int>(1);
    NetworkVariable<int>                    m_Team              = new NetworkVariable<int>(-1);
    NetworkVariable<bool>                   m_IsPlayer          = new NetworkVariable<bool>(true);
    NetworkVariable<bool>                   m_IsInitialized     = new NetworkVariable<bool>(false);

    // -- Server Variable
    RuneData[] m_RuneData;

    // -- local variables
    bool m_GameRunning = false;

    // -- Components & GameObjects
    BehaviorTree            m_BehaviorTree;
    Game.Character.AnimationHandler        m_AnimationHandler;
    GFXHandler              m_GFXHandler;
    Movement                m_Movement;
    Life                    m_Life;
    EnergyHandler           m_EnergyHandler;
    SpellHandler            m_SpellHandler;
    StateHandler            m_StateHandler;
    CounterHandler          m_CounterHandler;
    AutoAttackHandler       m_AutoAttackHandler;
    TriggerEffectHandler    m_TriggerEffectHandler;
    ClientAnalytics         m_ClientAnalytics;

    // ===================================================================================
    // PUBLIC ACCESSORS

    // -- Data
    public SPlayerData      PlayerData          => m_PlayerData.Value;
    public string           PlayerName          => m_PlayerName.Value.ToString();
    public ECharacter       Character           => m_Character.Value;
    public int              CharacterLevel      => m_CharacterLevel.Value;
    public RuneData[]       RuneData            => m_RuneData;
    public int              Team                => m_Team.Value;
    public bool             IsPlayer            => m_IsPlayer.Value;
    public ulong            PlayerId            => IsPlayer ? OwnerClientId : GameManager.BOT_CLIENT_ID;
    public bool             GameRunning         => m_GameRunning;


    // -- Components & GameObjects
    public BehaviorTree     BehaviorTree                => m_BehaviorTree;
    public Game.Character.AnimationHandler AnimationHandler    => m_AnimationHandler;
    public GFXHandler       GFXHandler                  => m_GFXHandler;
    public Movement         Movement                    => m_Movement;
    public Life             Life                        => m_Life;
    public SpellHandler     SpellHandler                => m_SpellHandler;
    public StateHandler     StateHandler                => m_StateHandler;
    public CounterHandler   CounterHandler              => m_CounterHandler;
    public AutoAttackHandler AutoAttackHandler          => m_AutoAttackHandler;
    public TriggerEffectHandler TriggerEffectHandler    => m_TriggerEffectHandler;
    public ClientAnalytics  ClientAnalytics             => m_ClientAnalytics;
    public EnergyHandler    EnergyHandler               => m_EnergyHandler;
    public Collider2D       Collider                    => m_Collider;
    /// <summary> y position of the character's Height point </summary>
    public float            CharacterHeight             => m_Collider.transform.position.y + m_Collider.bounds.extents.y;

    #endregion


    #region Initialization 

    /// <summary>
    /// Called when the controller is spawned on the network
    /// </summary>
    public override void OnNetworkSpawn()
    {
        ErrorHandler.Log("Controller.OnNetworkSpawn()", ELogTag.GameSystem);   

        // setup components
        m_Life                  = Finder.FindComponent<Life>(gameObject);
        m_EnergyHandler         = Finder.FindComponent<EnergyHandler>(gameObject);
        m_Movement              = Finder.FindComponent<Movement>(gameObject);
        m_SpellHandler          = Finder.FindComponent<SpellHandler>(gameObject);
        m_AnimationHandler      = Finder.FindComponent<Game.Character.AnimationHandler>(gameObject);
        m_GFXHandler            = Finder.FindComponent<GFXHandler>(gameObject);
        m_StateHandler          = Finder.FindComponent<StateHandler>(gameObject);
        m_CounterHandler        = Finder.FindComponent<CounterHandler>(gameObject);
        m_AutoAttackHandler     = Finder.FindComponent<AutoAttackHandler>(gameObject);
        m_TriggerEffectHandler  = Finder.FindComponent<TriggerEffectHandler>(gameObject);
        m_ClientAnalytics       = Finder.FindComponent<ClientAnalytics>(gameObject, throwError: false);

        // check behavior tree
        m_BehaviorTree = Finder.FindComponent<BehaviorTree>(gameObject, throwError: false);
        
        // add event to call UI initialization after NetworkVariable update 
        m_IsInitialized.OnValueChanged += OnInitializedChanged;

        GameManager.GameStartedEvent    += OnGameStarted;
        m_Life.DiedEvent                += OnDied;
    }

    /// <summary>
    /// Wait for all data to be initialized on the server side before starting display and setup (ui, etc...)
    /// </summary>
    /// <param name="old"></param>
    /// <param name="newValue"></param>
    void OnInitializedChanged(bool old, bool newValue)
    {
        ErrorHandler.Log("==============================================================", ELogTag.GameSystem);
        ErrorHandler.Log("Initialized : ", ELogTag.GameSystem);
        ErrorHandler.Log("     + LocalClient : " + NetworkManager.Singleton.LocalClientId, ELogTag.GameSystem);
        ErrorHandler.Log("     + Owner : " + OwnerClientId, ELogTag.GameSystem);
        ErrorHandler.Log("==============================================================", ELogTag.GameSystem);

        // add controller on client side
        if (newValue)
            GameManager.Instance.AddController(PlayerId, this);
    }

    /// <summary>
    /// Initialize the controller
    /// </summary>
    public void Initialize(SPlayerData playerData, int team, bool isPlayer = true)
    {
        if (!IsServer)
            return;

        m_Team.Value            = team;
        m_IsPlayer.Value        = isPlayer;

        transform.position = ArenaManager.Instance.Spawns[team][0].position;
        transform.rotation = Quaternion.Euler(0f, team == 0 ? 0f : -180f, 0f);

        InitializeCharacterData(playerData);

        m_IsInitialized.Value = true;
    }

    public void InitializeUI()
    {
        ECharacter character    = m_Character.Value;
        int team                = m_Team.Value;

        // display the player's ui 
        GameUIManager.Instance.SetPlayersUI(PlayerId, team);

        // setup character preview
        m_GFXHandler.Initialize(character);

        // setup local client size
        SetSize();

        // get animator
        Animator animator       = Finder.FindComponent<Animator>(m_GFXHandler.CharacterPreview);
        m_AnimationHandler.Initialize(animator);

        // initialize base MovementSpeed
        m_AnimationHandler.UpdateMovementSpeed();

        // update personnal UI if is owner (and not an AI)
        if (!IsOwner || !IsPlayer)
            return;

        // flip camera for player of team 1
        if (team == 1)
        {
            Camera.main.transform.rotation = Quaternion.Euler(0f, -180f, 0f);
            var cameraPos = Camera.main.transform.position;
            cameraPos.z *= -1;
            Camera.main.transform.position = cameraPos;
        }

        // setup the seplls icons buttons
        SetupSpellUI();

        // select auto attack by default (if not IsAutoTarget)
        bool isAutoTarget = true;           // TODO : use PlayerPref to set isAutoTarget or not by default
        if (! (isAutoTarget || SpellLoader.GetSpellData(m_SpellHandler.AutoAttack).IsAutoTarget))
            m_SpellHandler.AskSpellSelectionServerRPC(m_SpellHandler.AutoAttack);
    }

    /// <summary>
    /// Implement all data related to the Character
    /// </summary>
    void InitializeCharacterData(SPlayerData playerData)
    {
        if (! IsServer)
            return;

        m_PlayerData.Value      = playerData;
        m_PlayerName.Value      = playerData.PlayerName;
        m_Character.Value       = playerData.Character;
        m_CharacterLevel.Value  = playerData.CharacterLevel;

        // set all RuneData depending on activation type
        m_RuneData = new RuneData[playerData.Runes.Length];
        for (int i = 0; i < playerData.Runes.Length; i++)
        {
            // safety check
            if (i >= playerData.Runes.Length)
            {
                ErrorHandler.Error("Bad index (" + i + ") for provided Runes of length : " + playerData.Runes.Length);
                break;
            }

            // safety check
            if (i >= playerData.RuneLevels.Length)
            {
                ErrorHandler.Error("Bad index (" + i + ") for provided RuneLevels of length : " + playerData.RuneLevels.Length);
                break;
            }

            m_RuneData[i] = SpellLoader.GetRuneData(playerData.Runes[i], playerData.RuneLevels[i]);
            m_RuneData[i].SetActivation(CharacterBuildsCloudData.GetRuneActivationFromIndex(i));
        }
     
        CharacterData characterData = CharacterLoader.GetCharacterData(playerData.Character, playerData.CharacterLevel, destroy: true);
        characterData.AddBonusStats(GetBonusStats());

        // initialize SpellHandler with character's spells
        m_SpellHandler.Initialize(characterData.AutoAttack, characterData.SpecialAbility, characterData.Ultimate, playerData.Spells.ToList(), playerData.SpellLevels.ToList());

        // initialize MovementSpeed with character's speed
        m_Movement.Initialize(characterData.Speed);

        // initialize StateHandler with character data
        m_StateHandler.Initialize(characterData);

        // init health and energy
        m_Life.Initialize(characterData.MaxHealth, characterData.GetInt(EStateEffectProperty.Shield));
        m_EnergyHandler.Initialize(10, characterData.MaxEnergy);
        m_TriggerEffectHandler.Initialize(GetTriggerEffects());

        // init BehaviorTree
        if (m_BehaviorTree != null)
            m_BehaviorTree.Initialize(playerData.BotData);
    }

    /// <summary>
    /// Create the SpellItemUI for each spell of the character
    /// </summary>
    public void SetupSpellUI()
    {
        if (!IsOwner)
            return;

        // clear the spell container (in case any spell was already there)
        GameUIManager.Instance.ClearSpells();

        // add linked spells
        var characterData = CharacterLoader.GetCharacterData(m_Character.Value, destroy: true);
        GameUIManager.Instance.CreateLinkedSpellTemplate(characterData.Ultimate, m_CharacterLevel.Value);
        GameUIManager.Instance.CreateLinkedSpellTemplate(characterData.SpecialAbility, m_CharacterLevel.Value);

        // create a SpellItemUI for each spell of the character
        for (int i = 0; i < m_SpellHandler.Spells.Count; i++)
        {
            ESpell spell = m_SpellHandler.Spells[i];
            
            // skip linked spells
            if (spell == characterData.Ultimate || spell == characterData.AutoAttack || spell == characterData.SpecialAbility)
                continue;
            
            GameUIManager.Instance.CreateSpellTemplate(m_SpellHandler.Spells[i], m_SpellHandler.SpellLevels[i]);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        GameManager.GameStartedEvent -= OnGameStarted;
    }

    #endregion


    #region Bonus Stats & Trigger Effects

    List<SCharacterStatScaling> GetBonusStats()
    {
        var bonusStats = m_PlayerData.Value.BonusStats.ToList();

        if (m_RuneData == null)
        {
            ErrorHandler.Error("Rune Data not defined yet");
            return bonusStats;
        }

        foreach (RuneData data in m_RuneData)
        {
            bonusStats.AddRange(data.GetBonusStats());
        }

        return bonusStats;
    }

    List<STriggerEffect> GetTriggerEffects()
    {
        // get base raw list of trigger effects
        var list = m_PlayerData.Value.TriggerEffects.ToList();

        if (m_RuneData == null)
        {
            ErrorHandler.Error("Rune Data not defined yet");
            return list;
        }

        // set level of trigger effects = to character level and add it to list of trigger effects
        foreach (RuneData data in m_RuneData)
        {
            list.AddRange(data.GetTriggerEffects());
        }

        return list;
    }

    #endregion


    #region On Game Starts

    /// <summary>
    /// List of action enabled on game starting
    ///     - enable AI Behavior tree
    ///     
    ///     TODO (?)
    ///     - allow server to receive data
    /// 
    /// ONLY SERVER (?)
    /// </summary>
    void OnGameStarted()
    {
        // set to "true" the variable that the game has started
        m_GameRunning = true;

        if (!IsServer)
            return;

        // activate components allowing the player to make actions
        ActivateActionComponent(true);

        // when game starts, activate behavior tree of the 
        if (!IsPlayer)
        {
            m_BehaviorTree.Activate(true);
        }
    }

    #endregion


    #region Server RPC


    #endregion


    #region Public Manipulators

    [ClientRpc]
    public void ActivateColliderClientRPC(bool on)
    {
        m_Collider.enabled = on;
    }

    /// <summary>
    /// Initialize size of the character
    /// </summary>
    public void SetSize()
    {
        transform.localScale = Settings.CharacterSizeFactor * CharacterLoader.GetCharacterData(m_Character.Value, destroy: true).Size * Vector3.one;
    }

    #endregion


    #region Death & Game Over Manipulators


    /// <summary>
    /// Remove Character display and 
    /// </summary>
    void OnDied()
    {
        //m_CharacterPreview.SetActive(false);
        ActivateActionComponent(false);
    }
  
    public void OnGameEnded(bool win)
    {
        // deactivate all "action" components
        ActivateActionComponent(false);

        // set game animation if still 
        m_AnimationHandler.GameOverAnimation(win);
    }

    /// <summary>
    /// activate / deactivate players "action" components (that allows player to take actions)
    /// </summary>
    /// <param name="active"></param>
    public void ActivateActionComponent(bool active)
    {
        m_Movement.enabled              = active;
        m_StateHandler.enabled          = active;
        m_CounterHandler.enabled        = active;

        m_SpellHandler.Activate(active);
        m_TriggerEffectHandler.Activate(active);

        if (m_AutoAttackHandler != null)
            m_AutoAttackHandler.enabled     = active;

        if (m_BehaviorTree != null)
        {
            m_BehaviorTree.Activate(active);
            m_BehaviorTree.enabled = active;
        }
    }

    #endregion

}
