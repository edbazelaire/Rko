using AI;
using Assets;
using Assets.Scripts.Game;
using Data;
using Data.DataStructures;
using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.PowerEffects;
using Enums;
using Game;
using Game.Character;
using Game.Loaders;
using Managers;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Controller : NetworkBehaviour
{
    #region Members      

    public static Action<Controller> OnDeathEvent;
    public Action OnDestroyedEvent;

    // ===================================================================================
    // PRIVATE VARIABLES 
    // -- Network Variables
    NetworkVariable<FixedString64Bytes>     m_PlayerName        = new NetworkVariable<FixedString64Bytes>("");
    NetworkVariable<ulong>                  m_PlayerId          = new NetworkVariable<ulong>(0);
    NetworkVariable<SPlayerData>            m_PlayerData        = new NetworkVariable<SPlayerData>();
    NetworkVariable<FixedString64Bytes>     m_Character         = new NetworkVariable<FixedString64Bytes>(ECharacter.None.ToString());
    NetworkVariable<int>                    m_CharacterLevel    = new NetworkVariable<int>(1);
    NetworkVariable<int>                    m_Team              = new NetworkVariable<int>(-1);
    NetworkVariable<bool>                   m_IsPlayer          = new NetworkVariable<bool>(true);
    NetworkVariable<bool>                   m_IsInitialized     = new NetworkVariable<bool>(false);

    // -- Server Variable
    RuneData[]      m_RuneData;
    CharacterData   m_CharacterData;
    Controller      m_SpawnOwner;

    // -- local variables
    bool m_IsActive     = false;
    bool m_GameRunning  = false;

    // -- Components & GameObjects
    BehaviorTree            m_BehaviorTree;
    Game.Character.AnimationHandler        m_AnimationHandler;
    GFXHandler              m_GFXHandler;
    Movement                m_Movement;
    protected Life          m_Life;
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
    public bool             IsActive            => m_IsActive;
    public bool             IsTargettable       => IsActive && Life.IsAlive && ! StateHandler.IsUnTargetable;
    public SPlayerData      PlayerData          => m_PlayerData.Value;
    public string           PlayerName          => m_PlayerName.Value.ToString();
    public string           Character           => m_Character.Value.ToString();
    public CharacterData    CharacterData       => m_StateHandler.CharacterData;
    public int              CharacterLevel      => m_CharacterLevel.Value;
    public RuneData[]       RuneData            => m_RuneData;
    public virtual int      Team                => m_Team.Value;
    public bool             IsPlayer            => m_IsPlayer.Value;
    public ulong            PlayerId            => m_PlayerId.Value;
    public ulong            AnalyticsId         => IsSpawn ? SpawnOwner.PlayerId : PlayerId;
    public bool             IsSpawn             => (int)PlayerId >= GameManager.SPAWN_CLIENT_ID;
    public Controller       SpawnOwner          => m_SpawnOwner;
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
    public Collider2D       Collider                    => m_GFXHandler.Collider;
    /// <summary> y position of the character's Height point </summary>
    public float            CharacterHeight             => Collider.transform.position.y + Collider.bounds.extents.y;
  
    #endregion


    #region Initialization 

    protected virtual void FindComponents()
    {
        // setup components
        m_Life                  = Finder.FindComponent<Life>(gameObject);
        m_EnergyHandler         = Finder.FindComponent<EnergyHandler>(gameObject);
        m_Movement              = Finder.FindComponent<Movement>(gameObject);
        m_SpellHandler          = Finder.FindComponent<SpellHandler>(gameObject);
        m_AnimationHandler      = Finder.FindComponent<Game.Character.AnimationHandler>(gameObject);
        m_GFXHandler            = Finder.FindComponent<GFXHandler>(gameObject);
        m_StateHandler          = Finder.FindComponent<StateHandler>(gameObject);
        m_CounterHandler        = Finder.FindComponent<CounterHandler>(gameObject);
        m_AutoAttackHandler     = Finder.FindComponent<AutoAttackHandler>(gameObject, throwError: false);
        m_TriggerEffectHandler  = Finder.FindComponent<TriggerEffectHandler>(gameObject);
        m_ClientAnalytics       = Finder.FindComponent<ClientAnalytics>(gameObject, throwError: false);

        // check behavior tree
        m_BehaviorTree = Finder.FindComponent<BehaviorTree>(gameObject, throwError: false);
        if (! IsServer && m_BehaviorTree != null)
            m_BehaviorTree.enabled = false;
    }

    /// <summary>
    /// Called when the controller is spawned on the network
    /// </summary>
    public override void OnNetworkSpawn()
    {
        ErrorHandler.Log("Controller.OnNetworkSpawn()", ELogTag.GameSystem);

        FindComponents();

        // add event to call UI initialization after NetworkVariable update 
        m_IsInitialized.OnValueChanged      += OnInitializedChanged;
        m_Life.OnDeathEvent                 += OnDied;
    }

    private void Update()
    {
        if (! IsSpawn)
            return;

        if (! GameManager.IsGameRunning)
            return;

        if (Life.Hp.Value <= 0)
        {
            ErrorHandler.Warning($"Found SPAWN ({m_Character.Value}) with no HP but not destroyed");
            m_Life.Kill(ignoreDeathEffects: true, force: true);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Wait for all data to be initialized on the server side before starting display and setup (ui, etc...)
    /// </summary>
    /// <param name="old"></param>
    /// <param name="newValue"></param>
    void OnInitializedChanged(bool old, bool newValue)
    {
        // add controller on client side
        if (!newValue)
            return;

        if (IsPlayer)
            transform.position = ArenaManager.Instance.Spawns[m_Team.Value][0].position;

        // set character is activated
        m_IsActive = true;

        if (! IsSpawn)
        {
            GameManager.Instance.AddController(PlayerId, this);
            if (PlayerId == NetworkManager.LocalClientId)
            {
                GameAnalyticsManager.Instance.SetUpLocalData(PlayerId, m_PlayerData.Value);
            }
        } else
        {
            GameManager.Instance.AddSpawnController(PlayerId, this);
            InitializeUI();
        }

        // GAME STARTED ?
        if (GameManager.Instance.IsGameStarted)
            // GAME STARTER : call OnGameStarted() method
            OnGameStarted();
        else
            // GAME NOT STARTED : register to event
            GameManager.GameStartedEvent += OnGameStarted;
    }

    /// <summary>
    /// Initialize the controller
    /// </summary>
    public virtual void Initialize(SPlayerData playerData, int team, bool isPlayer = true)
    {
        if (!IsServer)
            return;

        m_Team.Value            = team;
        m_IsPlayer.Value        = isPlayer;
        m_PlayerId .Value       = IsPlayer ? OwnerClientId : GameManager.Instance.GetNextBotId();

        transform.position = ArenaManager.Instance.Spawns[team][0].position;
        transform.rotation = Quaternion.Euler(0f, team == 0 ? 0f : -180f, 0f);

        InitializeCharacterData(playerData);

        gameObject.name = m_Character.Value.ToString() + "_" + PlayerId.ToString();
        m_IsInitialized.Value = true;
    }

    public void InitializeSpawn(SPlayerData playerData, int team, Controller spawnOwner)
    {
        if (!IsServer)
            return;

        m_Team.Value        = team;
        m_IsPlayer.Value    = false;
        m_PlayerId.Value    = GameManager.Instance.GetNextSpawnId();
        m_SpawnOwner        = spawnOwner;

        transform.rotation = Quaternion.Euler(0f, team == 0 ? 0f : -180f, 0f);
        InitializeCharacterData(playerData);

        gameObject.name = m_Character.Value.ToString() + "_" + PlayerId.ToString();
        m_IsInitialized.Value = true;
    }

    public void InitializeUI()
    {
        int team                = m_Team.Value;
        
        // init characte preview and animator
        InitializeGraphics();

        // display the player's ui 
        if (!IsSpawn)
            GameUIManager.Instance.SetPlayersUI(PlayerId, team);
        else
            AddSpawnUI();

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

        // setup Emots
        var emotList = m_PlayerData.Value.ProfileData.Emots
            .Select(emotName => Enum.TryParse<EEmot>(emotName.ToString(), out var result) ? result : EEmot.Trollexander)
            .ToList();
        GameUIManager.EmotsSectionUI.Initialize(emotList);

        // select auto attack by default (if not IsAutoTarget)
        bool isAutoTarget = true;           // TODO : use PlayerPref to set isAutoTarget or not by default
        if (! (isAutoTarget || SpellLoader.GetSpellData(m_SpellHandler.AutoAttack).IsAutoTarget))
            m_SpellHandler.AskSpellSelectionServerRPC(m_SpellHandler.AutoAttack);
    }

    public void InitializeGraphics()
    {
        // setup character preview
        m_GFXHandler.Initialize(m_Character.Value.ToString(), m_PlayerData.Value.Skin);

        // get animator
        Animator animator = Finder.FindComponent<Animator>(m_GFXHandler.CharacterPreview);
        m_AnimationHandler.Initialize(animator);
    }

    /// <summary>
    /// Implement all data related to the Character
    /// </summary>
    protected virtual void InitializeCharacterData(SPlayerData playerData)
    {
        if (! IsServer)
            return;

        m_PlayerData.Value      = playerData;
        m_PlayerName.Value      = playerData.PlayerName;
        m_Character.Value       = playerData.BuildData.Character.ToString();
        m_CharacterLevel.Value  = playerData.BuildData.CharacterLevel;

        // set all RuneData depending on activation type
        m_RuneData = new RuneData[playerData.BuildData.Runes.Length];
        for (int i = 0; i < m_RuneData.Length; i++)
        {
            // safety check
            if (i >= playerData.BuildData.Runes.Length)
            {
                ErrorHandler.Error("Bad index (" + i + ") for provided Runes of length : " + playerData.BuildData.Runes.Length);
                break;
            }

            // safety check
            int runeLevel = 1;
            if (i >= playerData.BuildData.RuneLevels.Length)
                ErrorHandler.Error("Bad index (" + i + ") for provided RuneLevels of length : " + playerData.BuildData.RuneLevels.Length);
            else
                runeLevel = playerData.BuildData.RuneLevels[i];

            m_RuneData[i] = SpellLoader.GetRuneData(playerData.BuildData.Runes[i], runeLevel);
            m_RuneData[i].SetActivation(CharacterBuildsCloudData.GetRuneActivationFromIndex(i));
        }

        CharacterData characterData = CharacterLoader.GetCharacterData(playerData.BuildData.Character.ToString(), playerData.BuildData.CharacterLevel, destroy: true);
        characterData.AddBonusStats(GetBonusStats());
        m_CharacterData = characterData;

        // initialize StateHandler with character data
        m_StateHandler.Initialize(characterData);

        // initialize SpellHandler with character's spells
        m_SpellHandler.Initialize(characterData.AutoAttack, characterData.SpecialAbility, characterData.Ultimate, playerData.BuildData.Spells.ToList(), playerData.BuildData.SpellLevels.ToList());

        // initialize MovementSpeed with character's speed
        m_Movement.Initialize(characterData.Speed);

        // init health and energy
        m_Life.Initialize(characterData.MaxHealth, characterData.GetInt(EStateEffectProperty.Shield));
        m_EnergyHandler.Initialize(characterData.BaseEnergy, characterData.MaxEnergy, characterData.PassiveEnergyGain);
        m_TriggerEffectHandler.Initialize(GetTriggerEffects(characterData));

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
        var characterData = CharacterLoader.GetCharacterData(m_Character.Value.ToString(), destroy: true);
        GameUIManager.Instance.CreateLinkedSpellTemplate(characterData.Ultimate, m_CharacterLevel.Value, 2);
        GameUIManager.Instance.CreateLinkedSpellTemplate(characterData.SpecialAbility, m_CharacterLevel.Value, 1);

        // create a SpellItemUI for each spell of the character
        for (int i = 0; i < m_SpellHandler.Spells.Count; i++)
        {
            ESpell spell = m_SpellHandler.Spells[i];
            
            // skip linked spells
            if (spell == characterData.Ultimate || spell == characterData.AutoAttack || spell == characterData.SpecialAbility)
                continue;

            GameUIManager.Instance.CreateSpellTemplate(m_SpellHandler.Spells[i], m_SpellHandler.SpellLevelsNet[i], i);
        }
    }

    protected void AddSpawnUI()
    {
        var spawnUIPrefab = AssetLoader.Load<SpawnUI>("SpawnUI", AssetLoader.c_SpawnUIContentPath);
        if (spawnUIPrefab == null)
        {
            ErrorHandler.Error("Unable to load health bar for Spawn");
            return;
        }

        var spawnUI = Instantiate(spawnUIPrefab, transform); // parent is fine
        spawnUI.Initialize(m_CharacterData.Size);

        // setup health bar
        PlayerBarUI healthBar = Finder.FindComponent<PlayerBarUI>(spawnUI.gameObject, "SpawnHealthBar");
        healthBar.Initialize(m_Life.Hp.Value, m_Life.MaxHp.Value);
        m_Life.Hp.OnValueChanged += healthBar.OnValueChanged;
        m_Life.MaxHp.OnValueChanged += healthBar.OnMaxValueChanged;

        // setup shield bar
        PlayerBarUI shieldBar = Finder.FindComponent<PlayerBarUI>(spawnUI.gameObject, "SpawnShieldBar");
        shieldBar.Initialize(m_Life.FinalShield.Value, m_Life.MaxHp.Value);
        m_Life.FinalShield.OnValueChanged += (int _, int newValue) => shieldBar.OnValueChanged(0, newValue);

        // setup energy bar
        PlayerBarUI energyBar = Finder.FindComponent<PlayerBarUI>(spawnUI.gameObject, "SpawnEnergyBar");
        energyBar.Initialize(m_EnergyHandler.Energy.Value, m_EnergyHandler.MaxEnergy.Value);
        m_EnergyHandler.Energy.OnValueChanged       += energyBar.OnValueChanged;
        m_EnergyHandler.MaxEnergy.OnValueChanged    += energyBar.OnMaxValueChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        OnDestroyedEvent?.Invoke();
        GameManager.GameStartedEvent -= OnGameStarted;
    }

    #endregion


    #region Activation / Deactivation

    public void Activate(bool activate)
    {
        if (activate == m_IsActive)
            return;

        m_IsActive = activate;
        gameObject.SetActive(activate);

        if (activate)
        {
            // check that OnGameStarted was called
            if (GameManager.IsGameRunning && ! GameRunning)
                OnGameStarted();
        }
    }

    #endregion


    #region Bonus Stats & Trigger Effects

    List<SCharacterStatScaling> GetBonusStats()
    {
        List < SCharacterStatScaling > bonusStats;
        if (m_PlayerData.Value.BonusStats == null)
            bonusStats = new List<SCharacterStatScaling>();
        else
            bonusStats = m_PlayerData.Value.BonusStats.ToList();

        if (m_RuneData == null)
        {
            ErrorHandler.Error("Rune Data not defined yet");
            return bonusStats;
        }

        // RUNES
        foreach (RuneData data in m_RuneData)
        {
            bonusStats.AddRange(data.GetBonusStats());
        }

        // POWER UPS
        if (! m_PlayerData.Value.PowerUps.IsNullOrEmpty())
        {
            foreach (var powerUp in m_PlayerData.Value.PowerUps)
            {
                SPowerEffect data = SpellLoader.GetPowerUp(powerUp.ToString(), m_CharacterLevel.Value);
                
                // CHECK : Power up not already in Runes
                if (Enum.TryParse(data.BaseName, out ERune rune) && m_PlayerData.Value.BuildData.Runes.Contains(rune))
                {
                    ErrorHandler.Warning("PowerUp " + data.Name + " was already in runes - skipped");
                    continue;
                }

                if (data.BonusStats != null)
                    bonusStats.AddRange(data.BonusStats);
            }
        }
        
        return bonusStats;
    }

    List<STriggerEffect> GetTriggerEffects(CharacterData characterData)
    {
        // get base raw list of trigger effects
        List<STriggerEffect> list = ! m_PlayerData.Value.TriggerEffects.IsNullOrEmpty() ? m_PlayerData.Value.TriggerEffects.ToList() : new();

        // CHARACTER : base trigger effects
        foreach (SPowerEffect data in characterData.SpecialPowers)
        {
            list.AddRange(data.TriggerEffects);
        }

        // RUNES
        if (m_RuneData == null)
        {
            m_RuneData = new RuneData[0];
            ErrorHandler.Warning("Rune Data not defined");
        }

        foreach (RuneData data in m_RuneData)
        {
            list.AddRange(data.GetTriggerEffects());
        }

        // set level of trigger effects = to character level and add it to list of trigger effects
        if (!m_PlayerData.Value.PowerUps.IsNullOrEmpty())
        {
            foreach (var powerUp in m_PlayerData.Value.PowerUps)
            {
                SPowerEffect data = SpellLoader.GetPowerUp(powerUp.ToString(), m_CharacterLevel.Value);

                // CHECK : Power up not already in Runes
                if (Enum.TryParse(data.BaseName, out ERune rune) && m_PlayerData.Value.BuildData.Runes.Contains(rune))
                {
                    ErrorHandler.Warning("PowerUp " + data.Name + " was already in runes - skipped");
                    continue;
                }

                data.SetParent(data.BaseName);
                list.AddRange(data.TriggerEffects);
            }
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
        if (!m_IsActive)
            return;

        // set to "true" the variable that the game has started
        m_GameRunning = true;

        // activate components allowing the player to make actions
        ActivateActionComponent(true);

        if (!IsServer)
            return;

        // when game starts, activate behavior tree
        if (! IsPlayer && m_BehaviorTree != null)
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
        Collider.enabled = on;
    }

    #endregion


    #region Death & Game Over Manipulators

    /// <summary>
    /// Remove Character display and 
    /// </summary>
    void OnDied()
    {
        if (m_SpellHandler.IsCasting)
            m_SpellHandler.CancelCast();

        if (IsSpawn)
        {
            Debug.Log("OnDied() : " + m_Character.Value);
            Destroy(gameObject);
            return;
        }

        ActivateActionComponent(false);
    }

    public void OnGameEnded(bool win)
    {
        // stop all current coroutines
        StopAllCoroutines();

        if (IsSpawn)
        {
            Destroy(gameObject);
            return;
        }

        // deactivate all "action" components
        ActivateActionComponent(false);

        // set game animation if still 
        m_AnimationHandler.GameOverAnimation(win);
    }

    /// <summary>
    /// activate / deactivate players "action" components (that allows player to take actions)
    /// </summary>
    /// <param name="active"></param>
    public virtual void ActivateActionComponent(bool active)
    {
        // movement is a client component too
        m_Movement.Activate(active);

        if (!IsServer)
            return;

        m_StateHandler.enabled          = active;
        m_CounterHandler.enabled        = active;

        m_SpellHandler.Activate(active);
        m_TriggerEffectHandler.Activate(active);

        if (m_AutoAttackHandler != null && m_SpellHandler.AutoAttack != ESpell.None)
            m_AutoAttackHandler.Activate(active);

        if (m_BehaviorTree != null)
        {
            m_BehaviorTree.Activate(active);
            m_BehaviorTree.enabled = active;
        }
    }

    #endregion
}
