using Analytics.Events;
using Data.GameManagement;
using Enums;
using Game;
using Inventory;
using Managers;
using Network;
using Save;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Tools;
using Tools.Animations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class EndGameUI : MObject
{
    #region Members

    const string GOLDS_FORMAT = "+ {0}";
    const string c_TitleText = "TitleText";
    const string c_LeaveButton = "LeaveButton";

    // Data
    bool m_Win;

    // Components
    GameObject  m_Background;
    TMP_Text    m_TitleText;
    GameObject  m_RewardsContent;
    GameObject  m_XpRewardDisplay;
    TMP_Text    m_XpQty;
    GameObject  m_GoldsRewardDisplay;
    TMP_Text    m_GoldsQty;
    GameObject  m_GemsRewardDisplay;
    TMP_Text    m_GemsQty;
    Image       m_ChestRewardIcon;
    Button      m_LeaveButton;
    GameObject  m_Fireworks;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        m_Background            = Finder.Find(gameObject, "Background");
        m_Fireworks             = Finder.Find(gameObject, "Fireworks");
        m_TitleText             = Finder.FindComponent<TMP_Text>(gameObject, c_TitleText);
        m_LeaveButton           = Finder.FindComponent<Button>(gameObject, c_LeaveButton);
        m_RewardsContent        = Finder.Find(gameObject, "RewardsContent");
        m_XpRewardDisplay       = Finder.Find(m_RewardsContent, "XpRewardDisplay");
        m_XpQty                 = Finder.FindComponent<TMP_Text>(m_XpRewardDisplay, "Qty");
        m_GoldsRewardDisplay    = Finder.Find(m_RewardsContent, "GoldsRewardDisplay");
        m_GoldsQty              = Finder.FindComponent<TMP_Text>(m_GoldsRewardDisplay, "Qty");
        m_GemsRewardDisplay     = Finder.Find(m_RewardsContent, "GemsRewardDisplay");
        m_GemsQty               = Finder.FindComponent<TMP_Text>(m_GemsRewardDisplay, "Qty");
        m_ChestRewardIcon       = Finder.FindComponent<Image>(m_RewardsContent, "ChestRewardIcon");
    }

    public override void Initialize()
    {
        base.Initialize();

        gameObject.SetActive(false);
    }

    // Use this for initialization
    public void Activate(bool win, bool preventiveLossApplied)
    {
        ErrorHandler.Log("END OF GAME : " + LobbyHandler.Instance.GameMode + " - " + (win ? "WIN" : "LOSS"), ELogTag.GameSystem);

        // make sure that into is deactivated
        GameUIManager.IntroGameUI.Deactivate();

        // save if this is win or not
        m_Win = win;

        // set color and text according to context
        m_TitleText.text = m_Win ? "Victory" : "Defeat";
        m_TitleText.color = m_Win ? Color.green : Color.red;

        // handle data processing before animation & stuff
        HandleReward(win);
        HandleProgression(win, preventiveLossApplied);
        HandleEndGameData(win);

        // activate game object
        gameObject.SetActive(true);

        // start animation coroutine
        StartCoroutine(ActivationAnimation());
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


    #region Reward & EndGame

    void HandleReward(bool win)
    {
        ErrorHandler.Log("HandleReward() : start", ELogTag.Rewards);

        SRewardCalculator reward = win ? Rewarder.WinGameReward : Rewarder.LossGameReward;
        reward.SetCurrencyMultiplicator(CalculateCurrencyMultiplicator());

        // no rewards for training mode
        if (LobbyHandler.Instance.GameMode == EGameMode.Training)
            reward = new SRewardCalculator(0, 0, 0, 0, new List<SChestDropPercentage>());

        // ----------------------------------------------------------------------------
        // Xp   
        int xp = reward.GetXp();
        ErrorHandler.Log("         + XP : " + xp, ELogTag.Rewards);
        if (xp <= 0)
            m_XpRewardDisplay.SetActive(false);
        else
        {
            m_XpRewardDisplay.SetActive(true);
            m_XpQty.text = string.Format(GOLDS_FORMAT, xp);
            InventoryManager.AddCollectable(CharacterBuildsCloudData.SelectedCharacter, xp);
        }

        // ----------------------------------------------------------------------------
        // GOLDS   
        int golds = reward.GetGolds();
        ErrorHandler.Log("         + GOLDS : " + golds, ELogTag.Rewards);

        if (golds <= 0)
            m_GoldsRewardDisplay.SetActive(false);
        else
        {
            m_GoldsRewardDisplay.SetActive(true);
            m_GoldsQty.text = string.Format(GOLDS_FORMAT, golds);
            InventoryManager.UpdateCurrency(ECurrency.Golds, golds, ERewardContext.EndGameChest.ToString()) ;
        }

        // ----------------------------------------------------------------------------
        // Gems   
        int gems = reward.GetGems();
        ErrorHandler.Log("         + XP : " + xp, ELogTag.Rewards);
        if (gems <= 0)
            m_GemsRewardDisplay.SetActive(false);
        else
        {
            m_GemsRewardDisplay.SetActive(true);
            m_GemsQty.text = string.Format(GOLDS_FORMAT, gems);
            InventoryManager.UpdateCurrency(ECurrency.Gems, gems, ERewardContext.EndGameChest.ToString());
        }

        // ----------------------------------------------------------------------------
        // CHESTS
        // init chests rewards to empty list
        List<EChest> chests = new();
        ErrorHandler.Log("         + CHESTS : " + chests.Count, ELogTag.Rewards);

        // check if any index is available to store the chest (otherwise : no chest reward)
        if (InventoryManager.GetFirstAvailableIndex(out int index))
            chests = reward.GetChests();

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

        ErrorHandler.Log("HandleReward() : end", ELogTag.Rewards);
    }

    float CalculateCurrencyMultiplicator()
    {
        switch (LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                ArenaData arenaData = AssetLoader.LoadArenaData(PlayerPrefsHandler.GetArenaType());
                return arenaData.CurrentRewardMultiplicator;

            default:
                return 1f;
        }
    }

    void HandleProgression(bool win, bool preventiveLossApplied)
    {
        // if a preventive loss has already been applied and this is a loss - exit
        if (!win && preventiveLossApplied)
            return;
       
        switch (LobbyHandler.Instance.GameMode)
        {
            case EGameMode.Arena:
                ErrorHandler.Log("HandleProgression() : Loading Arena Data : " + PlayerPrefsHandler.GetArenaType().ToString(), ELogTag.GameSystem);

                // if preventive loss has been applied, apply double win
                ProgressionCloudData.UpdateStageValue(PlayerPrefsHandler.GetArenaType(), win, nTimes: win & preventiveLossApplied ? 2 : 1);
                break;

            case EGameMode.Ranked:
                ErrorHandler.Log("HandleProgression() : Ranked game", ELogTag.GameSystem);

                // if preventive loss has been applied, apply double win
                ProgressionCloudData.UpdateLeagueValue(win, nTimes: win & preventiveLossApplied ? 2 : 1);
                break;

            // no progression on training game
            case EGameMode.Training:
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
    void HandleEndGameData(bool win)
    {
        // send analytics event (that also saves in StatCloudData)
        MAnalytics.SendEvent(new GameEndedEvent(LobbyHandler.Instance.GameMode, win, StaticPlayerData.Character, StaticPlayerData.CharacterLevel)); ;
    }

    #endregion


    #region Animation

    IEnumerator ActivationAnimation()
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

        // BOUNCE : Rewards
        m_RewardsContent.SetActive(true);
        fadeIn = m_RewardsContent.AddComponent<Fade>();
        fadeIn.Initialize(duration: 0.5f, startScale: 0.5f);

        // Move : Title
        m_TitleText.gameObject.SetActive(true);
        var moveTitle = m_TitleText.gameObject.AddComponent<MoveAnimation>();
        var pos = m_TitleText.gameObject.transform.position;
        pos.y += 250;
        moveTitle.Initialize(duration: 0.5f, startPos: pos);

        // FadeIn : Button
        m_LeaveButton.gameObject.SetActive(true);
        var fadeInButton = m_LeaveButton.gameObject.AddComponent<Fade>();
        fadeInButton.Initialize(duration: 0.5f, startOpacity: 0f);

        // FIREWORKS particles (on win only)
        if (m_Win)
            m_Fireworks.SetActive(true);

        yield return new WaitUntil(() => fadeIn.IsOver);
    }

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();

        m_LeaveButton.onClick.AddListener(Leave);
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();

        m_LeaveButton.onClick?.RemoveListener(Leave);
    }

    #endregion
}
