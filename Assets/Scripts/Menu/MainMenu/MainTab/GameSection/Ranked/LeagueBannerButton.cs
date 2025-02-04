using Assets;
using Enums;
using Menu.Common.Notifications;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class LeagueBannerButton : MObject
{
    #region Members

    ELeague                 m_League;

    Image                   m_Icon;
    NotificationParticles     m_NotificationDisplay;
    Button                  m_Button;

    public Button Button => m_Button;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Icon                  = Finder.FindComponent<Image>(gameObject, "Icon");
        m_NotificationDisplay   = Finder.FindComponent<NotificationParticles>(gameObject);
        m_Button                = Finder.FindComponent<Button>(gameObject);
    }

    public void Initialize(ELeague league)
    {
        m_League = league;

        base.Initialize();

        m_NotificationDisplay.Initialize(Finder.FindComponent<Image>(gameObject, "Background"), Vector2.one * 2);

        CheckNotificationActivation();
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        m_Icon.sprite = AssetLoader.LoadLeagueBanner(m_League);
    }

    #endregion


    #region GUI Manipulators

    void CheckNotificationActivation()
    {
        if (NotificationCloudData.HasRewardsForLeague(m_League))
            m_NotificationDisplay.Activate();
        else
            m_NotificationDisplay.Deactivate();
    }

    #endregion

    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();

        m_Button.onClick.AddListener(OnButtonClicked);
        NotificationCloudData.LeagueRewardChangedEvent = OnLeagueRewardChanged;
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();

        m_Button.onClick.RemoveAllListeners();
        NotificationCloudData.LeagueRewardChangedEvent -= OnLeagueRewardChanged;
    }

    void OnLeagueRewardChanged()
    {
        CheckNotificationActivation();
    }

    void OnButtonClicked()
    {
        Main.SetPopUp(EPopUpState.LeaguesPathScreen);
    }

    #endregion
}
