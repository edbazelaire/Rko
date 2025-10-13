namespace Enums
{
    public enum EPopUpState
    {
        None = 0,   

        // -- screens
        MainMenuScreen              = 1,
        LoadingScreen               = 2,
        RewardsScreen               = 3,
        AchievementRewardScreen     = 4,
        LevelUpScreen               = 5,
        LobbyScreen                 = 6,
        ArenaPathScreen             = 7,   
        EternalMenageriePathScreen  = 38,   // <-->
        LeaguesPathScreen           = 8,
        ArenaBuildScreen            = 9,
        ArenaBuildConstructorScreen = 10,

        // -- info PopUps
        CollectableInfoPopUp        = 11,
        CharacterInfoPopUp          = 12,
        BossInfoPopUp               = 35,   
        RuneInfoPopUp               = 13,
        SpellInfoPopUp              = 14,
        StateEffectPopUp            = 15,
        TriggerEffectPopUp          = 16,
        RunePowerPopUp              = 17,
        PowerUpInfoScreen           = 18,
        PowerUpSelectionScreen      = 19,
        BossInfoScreen              = 20,

        // -- message PopUps
        QuickMessagePopUp           = 21,
        QuickRewardMessagePopUp     = 37,   
        MessagePopUp                = 22,
        ConfirmPopUp                = 23,
        PseudoPopUp                 = 24,
        ErrorMessagePopUp           = 25,
        ConfirmBuyPopUp             = 26,
        ConfirmBuyItemPopUp         = 27,
        ConfirmBuyBundlePopUp       = 28,
        ConfirmWatchAdPopUp         = 33,
        ConfirmUpgradeMasteryPopUp  = 36,       

        // -- options popup
        ArenaOptionsPopUp           = 34,     

        // -- config popup
        SettingsPopUp               = 29,
        MessageriePopUp             = 30,
        PromoCodePopUp              = 31,
        LoginPopUp                  = 32,

        // -- loading and other
        BlackScreen                 = 1000,
    }

    public enum EEnv
    {
        dev,
        beta
    }

    public enum EScreenAspect
    {
        Square,     // screen is more square shape than rectangle
        Normal,     // aspect ratio of the screen is normal
        Large,      // aspect ratio of the screen is considere larger than normal
    }

    public enum EGameMode
    {
        Arena,
        Ranked,
        Training,
    }

    public enum ESpecialGiftCodes
    {
        FULLUNLOCK = 0,
        MEOWMEOW = 1,
    }

    public enum EProduct
    {
        None = 0,

        // DEFAULT OFFERS ----------------------------------------------
        Gems_01 = 1,
        Gems_02 = 2,
        Gems_03 = 3,
        Gems_04 = 4,
        Gems_05 = 5,
        Gems_06 = 6,

        // SPECIAL PACKS ----------------------------------------------

    }

    public enum EArenaType
    {
        None = -1,

        //FireArena = 0,
        FrostArena          = 1,
        EternalMenagerie    = 2,
    }

    public enum EArenaDifficulty
    {
        Easy,
        Normal,
        Hard,
        Painful,
        Brutal,
        Torment,
        Infernal,
        //Savage,
        //Relentless,
        //Nightmare,
        //Cataclysmic,
        //Hell,
    }

    public enum EArenaMod
    {
        None = 0,

        Random      = 1,
        NoDeath     = 2,
        HardCore    = 3,
    }

    public enum EBlessing
    {
        None = 0,

        Alexander = 1,
    }

    public enum EPowerUpTag
    {
        Damage      = 0,
        Heal        = 1,
        Shield      = 2,
        Dots        = 3,
        Buffs       = 4,
        Energy      = 5,

        Reward      = 101,

        Tank        = 1001,
        Healer      = 1002,
        Assassin    = 1003,
        Mage        = 1004,
        Berserker   = 1005,
        Invocation  = 1006,

    }

    public enum ESkin
    {
        None = 0,

        // BOSS ====================================================
        Zorg_II     = 10001,
        Zorg_III    = 10002,
        Zorg_IV     = 10003,
        Zorg_V      = 10004,
    }

    public enum ECharacter
    {
        None = -1,

        Kahnan      = 0,
        Alexander   = 1,
        Srug        = 2,
        Marcus      = 3,
        Nagini      = 4,
        Subrog      = 5,
        NeedleJack  = 6,
        Iztac       = 7,
        Bulgor      = 8,
    }

    public enum EBoss
    {
        None        = -1,

        // Frost Arena
        IceGolem    = 0,
        MaiHau      = 1,
        Atassut     = 2,
        Sikunik     = 3,
        Fenris      = 4,

        // Eternal Menagerie
        Zorg        = 10,

        // ======================================================================
        // Mobs
        Lunassian               = 1001,
        VenomfangLunassian      = 1002,
        MoonclawLunassian       = 1003,
        AshhowlLunassian        = 1004,
        ElderLunassian          = 1005,
    }

    public enum ESpawn
    {
        None = -1,

        Stalacmite      = 1,
        DarkVeil        = 2,
        AzurePowerOrb   = 3,
        ChemicalSlime   = 4,
        Molken          = 5,
        ChaosTurtle     = 6,
        DoomCrystal     = 7,
        Fiorana         = 8,
        Noctrelle       = 9,
        Levrak          = 10,
        Voidgazer       = 11,
        Boomcrust       = 12,
        Toxstinger      = 13,
    }

    public enum ESpell
    {
        None = 0,

        Blazeburst          = 1,
        RockShower          = 2,
        FireBomb            = 3,
        Heal                = 4,
        SoulReaper          = 5,
        Invisibility        = 6,
        //AxeThrow            = 7,
        //Erasement           = 8,
        Rempart             = 9,
        IronSkin            = 10,
        //PoisonSpit          = 11,
        //PoisonFury          = 12,
        //ShadowRealm         = 13,
        //Curse               = 14,
        Torment             = 15,
        Sanctuary           = 16,
        ScorchedEarth       = 17,
        //BerserkerRage       = 18,
        SmokeBomb           = 19,
        //ArcticToundra       = 20,
        Blizzard            = 21,
        //FrozenOrb           = 22,
        FrostbiteTouch      = 23,
        Silence             = 24,
        //PyroBlast           = 25,
        FrostVenomBarrage   = 26,
        PyrotoxinMist       = 27,
        FireBarrage         = 28,
        IceLance            = 29,
        Needlestorm         = 30,
        FrostLeap           = 31,
        Meteor              = 32,
        Comets              = 33,
        //VenomousBite        = 34,
        ShadowShurikens     = 35,
        CursedTimes         = 36,
        // missing            = 37,
        //ThickSkin           = 38,
        //WinterProtection    = 39,
        ExtraHands          = 40,
        PoisonedBlade       = 41,
        //Corrupted           = 42,
        VoidEmbrace         = 43,
        PlagueArrows        = 44,
        //EmperorOfFlames     = 45,
        ExperimentalVial    = 46,
        //Vortex              = 47,
        ShardrotIceLance    = 48,
        SoulFreeze          = 49,
        DarkVeil            = 50,
        IceField            = 51,
        Stalacmite          = 52,
        PoisonDart          = 53,
        DarkEnergyField     = 54,
        //EclipseBlade        = 55,
        //ShadowStrike        = 56,
        //Duskfall            = 57,
        DirtBomb            = 58,
        ChemicalSlime       = 59,
        //Quickfix            = 60,
        //Leaner              = 61,  
        //Overdose            = 62,
        //Cryock              = 63,
        //Shardskin           = 64,
        //Icension            = 65,
        Maelstrom           = 66,
        Orblivion           = 67,
        Hellnova            = 68,
        //Lavachunk           = 69,
        IceBeam             = 70,
        Molken              = 71,
        HollowDagger        = 72,
        TrueshotBarrage     = 73,
        PiercingBolts       = 74,
        PressureShot        = 75,
        WeaponsSalve        = 76,
        GreatIceLance       = 77,

        // ======================================================================
        // Character ABILITIES
        // -- Alexander
        AxeThrow            = 1001,
        ThickSkin           = 1002,
        Erasement           = 1003,
        // -- Marcus
        FrozenOrb           = 1004,
        MarcusIceLance      = 1005,
        ArcticToundra       = 1006,
        // -- Srug
        Curse               = 1007,
        Vortex              = 1008,
        ShadowRealm         = 1009,
        // -- Kahnan
        Fireball            = 1010,
        IgnitionVeil        = 1011,
        PyroBlast           = 1012,
        // -- Nagini
        PoisonSpit          = 1013,
        VenomousBite        = 1014,
        PoisonFury          = 1015,
        // -- Iztac
        Cryock              = 1016,
        Shardskin           = 1017,
        Icension            = 1018,
        // -- NeedleJack
        Quickfix            = 1019,
        Leaner              = 1020,
        Overdose            = 1021,
        // -- Subrog
        EclipseBlade        = 1022,
        ShadowStrike        = 1023,
        Duskfall            = 1024,
        // -- Bulgor
        Lavachunk           = 1025,
        VolcanicMeditation  = 1026,
        MoltenSmash         = 1027,

        // ======================================================================
        // BOSSES ABILITIES
        // -- IceGolem
        IceRock             = 10001,
        Carapice            = 10002,
        IceRockNRoll        = 10003,
        CryoPunch           = 10004,
        FrostfistRain       = 10005,
        GroundBreaker       = 10006,

        // -- MaiHau
        Slicide             = 10101,
        SlIceBreaker        = 10102,
        XStrike             = 10107,
        RazorTempest        = 10103,
        Crosslice           = 10104,
        ExtraClaws          = 10105,
        FuryBond            = 10106,
        PoisonDarts         = 10108,

        // -- Atassut
        ChaosOrb            = 10201,
        Scythefall          = 10202,
        Umbracryx           = 10203,
        ShadowVeil          = 10204,
        AstralIcefall       = 10205,
        DarkstarDescent     = 10206,
        DoomCrystals         = 10207,

        // -- Sikunik
        BlueMeteor          = 10301,
        DragonicRest        = 10302,
        Soaring             = 10303,
        AzureDeflagration   = 10304,
        AzurePowerOrbs      = 10305,
        Eruptions           = 10306,
        Rainballs           = 10307,
        Disintegrate        = 10308,

        // -- Fenris
        IceClaws            = 10401,
        PackHunt            = 10402,
        FrostfangStrike     = 10403,

        // -- Zorg
        UnstableOrb         = 11001,
        MadnessWhispers     = 11002,
        //Tormentations       = 11003,
        Tormentations       = 11004,
        PandemoniumNova     = 11005,
        ImminentDoom        = 11006,
        AbyssalStrike       = 11007,

        // -- Lunassian
        LeatherSkin         = 100002,
        StalacmiteLuna      = 100003,
        FrostVenomOrb       = 100004,
        HealingField        = 100006,
        FrostCursedOrb      = 100007,
        MoonSilence         = 100009,
        FrostFireOrb        = 100010,
        Howling             = 100012,
        GlacialOrb          = 100013,
        FerociousBite       = 100015,

        // ======================================================================
        // SPAWN ABILITIES
        ChemicalSlimeAttack = 1000001,  // ChemicalSlime (aa)
        Sparks              = 1000002,  // Molken (aa)
        Wazaaah             = 1000003,  // Molken (ult)
        Tchaos              = 1000004,  // Chaos Turtle (ult)
        Beamanco            = 1000005,  // Doomcrystal (aa)
        SoulShards          = 1000006,  // Leverak  (aa)
        Netherglacier       = 1000007,  // Leverak  (sa)
        GlacialBreath       = 1000008,  // Leverak  (ult)
        AbyssalStare        = 1000009,  // Voidgazer (aa)
        Brainrot            = 1000010,  // Voidgazer (ult)
        Boomer              = 1000011,  // Boomcrust (aa)
        BoomBlast           = 1000012,  // Boomcrust (ult)
        Sting               = 1000013,  // Toxstinger (aa)
        ToxicBramble        = 1000014,  // Forana (aa)
        RottenScent         = 1000015,  // Forana (ult)
    }

    public enum ESpellType
    {
        None = -1,

        Projectile          = 0,
        InstantSpell        = 1,
        Aoe                 = 2,
        Counter             = 3,
        Jump                = 4,
        Zone                = 5,
        Buff                = 6,
        MultiProjectiles    = 7,
        Teleportation       = 8,
        MultiSpell          = 9,
        Mine                = 10,
        Spawner             = 11,
        Beam                = 12,
    }

    public enum ESpellElement
    {
        Neutral,

        Fire,
        Frost,
        Poison,
        Void,
    }

    public enum ESpecialValue
    {
        DamageReduction,
    }

    public enum EHitType
    {
        Damage,
        Heal,
        LifeSteal,
        Shield,
    }

    public enum EDamageCategory
    {
        Physical = 1,
        Magical  = 2,
    }

    public enum EHitCategory
    {
        Direct      = 1,
        Dot         = 2,
        Execution   = 3,
        TrueDamage  = 4,
        Piercing    = 5,
    }

    public enum ESpellCategory
    {
        Direct,
        Aoe,
    }

    public enum ESpellSelectionState
    {
        None,       // no specific activation state

        Inactive,   // spell is deactivated
        Cooldown,   // spell is in cooldown
        Brillance,  // spell has a special visual effect
    }

    public enum ERarety
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum ERuneActivation
    {
        None,

        Minor   = 1,
        Major   = 2,
        Primal  = 3
    }

    public enum ECollectableType
    {
        None,
        Character,
        Spell,
        Rune
    }

    public enum ERune
    {
        None = 0,

        FrostRune   = 1,
        FireRune    = 2,
        PoisonRune  = 3,
        CurseRune   = 4,

        ProtectorRune           = 5,
        BerserkerRune           = 6,
        ShieldRune              = 7,
        ResurrectionRune        = 8,
        InfernalProtectionRune  = 9,
        AncientAegisRune        = 10,
        HealRune                = 11,
        SoulSiffonRune          = 12,
        ShardcurseRune          = 13,
        ToxicWaterRune          = 14,
        ThermalShockRune        = 15,
        ParasiteRune            = 16,
        PoisonfangRune          = 17,
        VampiricRune            = 18,
        SwordRune               = 19,
        GlacialImpact           = 20,
        SparklingSpirit         = 21,
        CorruptedFlame          = 22,
        RottingFlame            = 23,
        FleetfootRune           = 24,
        HammeredRune            = 25,
        CorruptionRune          = 26,
        IceLanceMastery         = 27,
        EmperorOfFlames         = 28,
        ChaosTurtle             = 29,
        LethalityRune           = 30,
        IgnitedWeapon           = 31,
    }

    public enum EEffectType
    {
        None,

        Spell,
        StateEffect,
        Rune,
        PowerUp
    }

    public enum EOrderBy
    {
        None,

        Rarety,
        Level,
    }

    public enum EAppState
    {
        /// <summary> entry point </summary>
        Release,
        /// <summary> screen displayed between 2 scenes </summary>$
        LoadingScreen,
        /// <summary> main menu of the application </summary>
        MainMenu,
        /// <summary> setting up a lobby before the game starts</summary>
        Lobby,
        /// <summary> Game started </summary>
        InGame,

        Count
    }

    public enum EGameState
    {
        /// <summary> no GameState set </summary>
        None, 

        /// <summary> waiting for player data & connections </summary>
        WaitingForConnection,
        /// <summary> The GameManager received all data and clients, is now preparing the game (spawning players, UI, etc..) </summary>
        PreparingGame,
        /// <summary> all players are connected, playing the intro before starting the game </summary>
        Intro,
        /// <summary> game is currently running </summary>
        GameRunning,
        /// <summary> game is over </summary>
        GameOver,
    }

    public enum ETutoState
    {
        Presentation,
        Move,
        ClickSpell,
        Heal,
    }

    public enum EGameResult
    {
        Loss,
        Win,
        Draw
    }

    public enum ETriggerType
    {
        None = 0, 

        GameStart           = 1,        // activate effect when the game starts
        Time                = 2,        // activate effect when after a period of TIME
        Hp                  = 3,        // activate effect when HP goes below a threshold
        Shield              = 4,        // activate effect when SHIELD foes below a threshold
        Death               = 5,        // activate effect when the player DIES
        //TriggerEnter        = 6,        // activate effect when something ENTER collision with target
        //TriggerExit         = 7,        // activate effect when something EXIT collision with target
    }

    public enum ESpellEvent
    {
        None = 0,

        OnStartCast     = 100,
        OnCancelCast    = 200,
        OnCast          = 300,
        OnSpawn         = 400,
        OnActivation    = 450,
        OnTriggerEnter  = 475,
        OnHit           = 500,

        OnTriggerExit   = 875,
        OnDeactivation  = 900,
        OnEnd           = 1000,

        OnOver          = 1500
    }

    public enum EStateEffectEvent
    {
        None            = 0,    

        OnApplied       = 1,        // procs when a state effect is applied
        OnRefreshed     = 2,        // procs when a state effect STACK is refreshed
        OnConsumed      = 3,        // procs when a state effect STACK is consumed 
        OnRemoved       = 4,        // procs when a state effect STACK is removed without beeing consumed
        OnActivated     = 5,        // procs when a state effect is activated
        OnDeactivated   = 6,        // procs when a state effect is deactivated
        OnEnd           = 7,        // procs when a state effect is ending

        OnTick          = 100,      // procs at each tick of the state effect
    }

    public enum ESpellTrajectory 
    {
        None = -1,

        High            = 0,
        Curve           = 1,
        Straight        = 2,
        Diagonal        = 3,
        DiagonalMiddle  = 4,
    }

    public enum ESpellTarget
    {
        None = 0,

        EnemyZone = 1,
        AllyZone = 2,
        Free = 3,
        
        Self = 4,
        FirstAlly = 5,
        FirstEnemy = 6,

        EnemyZoneStart = 7,
        AllyZoneStart = 8,
        EnemyZoneCenter = 9,
        AllyZoneCenter = 10,
        EnemyZoneEnd = 11,
        AllyZoneEnd = 12,

        CurrentTarget = 50,

        Mirror = 101,               // target the symetrical point
        Fixed = 102,                // at a fixed distance
    }

    public enum EStateEffectTarget
    {
        None = 0,

        Self        = 1,
        Target      = 2,
        Ally        = 3,
        Enemy       = 4,

        AllEnemies  = 10,
        AllAllies   = 11,
    }

    public enum ETargetLayer
    {
        None = 0,

        Player      = 1,
        Invocation  = 2,
        Structure   = 3,
    }

    public enum ESpellSpawn
    {
        None,
        Ground,
        OnPosition,
    }

    public enum EMultiProjectileType
    {
        None,

        Line = 1,
        Random = 2,
        RandomLine = 4,
    }

    public enum EDimension
    {
        None = 0,

        X = 1, 
        Y = 2,
    }

    public enum EMultiSpellZone
    {
        None        = 0,

        Random      = 1,
        Line        = 2,
    }

    public enum EJumpType
    {
        None,

        Curve,
        Dash,
        Teleport,
        Charge,
        QuickJump,
    }

    public enum ESpellEffectType
    {
        AutoAttack,
        Spells,
        Both,
    }

    public enum ESpellSlot
    {
        AutoAttack      = 0,
        SpecialAbility  = 1,
        Ultimate        = 2,
        Spell1          = 3,
        Spell2          = 4, 
        Spell3          = 5, 
        Spell4          = 6,
    }

    public enum EListEvent
    {
        Add,
        Remove,
    }

    public enum ESpawnTarget
    {
        None = 0,

        Caster,                 // spawn of the center of the caster
        Target,                 // Controller touched by the spell
        OnSpell,                // spell location
        TargetPos,              // target of the spell (position where it's aim at)
        Mouse,                  // on the mouse location    
        MapCenter,              // center of the map (0, 0, 0)
    }

    public enum ESpawnLocation
    {
        None = 0,

        Center,                 // spawn at the Center of the provided position
        Ground,                 // spawn on the ground (y = 0) at the X requested position
        Hight,                  // spawn in the "Hight" at the X requested position
        Sky,                    // spawn in the sky at the X requested position
    }

    public enum EBodyPart
    {
        None,

        L_Hand,
        R_Hand,
        L_Foot,
        R_Foot,
        Body,
        Head,
        SpellSpawn,
        L_Arm,
        R_Arm,
        L_Leg,
        R_Leg,
        Weapon
    }

    public enum EStateEffect
    {
        // default effects
        None                = 0,
        Stun                = 1,
        Slow                = 7,
        Silence             = 14,
        Airborne            = 23,
        Grounded            = 30,
        Jump                = 11,
        Invulnerable        = 3,
        Uncontrollable      = 10,
        Enraged             = 21,
        Invisible           = 4,
        UnTargettable       = 20,
        Vanish              = 25,
        Taunt               = 28,   
        Unstopable          = 29,   
        Knockback           = 5,

        // Frost
        Frost               = 6,
        Frozen              = 2,
        Frostbite           = 34,   // <----->
        IceBreak            = 16,

        // Burn
        Burn                = 8,
        Combustion          = 22,
        Scorched            = 15,

        // Poison
        Poison              = 9,
        Infected            = 17,
        Infection           = 24,
        Rotten              = 32,       

        // Cursed
        Cursed              = 13,
        CorruptedPower      = 31,
        Malediction         = 18,

        // ======================================================================
        // SPELL EFFECTS
        IronSkin            = 12,
        VoidPact            = 19,               // CHANGE THAT NAME !!! EFFECT REPLACING AUTO ATTACK IN "CORRUPTED"
        Cleanse             = 26,
        RestlessBlade       = 27,   
        Purity              = 33,       

        // ======================================================================
        // Special Effects (boss)
        DarkRetribution     = 10001,
        DragonicRest        = 10002,
        AzurePowerOrb       = 10003,
        Howling             = 10004,
        Melting             = 10005,
        AlphaFrenzyStack    = 10006,
        BloodLust           = 10007,
        Unyelding           = 10008,
        EldersTouch         = 10009,

        // ======================================================================
        // Special Effects (characters)
        Junkheal            = 20001,
        HeavyHitter         = 20002,
        MoltenSpirit        = 20003,

        // ======================================================================
        // Special Effects (spawns)
        ChaosTurtleDiet     = 30001,
        BornedToBurn        = 30002,
        SoulFeast           = 30003,
        AbyssalPower        = 30004,

        // ======================================================================
        // Special Effects (animation)
        SpecialAnimation    = 100001,
        BlockMovement       = 100002, 
        BlockCast           = 100003,
        BlockEnergyGain     = 100004,
    }

    public enum EStateEffectType
    {
        Default,
        Incarnation,
        AutoAttackBuff,
        SpellBuff,
    }

    public enum EScalingDirection
    {
        None = 0,
        Up, 
        Down,
    }

    public enum EScalingType
    {
        Exponential = 0,
        Additive = 1,
        Multiply = 2,
    }

    public enum ERoundingType
    {
        None        = 0,
        Floor       = 1,
        Round       = 2,
        Ceil        = 3,
    }

    public enum ESpellProperty
    {
        None = 0,

        SpellTarget             = 26,   
        Heal                    = 1,
        Damage                  = 2,
        Cooldown                = 3,
        Duration                = 4,
        LifeSteal               = 5,
        NProjectiles            = 6,
        NWaves                  = 7,
        DelayBetweenLaunches    = 8,
        DelayBetweenWaves       = 9,
        ProjectileZoneSize      = 10,
        Size                    = 11,
        DurationTick            = 12,
        GrowSizeFactor          = 13,
        TickDamage              = 14,
        TickHeal                = 15,
        TickShield              = 16,
        TickEnergy              = 23,   
        Shield                  = 17,
        Delay                   = 18,
        ExecutionDamage         = 19,
        Charges                 = 20,
        Trajectory              = 21,
        AnimationTimer          = 22,
        Speed                   = 24,   
        MaxHit                  = 25,   

        // -- MFollowing
        IsFollowing                         = 27,   
        MFollowing_TargetToFollow           = 28,
        MFollowing_TargetOffset             = 29,   
        MFollowing_FollowingSpeed           = 30,  

        // -- MSpawnPosition
        OverridesSpawnPosition               = 31,
        MSpawnPosition_SpawnTarget          = 32,
        MSpawnPosition_SpawnLocation        = 33,
        MSpawnPosition_BodyPart             = 34,
        MSpawnPosition_IsFollowing          = 35,
        MSpawnPosition_Offset               = 36,   // <--->
    }

    public enum EStateEffectProperty
    {
        None = 0,

        Level                   = 24,   
        Size                    = 29,     
        Duration                = 1,
        MaxStacks               = 2,
        SpeedBonus              = 3,
        Shield                  = 4,
        BonusShieldPerc         = 21,
        Resistance              = 30,   // <---->
        ResistanceFix           = 5,
        ResistancePerc          = 6,
        ResistanceTick          = 26,   
        ResistanceTickPerc      = 27,   
        BonusDamage             = 7,
        BonusDamagePerc         = 8,
        BonusExecutionDamage    = 22,
        BonusExecutionDamagePerc = 23,
        Lethality               = 28, 
        BonusLifeSteal          = 9,
        MissingLifeFactor       = 10,
        Damage                  = 11,
        ExecutionDamage         = 25,   

        Tick                    = 12,
        TickDamage              = 13,
        TickHeal                = 14,
        TickShield              = 15,
        TickEnergy              = 115,   
        AttackSpeed             = 16,

        CastSpeed               = 17,
        ConsumeState            = 18,
        CooldownReduction       = 19,
        CooldownReductionPerc   = 20,

        BonusTickDamage         = 100,
        BonusTickDamagePerc     = 101,
        BonusTickHeal           = 102,
        BonusTickShield         = 103,
        BonusTickLifeSteal      = 119,  

        Heal                    = 104,
        LifeSteal               = 105,
        BonusHeal               = 106,
        BonusHealPerc           = 107,
        HealReduction           = 117,
        HealReductionPerc       = 118,

        EndDamage               = 108,
        EndHeal                 = 109,
        Stacks                  = 110,
        Hp                      = 111,
        BonusBurnDamage         = 112,
        BonusSlowPerc           = 113,
        Priority                = 114,
        Energy                  = 116,
        PassiveEnergyGain       = 121,      
        MaxEnergy               = 122,      // <-->
        MaxThresholdIndex       = 120,      
    }

    public enum EAnimation
    {
        Self = -1,                       // animation would be the name of the spell (for special spells)
        
        None = 0,
        
        CastShootStraight   = 1,
        CastShoot           = 2,
        CancelCast          = 3,
        Counter             = 4,
        Jump                = 5,
        CastAOE             = 6,
        Win                 = 7,
        Loss                = 8,
        CastBuff            = 9,
        CancelStateEffect   = 10,
        Stun                = 11,
        Frozen              = 12,
        Silenced            = 13,
        CastHight           = 14,
        Airborne            = 15,
        CastDiagonal        = 16,
        PrepareCharge       = 17,
        Charge              = 18,
    }

    public enum ECounterType
    {
        None,

        Proc,
        Block,
        Reflect,
        ApplyStateEffect,
    }

    public enum ECounterActivation
    {
        OnHitPlayer,        // activated when the player gets hit
        SelfTrigger,        // activated when the CounterSpell gets triggered
    }

    public enum ELogType
    {
        None = 0,

        Normal,
        Warning,
        Error,
        FatalError
    }

    public enum ELogTag
    {
        Debug = -1,
        Debugger = -2,

        None = 0,
        All = 1,

        System              = 2, 
        Network             = 3,
        Lobby               = 4,
        Achievements        = 5,

        // Game
        Game                = 100,    
        Gameplay            = 101,      
        Rewards             = 103,      // end game rewards handling
        GameSystem          = 104,      // login, new player, stages, ...
        SpellHandler        = 105,      // casting error/success messages, cancel, all stages of spell casting, spell ending, ...
        Animation           = 106,      // animations playing
        StateEffects        = 107,      // state effects changes
        Quests              = 113,      // quests state effects changes
        SpellGFX            = 108,      // spell graphics playing
        StateEffectGFX      = 109,      // spell graphics playing
        Movement            = 111,      // display movement variables and inputs
        Forces              = 112,      // display FORCES adding / removing

        // AI
        AI                  = 200,
        AIFinalDecision     = 201,
        AICheckers          = 202,
        AITaskAttack        = 203,
        AITaskMove          = 204,
        AITaskJump          = 205,
        AITaskCounter       = 206,
        AITaskAutoAttack    = 207,
        AITaskUseSpell      = 208,
        AIBtState           = 209,
        AITree              = 210,

        // CloudData
        CloudData           = 300,
        StatCloudData       = 301,

        // Services
        Services            = 400,
        Analytics           = 401,

        // Spells
        Spells              = 500,      // spell infos, start, end, stats, Colliders(), ...
        Projectiles         = 501,  
        Aoe                 = 502,  
        Buff                = 503,      
        Spawns              = 504,
        SpawnWaves          = 505,  
        MultiSpells         = 506,

        // Bonus Stats
        BonusStats              = 600,
        BonusDamage             = 601,
        BonusTickDamage         = 602,
        BonusExecutionDamage    = 603,
        BonusHeal               = 604,
        BonusTickHeal           = 605,
    }

    public enum ERewardType
    {
        Currency,
        Chest, 
        Collectable
    }

    public enum ESubRewardType
    {
        Currency,
        Spell,
        Rune,
        Character
    }

    public enum ECurrency
    {
        Gold        = 0,
        Gems        = 1,
        Real        = 2,
        Xp          = 3,
        TotalXp     = 4,
    }

    public enum EChest
    {
        // classics
        Common          = 0,
        Rare            = 1,
        Epic            = 2,
        Legendary       = 3,

        // fire chests
        Ember           = 4,
        Scorchstone     = 5,
        PyroMaster      = 6,

        // frost chests
        WintersBreath   = 7,
        Iceforged       = 8,
        FrostMaster     = 9,

        // poison chests
        RarePoison      = 10,    // TODO
        EpicPoison      = 11,    // TODO
        Vinebound       = 12,

        // poison chests
        RareVoid        = 13,    // TODO
        EpicVoid        = 14,    // TODO
        Abyssforged     = 15,
    }

    public enum EPowerOrb
    {
        PowerOrb,
    }

    public enum EChestAnimState
    {
        Idle,
        Ready,
        Opening,
    }

    public enum EChestLockState
    {
        Empty,
        Locked,
        Unlocking,
        Ready
    }

    public enum ESoundDuration
    {
        PlayOnce,       // play once, no adjustements
        Loop,           // loop the sound until the end
        Fit,            // play once but adjuste the duration to match the animation
    }

    public enum EAnimationUI
    {
        None,

        Pulse,
    }

    public enum EComparator
    {
        None = 0,

        Inf     = 1,
        InfEq   = 2,
        Equal   = 3,
        SupEq   = 4,
        Sup     = 5,
    }

    public enum EOrdering
    {
        None,

        Ascending,
        Descending,
    }

    public enum ERewardContext
    {
        None = 0,

        EndGameChest,
        Shop,
        Achievements,
        ArenaReward,
        LeagueReward,
    }

    public enum EAnalytics
    {
        GameEnded,
        CurrencyChanged,
        CollectableChanged,
        ChestOpened,
        AchievementRewardUnlocked,
        ShopTransaction,
        
        InGame,
        Damage,
        Heal,
        ArenaGameEnded,
        RankedGameEnded,

        PlayerData,
    }

    public enum EAnalyticsParam
    {
        // Game
        GameMode        = 1,
        Win             = 2,
        Character       = 3,
        CharacterLevel  = 4,
        Rune            = 5,
        Spells          = 6,
        SpellLevels     = 7,
        GameId          = 8,

        // InGame data
        Spell           = 50,
        HitType         = 51,

        // Currency
        Currency        = 101,
        Context         = 102,
        Qty             = 103,
    }

    public enum ELeague
    {
        None = 0,

        Iron,
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond,
        Master,
        Champion
    }

    public enum EStatData
    {
        None,

        PlayedGames,
        Wins,
        PvpWins,
    }

    public enum EAchievement
    {
        Lootless = 1,
    }

    public enum ETitle
    {
        None = 0,

        Dicks_Slayer        = 1,
        Noobs_Crusher       = 2,
        Rank_1              = 3,
        First               = 4,
        Heal_Checker        = 10,
        Alpha_Tester        = 11,
        The_Shadow          = 12,
        Le_Charclo          = 22,
        The_Lootless        = 23,      
        // -- Alexander
        The_Barrel          = 23,       
        ProtectorOfMaedonis = 24,
        Brutalexander       = 25,
        // -- Subrog
        Moon_Shadow         = 26,
        Eclipse_Assassin    = 27,
        Sneaky_Conquest     = 28,     
        // -- Marcus
        Frozenblade         = 29,
        The_Northen_General = 30,        
        //SomethingSomething  = 31,  
        // -- Strug
        The_Hollow          = 32,
        Vengefull_Spirit    = 33,        
        //SomethingSomething  = 34,       // <--->  

        // [Wins] Achievement ======================================================
        The_Unbeatable      = 13,

        // [Gold] Achievement ======================================================
        The_Hobo                = 6,
        The_Money_Maker         = 7,
        Gold_Digger             = 19,
        Cresus                  = 8,
        Aurum_Sovereign         = 20,
        Great_Monarch_of_Coins  = 21,   // -- not used yet

        // [SoloGames] Achievement ======================================================
        Forever_Alone       = 5,
        Lone_Wolf           = 9,

        // [Damage] Achievement ======================================================
        Slapper             = 14,
        Certified_Smacker   = 15,
        The_Pain_Train      = 16,
        Worldbreaker        = 17,
        Walking_Cataclysm   = 18,

        /************************************
         *              ARENAS              *
         ************************************/
        // Common Arena Achievements ==============================================
        TheDicey                = 901,
        Immortal                = 902,
        RandomGuy               = 903,

        // Frost Arena ======================================================
        Winter_Soldier = 1001,
        The_Iceborned           = 1002,
        Arctic_Vanquisher       = 1003,
        Glacial_Conqueror       = 1004,
        Eternal_Winter_King     = 1005,
        // --
        Legend_Of_The_North     = 1016,

        // Fire Arena ======================================================
        Pyro_Master             = 1104,
    }

    public enum EAvatar
    {
        None = 0,

        // ======================================================================================
        // Commons
        Blessed = 1,
        FireDemon       = 2,
        Meteor          = 3,
        Coin            = 4,
        MoneyMan        = 5,
        Sakura          = 6,
        ChibiSakura     = 7,
        Tao             = 8,
        ChibiTao        = 9,
        Assassin        = 12,
        PrincessLife    = 13,

        // ======================================================================================
        // Chibis
        BlueFlameChibi  = 101,
        CosmicChibi     = 102,
        DemonicChibi    = 103,
        FireChibi       = 104,
        FirstWinChibi   = 106,
        //ChibiMarcus = 107,


        // ======================================================================================
        // Frost Arena
        Snowman = 10,
        FrostMaster     = 11,
        ChibiFrostDemon = 105,
        QuiProQuo       = 501, 
        IceGolem        = 502,
        FrozenLand      = 503,
        NiceGolem       = 504, 

        // ======================================================================================
        Alexander       = 1000,
        YoungAlexander  = 1001,
        Marcus          = 1010,
        ChibiMarcus     = 1011,
        GeneralMarcus   = 1012,
        Kahnan          = 1030,
        Srug            = 1040, 
        ChibiSrug       = 1041,
        Nagini          = 1050,
        Chibini         = 1051,
        Subrog          = 1060,
        ChibiSubrog     = 1061,
        EclipseAssassin = 1062,
        NeedleJack      = 1070,
        Bulgor          = 1080,
        HappyBulgor     = 1081,
        Sophia          = 1090,

        // ======================================================================================
        // Achievements
        Dragon          = 10001,
    }

    public enum EBorder
    {
        None = 0,

        // Ranking Borders
        LeagueBronze    = 10,
        LeagueSilver    = 20,
        LeagueGold      = 30,
        LeaguePlatinum  = 40,
        LeagueDiamond   = 50,
        LeagueMaster    = 60,
        LeagueChampion  = 70,
        //LeagueLegend    = 80,

        // Special ranking border
        Rank1 = 91,

        // Others
        Sakura          = 101,
        Assassin        = 102,
        Devil           = 103,
        Flowers         = 104,
        Ribbon          = 105,
        Bloody          = 106,
        Plumo           = 107,
        Demonic         = 108,
        King            = 109,
        Noble           = 110, 
        Domination      = 111, 

        // Arena
        Fire            = 201,
        Frost           = 202,
        IceTouch        = 203,

        // Characters
        Alexander       = 1001,
        //Srug            = 1002,
        Marcus          = 1003,
        //Nagini          = 1004,
        Subrog          = 1005,
        //NeedleJack      = 1006,
        //Iztac           = 1007,
        //Bulgor          = 1008,
        //Kahnan          = 1009,
    }

    public enum EBadge
    {
        None = 0,

        // Games
        PlayedGame  = 1,
        Wins        = 2,
        Damage      = 3,
        Heals       = 4,

        // -- speciaux
        DamageDealer = 50,
        HeartOfFire = 51,

        // -- pvp
        PvpGamesWon     = 100,
        // -- solo
        SoloGames       = 201,
        LoneWolf        = 202,

        // -- collectables
        GoldCollector   =  501,
        CardCollector   = 502,
        XpCollector     = 503,

        // Legendary
        DemonLord       = 1001,
        Gladiator       = 1002,
        Rank1           = 1003,

        // Characters Achievements
        BattleAxe       = 2001,
        EclipseBlade    = 2002,
        IceLance        = 2003,

        // Arena Frost
        SeedOfChance    = 10000,
        UncertainShot   = 10001,
        MasterOfChaos   = 10002,        
        RandomEntity    = 10003,            // TODO (icon not made)  ----------------------------------
        Undying         = 10004,            // TODO (icon not made)  ----------------------------------
        ItSnowProblem   = 10005,
        FrozenBlade     = 10006,
        LegendOfTheNorth = 10007,
        Spicy           = 10008,
        FrozenSkull     = 10009,
        FrozenHeart     = 10010,
        HiceBreaker     = 10011,
    }

    public enum EAchievementReward
    {
        Badge       = 1,
        Avatar      = 2,
        Border      = 3,
        Title       = 4,
        Emot        = 5,
    }

    public enum EEmot
    {
        // ====================================================
        // Common
        SadKitty            = 2,
        Pidgeon             = 4,
        ThumbUp             = 6,
        Mehven              = 8,
        MeGusta             = 9,
        
        // ====================================================
        // Sophia 
        HowCouldYou         = 10,
        IBelieveInYou       = 11,
        Kawai               = 12,
        PleaseDont          = 13,
        Kaching             = 18,
        SadPrincess         = 20,       

        // ====================================================
        // Alexander
        Pokerface           = 5,
        Trollexander        = 7,
        Hey                 = 17,
        BigMuscles          = 15,
        CuteAlexander       = 16,
        Hahalexander        = 21,
        UGotThis            = 22,
        RedBarrel           = 23,
        Hilexander          = 24,
        Sadlexander         = 25,
        BigPunch            = 26,   

        // ====================================================
        // Kahanan
        Ah                  = 1,

        // ====================================================
        // Marcus
        Omg                 = 3,
        Yada                = 19, 
        Hilarious           = 36,
        PissedOfMarcus      = 37,
        Duuuude             = 38,
        CheckThisOut        = 39,
        Looser              = 40,
        Markiss             = 41,
        NiceOne             = 42,       // <---->

        // ====================================================
        // Subrog
        DeathSentence       = 27,   
        HiThere             = 28,   
        NotFair             = 29,   
        ServesYouRight      = 30,   
        Subdead             = 31,   
        Sublove             = 32,   
        Subrage             = 33,   
        UhOh                = 34,         
        InnocentFace        = 35,

        // ====================================================
        // Srug
        AngrySpirit         = 101,
        ChillSpirit         = 102,
        LoveSpirit          = 103,
        LaughingSpirit      = 104,
        CantWatch           = 105,
        SoWonderful         = 106,
        Bwaalwaala          = 107,
        Whut                = 108,
        HighSpirit          = 109,

        // ====================================================
        // Bulgor
        OhLord = 14,
    }

    public enum EBoost
    {
        None = 0,

        ChestSpeedBoost = 1,
    }

    public enum EVolumeOption
    {
        MasterVolume,
        SoundEffectsVolume,
        MusicVolume,
    }

    public enum ECaptionType
    {
        None,

        Normal,
        Exclamation
    }

    public enum ECaptionColor
    {
        None,
        White,
        Black,
    }

    public enum EAuthServices
    {
        Anonymous,
        UnityPlayerAccount,
        Apple,
        Google,
    }
}