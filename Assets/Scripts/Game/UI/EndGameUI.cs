using Analytics.Events;
using Assets;
using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Assets.Scripts.Tools;
using Data.GameManagement;
using Enums;
using Game;
using Game.UI.EndGameUI;
using Inventory;
using Managers;
using Menu.Common.Rewards;
using Network;
using Save;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using Tools.Animations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


enum EEndGameState
{
    Inactive,
    Intro,
    PowerUps,
    Rewards,
    Exit,
}


public class EndGameUI : MObject
{
    #region Members

    const string GOLD_FORMAT = "+ {0}";

    // Data
    EEndGameState m_State;
    EGameResult m_GameResult;
    bool m_IsBossFight = false;
    // -- arena
    ArenaData m_ArenaData = null;
    int m_CurrentLevel = 0;
    int m_CurrentStage = 0;

    // Components
    EndGameAnalyticsUI  m_EndGameAnalyticsUI;
    GameObject          m_RewardsSection;
    GameObject          m_Background;
    TMP_Text            m_TitleText;
    GameObject          m_RewardsContent;
    PowerUpSection      m_PowerUpSection;
    GameObject          m_XpRewardDisplay;
    TMP_Text            m_XpQty;
    GameObject          m_GoldRewardDisplay;
    TMP_Text            m_GoldQty;
    GameObject          m_GemsRewardDisplay;
    TMP_Text            m_GemsQty;
    GameObject          m_OrbPowerRewardDisplay;
    TMP_Text            m_OrbPowerRewardQty;
    PowerOrbContainer   m_PowerOrbContainer;
    GameObject          m_RefreshTokenDisplay;
    Image               m_ChestRewardIcon;
    Button              m_LeaveButton;
    Button              m_DetailsButton;
    GameObject          m_Fireworks;

    public EndGameAnalyticsUI EndGameAnalyticsUI => m_EndGameAnalyticsUI;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        m_Background                = Finder.Find(gameObject, "Background");
        m_Fireworks                 = Finder.Find(gameObject, "Fireworks");
        m_TitleText                 = Finder.FindComponent<TMP_Text>(gameObject, "TitleText");

        m_EndGameAnalyticsUI        = Finder.FindComponent<EndGameAnalyticsUI>(gameObject, "EndGameAnalyticsUI");
        m_PowerUpSection            = Finder.FindComponent<PowerUpSection>(gameObject, "PowerUpSection");

