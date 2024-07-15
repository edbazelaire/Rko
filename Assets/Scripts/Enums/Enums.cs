namespace Enums
{
    public enum EPopUpState
    {
        None = 0,   

        // -- screens
        LoadingScreen,
        RewardsScreen,
        AchievementRewardScreen,
        LevelUpScreen,
        LobbyScreen,
        ArenaPathScreen,
        LeaguesPathScreen,

        // -- info PopUps
        CollectableInfoPopUp,
        CharacterInfoPopUp,
        SpellInfoPopUp,
        StateEffectPopUp,
        RuneSelectionPopUp,

        // -- message PopUps
        MessagePopUp,
        PseudoPopUp,
        ErrorMessagePopUp,
        ConfirmBuyPopUp,
        ConfirmBuyItemPopUp,
        ConfirmBuyBundlePopUp,

        // -- config popup
        SettingsPopUp,
    }

    public enum EGameMode
    {
        Arena,
        Ranked,
        Training,
    }

    public enum EArenaType
    {
        FireArena,
        FrostArena,
    }

    public enum ECharacter
    {
        Kahnan,
        Alexander,
        Srug,
        Marcus,
        Nagini,

        Count
    }

    public enum ESpell
    {
        RockShower,
        Blazeburst,
        Fireball,
        FireBomb,
        Heal,
        Counter,
        Invisibility,
        AxeThrow,
        Erasement,
        Rempart,
        IronSkin,
        PoisonSpit,
        PoisonFury,
        ShadowRealm,
        Curse,
        Torment,
        Sanctuary,
        ScorchedEarth,
        BerserkerRage,
        SmokeBomb,
        ArcticToundra,
        Blizzard,
        FrozenOrb,
        FrostbiteTouch,
        Silence,
        PyroBlast,
        FrostVenomBarrage,
        PyrotoxinMist,
        FireBarrage,
        IceLance,
        Needlestorm,
        FrostLeap,
        Meteor,
        Comets,
        VenomousBite,
        ShadowShurikens,
        CursedTimes,
        ThickSkin,
        WinterProtection,
        ExtraHands,
        PoisonedBlade,
        Corrupted,
        VoidEmbrace,
        PlagueArrows,
        EmperorOfFlames,
        ToxicFiole,

        Count
    }

    public enum ESpellType
    {
        Projectile,
        InstantSpell,
        Aoe,
        Counter,
        Jump,
        Zone,
        Buff,
        MultiProjectiles,

        Count
    }

    public enum ERarety
    {
        Common,
        Rare,
        Epic,
        Legendary
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
        None,
        FrostRune = 1,
        FireRune = 2,
        PoisonRune = 3,
        CurseRune = 4,

        ProtectorRune,
        BerserkerRune,
        ShieldRune,
        ResurrectionRune,
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

    public enum ESpellActivationEvent
    {
        GameStart,
        Time,
        Hp,
        Shield,
        Death,
    }

    public enum ESpellEvent
    {
        None,

        OnStartCast,
        OnCast,
        OnSpawn,
        OnHit,
        OnEnd,
    }

    public enum ESpellTrajectory 
    {
        Hight,
        Curve,
        Straight,
        Diagonal,

        Count
    }

    public enum ESpellTarget
    {
        None,

        EnemyZone,
        AllyZone,
        Free,
        
        Self,
        FirstAlly,
        FirstEnemy,

        EnemyZoneStart,
        AllyZoneStart,
        EnemyZoneCenter,
        AllyZoneCenter,
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

        Line,
        Random,
    }

    public enum EJumpType
    {
        None,

        Curve,
        Dash,
        Teleport,
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
    }

    public enum ESpawnLocation
    {
        None = 0,

        Center,                 // spawn at the Center of the provided location
        Ground,                 // spawn at the feets of the caster
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
    }

    public enum EStateEffect
    {
        // default effect
        None,

        // default effect
        Stun,
        Frozen,
        Invulnerable,
        Invisible,

        // knockback effects
        Knockback,

        // slow effects
        Frost,
        Slow,

        // tick effects
        Burn,
        Poison,

        Uncontrollable,
        Jump,
        IronSkin,
        Cursed,
        Silence,
        Scorched,
        IceBreak,
        Infected,
        Malediction,
        VoidPact,
        UnTargettable,
        SpecialAnimation,
    }

    public enum ESpellProperty
    {
        Non,

        Heal,
        Damages,
        Cooldowns,
        Duration,
        LifeSteal,
        NProjectiles,
        NWaves,
        DelayBetweenLaunches,
        DelayBetweenWaves,
        ProjectileZoneSize,
    }

    public enum EStateEffectProperty
    {
        None = 0,

        Duration                = 1,
        MaxStacks               = 2,
        SpeedBonus              = 3,
        Shield                  = 4,
        ResistanceFix           = 5,
        ResistancePerc          = 6,
        BonusDamages            = 7,
        BonusDamagesPerc        = 8,
        BonusLifeSteal          = 9,
        MissingLifeFactor       = 10,
        Damages                 = 11,

        Tick                    = 12,
        TickDamages             = 13,
        TickHeal                = 14,
        TickShield              = 15,
        AttackSpeed             = 16,

        CastSpeed               = 17,
        ConsumeState            = 18,
        CooldownReduction       = 19,
        CooldownReductionPerc   = 20,

        BonusTickDamages        = 100,
        BonusTickDamagesPerc    = 101,
        BonusTickHeal           = 102,
        BonusTickShield         = 103,

        Heal,
        LifeSteal,
        BonusHeal,
        BonusHealPerc,

        EndDamages,
        EndHeal,
        Stacks,
        Hp,
    }

    public enum EAnimation
    {
        None,
        CastShootStraight,
        CastShoot,
        CancelCast,
        Counter,
        Jump,
        CastAOE,
        Win,
        Loss,
        CastBuff,
        CancelStateEffect,
        Stun,
        Frozen,
        Silenced,
    }

    public enum ECounterType
    {
        None,

        Proc,
        Block,
        Reflect,
        ApplyStateEffect
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
        Spells              = 102,      // spell infos, start, end, stats, Colliders(), ...
        Rewards             = 103,      // end game rewards handling
        GameSystem          = 104,      // login, new player, stages, ...
        SpellHandler        = 105,      // casting error/success messages, cancel, all stages of spell casting, spell ending, ...
        Animation           = 106,      // animations playing
        StateEffects        = 107,      // state effects changes
        SpellGFX            = 107,      // spell graphics playing

        // AI
        AI                  = 200,
        AIFinalDecision     = 201,
        AICheckers          = 202,
        AITaskAttack        = 203,
        AITaskMove          = 204,
        AITaskJump          = 205,
        AITaskCounter       = 206,

        // CloudData
        CloudData           = 300,
        StatCloudData       = 301,

        // Services
        Services            = 400,
        Analytics           = 401,
    }

    public enum ERewardType
    {
        Currency,
        Chest, 
        Collectable
    }

    public enum ECurrency
    {
        Golds,
        Gems,
        Dollars,
        Xp,
    }

    public enum EChest
    {
        // classics
        Common,
        Rare,
        Epic,
        Legendary,

        // fire chests
        Ember,
        Scorchstone,
        PyroMaster,

        // frost chests
        WintersBreath,
        Iceforged,
        FrostMaster,
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
    }

    public enum EAnalyticsParam
    {
        // Game
        GameMode        = 1,
        Win             = 2,
        Character       = 3,
        CharacterLevel  = 4,

        // InGame data
        Spell           = 50,
        HitType         = 51,

        // Currency
        Currency        = 101,
        Context         = 102,
        Qty             = 103,
    }

    public enum EHitType
    {
        Damage,
        Heal,
        LifeSteal
    }

    public enum ELeague
    {
        None = 0,

        Iron,
        Bronze,
        Silver,
        Gold,
        Platinium,
        Diamond,
        Champion
    }

    public enum EStatData
    {
        None,

        PlayedGames,
        Wins,
        PvpWins,
    }

    public enum ETitle
    {
        None = 0,

        Dicks_Slayer,
        Noobs_Crusher,
        Rank_1,
        First,
        Forever_Alone,
        Le_Charclo,
        The_Money_Maker,
        Cresus,
        Lone_Wolf,
        Pyro_Master,
        Glacial_Conqueror,
    }

    public enum EAvatar
    {
        None = 0,

        Blessed         = 1,
        FireDemon       = 2,
        Meteor          = 3,
        Coin            = 4,
        MoneyMan        = 5,
        Sakura          = 6,
        ChibiSakura     = 7,
        Tao             = 8,
        ChibiTao        = 9,
        Snowman         = 10,
        FrostMaster     = 11,

        BlueFlameChibi  = 101,
        CosmicChibi     = 102,
        DemonicChibi    = 103,
        FireChibi       = 104,
        ChibiFrostDemon = 105,

        Alexander       = 1000,
        YoungAlexander  = 1001,
        Marcus          = 1010,
        Bruh            = 1020,
        Kahnan          = 1030,
        Srug            = 1040,

    }

    public enum EBorder
    {
        None = 0,

        // Ranking Borders
        LeagueBronze    = 1,
        LeagueSilver    = 2,
        LeagueGold      = 3,
        //LeaguePlatinium = 4,
        //LeagueRubis     = 5,
        //LeagueDiamant   = 6,
        //LeagueLegend    = 7,

        // Special ranking border
        Rank1 = 11,

        // Others
        Sakura = 101,

        // Speical Events
        Fire = 201,
        Frost = 202,

    }

    public enum EBadge
    {
        None = 0,

        // Games
        PlayedGame  = 1,
        Wins        = 2,
        Damages     = 3,
        Heals       = 4,
        // -- pvp
        PvpGamesWon = 100,
        // -- solo
        SoloGames   = 201,
        LoneWolf    = 202,

        // Collectables
        GoldCollector   = 501,
        CardCollector   = 502,
        XpCollector     = 503,

        // Legendary
        DemonLord   = 1001,
        Gladiator   = 1002,
        Rank1       = 1003,
    }

    public enum EAchievementReward
    {
        None = 0,

        Badge       = 1,
        Avatar      = 2,
        Border      = 3,
        Title       = 4,
    }

    public enum EVolumeOption
    {
        MasterVolume,
        SoundEffectsVolume,
        MusicVolume,
    }
}