        m_RewardsSection            = Finder.Find(gameObject, "RewardsSection");
        m_LeaveButton               = Finder.FindComponent<Button>(gameObject, "LeaveButton");
        m_DetailsButton             = Finder.FindComponent<Button>(gameObject, "DetailsButton");
        m_RewardsContent            = Finder.Find(gameObject, "RewardsContent");
        m_XpRewardDisplay           = Finder.Find(m_RewardsContent, "XpRewardDisplay");
        m_XpQty                     = Finder.FindComponent<TMP_Text>(m_XpRewardDisplay, "Qty");
        m_GoldRewardDisplay         = Finder.Find(m_RewardsContent, "GoldsRewardDisplay");
        m_GoldQty                   = Finder.FindComponent<TMP_Text>(m_GoldRewardDisplay, "Qty");
        m_GemsRewardDisplay         = Finder.Find(m_RewardsContent, "GemsRewardDisplay");
        m_GemsQty                   = Finder.FindComponent<TMP_Text>(m_GemsRewardDisplay, "Qty");
        m_OrbPowerRewardDisplay     = Finder.Find(m_RewardsContent, "OrbPowerRewardDisplay");
        m_OrbPowerRewardQty         = Finder.FindComponent<TMP_Text>(m_OrbPowerRewardDisplay, "Qty");
        m_PowerOrbContainer         = Finder.FindComponent<PowerOrbContainer>(m_RewardsContent);
        m_RefreshTokenDisplay       = Finder.Find(m_RewardsContent, "RefreshTokenDisplay");
        m_ChestRewardIcon           = Finder.FindComponent<Image>(m_RewardsContent, "ChestRewardIcon");
    }

    public override void Initialize()
    {
        base.Initialize();

        SetUpData();

        m_PowerUpSection.Initialize();
        m_EndGameAnalyticsUI.Initialize();

        SetState(EEndGameState.Inactive);
    }

    void SetUpData()
    {
        m_IsBossFight = LobbyHandler.Instance.GameMode == EGameMode.Arena && ProgressionCloudData.CurrentArena.IsBoss();
        
        switch (LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                m_ArenaData = ProgressionCloudData.CurrentArena.LoadArenaData();
                m_CurrentLevel = ProgressionCloudData.CurrentArena.Level;
                m_CurrentStage = ProgressionCloudData.CurrentArena.Stage;
                break;

            case EGameMode.Ranked:
                m_ArenaData = null;
                m_CurrentLevel = ProgressionCloudData.CurrentLeagueLevel;
                m_CurrentStage = ProgressionCloudData.CurrentLeagueStage;
                break;

            default:
                m_ArenaData = null;
                m_CurrentLevel = 0;
                m_CurrentStage = 0;
                break;
        }
    }

    // Use this for initialization
    public void Activate(EGameResult gameResult, bool preventiveLossApplied)
    {
        ErrorHandler.Log("END OF GAME : " + LobbyHandler.Instance.GameMode + " - " + gameResult.ToString(), ELogTag.GameSystem);

        // make sure that into is deactivated
        GameUIManager.IntroGameUI.Deactivate();

        // save if this is win or not
        m_GameResult = gameResult;

        // set color and text according to context
        SetUpTitle();

        // handle data processing before animation & stuff
        HandleEndGameData();
        HandleProgression(preventiveLossApplied);

        // clean game data
        PlayerPrefs.SetString(EPlayerPref.CurrentGameId.ToString(), "");

        // activate game object
        gameObject.SetActive(true);

        // cancel methods in TimeWrapper
        TimeErrorWrapper.Instance.Cancel(GameManager.TIME_WRAPPER_ID);

        // go to IntroState
        SetState(EEndGameState.Intro);
    }


    void Leave()
    {
        // stop animation coroutine
        StopAllCoroutines();

        // reset network manager
        NetworkManager.Singleton.Shutdown();

        // reset GameManager
        if (GameManager.Exists)
            GameManager.Instance.Shutdown();

        // load MainMenu
        SceneLoader.Instance.LoadScene("MainMenu");
    }

    #endregion


    #region State Management

    void NextState()
    {
        SetState(m_State + 1);
    }

    void SetState(EEndGameState state)
    {
        m_State = state;

        switch (state)
        {
            case EEndGameState.Inactive:
                m_PowerUpSection.Activate(false); 
                m_RewardsSection.SetActive(false);
                m_EndGameAnalyticsUI.gameObject.SetActive(false); 
                gameObject.SetActive(false);
                break;

            case EEndGameState.Intro:
                // start animation coroutine
                StartCoroutine(IntroAnimation());
                break;

            case EEndGameState.PowerUps:
                // check if should skip power up selection
                if (m_GameResult != EGameResult.Win || ! m_IsBossFight || ProgressionCloudData.CurrentArena.IsOver())
                {
                    NextState();
                    return;
                }

                DisplayArenaPowerUps();
                break;

            case EEndGameState.Rewards:
                DisplayRewards();
                break;

            case EEndGameState.Exit:
                Leave();
                break;
        }
    }

    #endregion


    #region Arena PowerUp

    void DisplayArenaPowerUps()
    {
        m_PowerUpSection.OnEndEvent += NextState;
        m_PowerUpSection.Activate(true);
    }

    #endregion


    #region Reward & EndGame

    void DisplayRewards()
    {
        ErrorHandler.Log("HandleReward() : start", ELogTag.Rewards);

        m_GemsRewardDisplay.SetActive(false);
        m_XpRewardDisplay.SetActive(false);
        m_GoldRewardDisplay.SetActive(false);
        m_OrbPowerRewardDisplay.SetActive(false);   
        m_RefreshTokenDisplay.SetActive(false);   

        m_RewardsSection.SetActive(true);

        SRewardCalculator rewardCalculator = m_GameResult == EGameResult.Win ? Rewarder.WinGameReward : Rewarder.LossGameReward;
        rewardCalculator.SetCurrencyMultiplicator(CalculateCurrencyMultiplicator());

        // specific reward for tutorial
        if (LobbyHandler.Instance.IsTuto)
        {
            rewardCalculator = new SRewardCalculator(0, 0, 0, 0, new List<SChestDropPercentage>() {
                new SChestDropPercentage(new Dictionary<EChest, float>() { { EChest.Common, 1f } })
            });
        }

        // no rewards for training mode
        else if (LobbyHandler.Instance.GameMode == EGameMode.Training)
        {
            rewardCalculator = new SRewardCalculator(0, 0, 0, 0, new List<SChestDropPercentage>());
        }

        // no rewards for not boss fights in Arena
        else if (LobbyHandler.Instance.GameMode == EGameMode.Arena && ! m_IsBossFight)
        {
            rewardCalculator = new SRewardCalculator(0, 0, 0, 0, new List<SChestDropPercentage>());
        }

        // ----------------------------------------------------------------------------
        // Xp   
        int xp = rewardCalculator.GetXp();
        ErrorHandler.Log("         + XP : " + xp, ELogTag.Rewards);
        if (xp > 0)
        {
            m_XpRewardDisplay.SetActive(true);
            m_XpQty.text = string.Format(GOLD_FORMAT, xp);
            NotificationCloudData.AddXp(xp);
        }

        // ----------------------------------------------------------------------------
        // GOLDS   
        int golds = rewardCalculator.GetGold();
        ErrorHandler.Log("         + GOLD : " + golds, ELogTag.Rewards);
        if (golds > 0)
        {
            m_GoldRewardDisplay.SetActive(true);
            m_GoldQty.text = string.Format(GOLD_FORMAT, golds);
            InventoryManager.UpdateCurrency(ECurrency.Gold, golds, ERewardContext.EndGameChest.ToString());
        }

        // ----------------------------------------------------------------------------
        // Gems   
        int gems = rewardCalculator.GetGems();
        ErrorHandler.Log("         + XP : " + xp, ELogTag.Rewards);
        if (gems > 0)
        {
            m_GemsRewardDisplay.SetActive(true);
            m_GemsQty.text = string.Format(GOLD_FORMAT, gems);
            InventoryManager.UpdateCurrency(ECurrency.Gems, gems, ERewardContext.EndGameChest.ToString());
        }

        // ----------------------------------------------------------------------------
        // CHESTS
        // init chests rewards to empty list
        List<EChest> chests = new();
        ErrorHandler.Log("         + CHESTS : " + chests.Count, ELogTag.Rewards);

        // check if any index is available to store the chest (otherwise : no chest reward)
        if (InventoryManager.GetFirstAvailableIndex(out int index))
            chests = rewardCalculator.GetChests();

        if (chests.Count == 0)
        {
            m_ChestRewardIcon.gameObject.SetActive(false);
        }
        else
        {
            m_ChestRewardIcon.gameObject.SetActive(true);
            if (chests.Count > 1)
                ErrorHandler.Warning("Multiple chests provided to EndGameUI : case not handled");

            m_ChestRewardIcon.sprite = AssetLoader.LoadChestIcon(chests[0]);
            InventoryManager.AddChest(chests[0]);
        }

        // ----------------------------------------------------------------------------
        // Orb Power  
        if (LobbyHandler.Instance.GameMode == EGameMode.Arena && m_GameResult == EGameResult.Win)
        {
            // setup quantity of power orb collected
            SPowerOrb currentPowerOrb = ProgressionCloudData.CurrentArena.GetPowerOrb();
            int orbPower = m_ArenaData.CalculateOrbPowerReward(m_CurrentLevel, m_CurrentStage);
            m_OrbPowerRewardDisplay.SetActive(true);
            m_OrbPowerRewardQty.text = string.Format(GOLD_FORMAT, orbPower);

            // init image
            m_PowerOrbContainer.Initialize(currentPowerOrb.Clone(), activateIdle: false);

            // check if a bonus star has been provided
            if (m_IsBossFight && currentPowerOrb.TryUpgradeRarety())
                StartCoroutine(DisplayOrbUpgrade());

            // update to cloud
            ProgressionCloudData.AddCurrentArenaPowerOrbReward(orbPower, currentPowerOrb.Rarety);
        }

        // ----------------------------------------------------------------------------
        // Refresh Token
        if (LobbyHandler.Instance.GameMode == EGameMode.Arena && m_GameResult == EGameResult.Win)
        {
            if (ProgressionCloudData.CurrentArena.HasMod(EArenaMod.Random))
            {
                // display refresh
                m_RefreshTokenDisplay.SetActive(true);

                // update to cloud
                ProgressionCloudData.AddCurrentArenaRefreshToken(1);
            }
        }

        StartCoroutine(RewardsAnimation());

        ErrorHandler.Log("HandleReward() : end", ELogTag.Rewards);
    }

    IEnumerator DisplayOrbUpgrade()
    {
        yield return new WaitForSeconds(1.5f);

        m_PowerOrbContainer.PowerOrbData.UpgradeRarety();
        m_PowerOrbContainer.RefreshUI();
        m_PowerOrbContainer.PowerOrbUI.PlayUpgradeAnimation();
    }

    float CalculateCurrencyMultiplicator()
    {
        switch (LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                ArenaData arenaData = AssetLoader.LoadArenaData(ProgressionCloudData.CurrentArena.ArenaType, ProgressionCloudData.CurrentArena.SArenaDifficulty);
                return arenaData.CurrentRewardMultiplicator;

            default:
                return 1f;
        }
    }

    void HandleProgression(bool preventiveLossApplied)
    {
        // if a preventive loss has already been applied and this is a loss - exit
        if (m_GameResult == EGameResult.Loss && preventiveLossApplied)
            return;
       
        switch (LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                ErrorHandler.Log("HandleProgression() : Loading Arena Data : " + PlayerPrefsHandler.GetArenaType().ToString(), ELogTag.GameSystem);

                // if preventive loss has been applied, remove life loss
                switch(m_GameResult)
                {
                    case EGameResult.Win:
                        if (preventiveLossApplied)
                            ProgressionCloudData.AddArenaLoss(-1, false);
                        ProgressionCloudData.AddArenaWin();
                        break;

                    case EGameResult.Loss:
                        if (!preventiveLossApplied && !Main.StopPreventiveLoss)
                            ProgressionCloudData.AddArenaLoss();
                        break;

                    case EGameResult.Draw:
                        ErrorHandler.Error("Draw in Arena Mode should not happen"); 
                        break;

                    default:
                        ErrorHandler.Warning("Unhandled case : " + m_GameResult);
                        break;
                }
                break;

            case EGameMode.Ranked:
                ErrorHandler.Log("HandleProgression() : Ranked game", ELogTag.GameSystem);

                // if preventive loss has been applied, remove life loss
                switch (m_GameResult)
                {
                    case EGameResult.Win:
                        // if preventive loss has been applied, apply double win
                        ProgressionCloudData.UpdateLeagueValue(true, nTimes: preventiveLossApplied ? 2 : 1);
                        break;

                    case EGameResult.Loss:
                        // if preventive loss was NOT applied : apply loss
                        if (!preventiveLossApplied && !Main.StopPreventiveLoss)
                            ProgressionCloudData.UpdateLeagueValue(false, nTimes: 1);
                        break;

                    case EGameResult.Draw:
                        // if preventive loss was applied : refund it
                        if (preventiveLossApplied)
                            ProgressionCloudData.UpdateLeagueValue(true, nTimes: 1);
                        break;

                    default:
                        ErrorHandler.Warning("Unhandled case : " + m_GameResult);
                        break;
                }
                break;

            // no progression on training game
            case EGameMode.Training:
                if (LobbyHandler.Instance.IsTuto)
                {
                    ProfileCloudData.Instance.SetData(ProfileCloudData.KEY_TUTO_DONE, true, true);
                    LobbyHandler.Instance.IsTuto = false;
                }
                break;

            default:
                ErrorHandler.Error("Unhandled case : " + LobbyHandler.Instance.GameMode);
                break;
        }
    }

    /// <summary>
    /// Handle data display/save at the end of the game
    /// </summary>
    /// <param name="win"></param>
    void HandleEndGameData()
    {
        // send analytics event (that also saves in StatCloudData)
        switch(LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                MAnalytics.SendEvent(new ArenaGameEndedEvent(
                    m_GameResult == EGameResult.Win,
                    character:          StaticPlayerData.Character,
                    playerLevel:        StaticPlayerData.CharacterLevel,
                    runes:              StaticPlayerData.Runes,
                    spells:             StaticPlayerData.Spells.ToList(),
                    spellLevels:        StaticPlayerData.SpellLevels.ToList(),
                    arenaType:          PlayerPrefsHandler.GetArenaType(),
                    arenaDifficulty:    ProgressionCloudData.CurrentArena.SArenaDifficulty.ToString(),
                    level:              ProgressionCloudData.CurrentArena.Level,
                    stage:              GameUIManager.Instance.PreviousStage
                ));
                break;

            case EGameMode.Ranked:
                MAnalytics.SendEvent(new RankedGameEndedEvent(
                    m_GameResult == EGameResult.Win, 
                    character:      StaticPlayerData.Character, 
                    characterLevel: StaticPlayerData.CharacterLevel,
                    runes:          StaticPlayerData.Runes,
                    spells:         StaticPlayerData.Spells.ToList(),
                    spellLevels:    StaticPlayerData.SpellLevels.ToList(),
                    gameId:         PlayerPrefs.GetString(EPlayerPref.CurrentGameId.ToString()),
                    league:         ProgressionCloudData.CurrentLeague,
                    level:          ProgressionCloudData.CurrentLeagueLevel,
                    stage:          GameUIManager.Instance.PreviousStage
                ));
                break;

            case EGameMode.Training:
                break;

            default:
                ErrorHandler.Warning("Unhandled case : " + LobbyHandler.Instance.GameMode);
                return;
        }
    }

    #endregion


    #region Animation

    IEnumerator IntroAnimation()
    {
        // Deactivate all components visual animated components
        m_TitleText.gameObject.SetActive(false);
        m_RewardsContent.SetActive(false);
        m_LeaveButton.gameObject.SetActive(false);
        m_Fireworks.SetActive(false);

        // FADE IN : Background
        var fadeIn = m_Background.AddComponent<Fade>();
        fadeIn.Initialize(duration: 0.4f, startOpacity:0.5f);
        yield return new WaitUntil(() => fadeIn.IsOver);

        // Move : Title
        m_TitleText.gameObject.SetActive(true);
        var moveTitle = m_TitleText.gameObject.AddComponent<MoveAnimation>();
        var pos = m_TitleText.gameObject.transform.position;
        pos.y += 250;
        moveTitle.Initialize(duration: 0.5f, startPos: pos);

        // FIREWORKS particles (on win only)
        if (m_GameResult == EGameResult.Win)
            m_Fireworks.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        NextState();
    }

    IEnumerator RewardsAnimation()
    {
        var fadeIn = m_Background.AddComponent<Fade>();

        // BOUNCE : Rewards
        m_RewardsContent.SetActive(true);
        fadeIn = m_RewardsContent.AddComponent<Fade>();
        fadeIn.Initialize(duration: 0.5f, startScale: 0.5f);

        // FadeIn : Button
        m_LeaveButton.gameObject.SetActive(true);
        var fadeInButton = m_LeaveButton.gameObject.AddComponent<Fade>();
        fadeInButton.Initialize(duration: 0.5f, startOpacity: 0f);

        // FadeIn : Button
        m_DetailsButton.gameObject.SetActive(true);
        fadeInButton = m_DetailsButton.gameObject.AddComponent<Fade>();
        fadeInButton.Initialize(duration: 0.5f, startOpacity: 0f);

        yield return new WaitUntil(() => fadeIn.IsOver);
    }

    #endregion


    #region GUI Manipulators

    void SetUpTitle()
    {
        switch (m_GameResult)
        {
            case EGameResult.Win:
                m_TitleText.text = "Victory";
                m_TitleText.color = Color.green;
                break;

            case EGameResult.Loss:
                m_TitleText.text = "Defeat";
                m_TitleText.color = Color.red;
                break;

            case EGameResult.Draw:
                m_TitleText.text = "Draw";
                m_TitleText.color = Color.grey;
                break;

            default:
                ErrorHandler.Warning("Unhandled case : " + m_GameResult); 
                break;
        }
    }

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();

        m_DetailsButton.onClick.AddListener(m_EndGameAnalyticsUI.ToggleDisplay);
        m_LeaveButton.onClick.AddListener(NextState);
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();

        m_DetailsButton.onClick?.RemoveListener(m_EndGameAnalyticsUI.ToggleDisplay);
        m_LeaveButton.onClick?.RemoveListener(NextState);
    }

    #endregion
}
