using Assets;
using Enums;
using Menu.Common.Notifications;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class ArenaButton : MObject
{
    #region Members

    EArenaType m_ArenaType;

    NotificationDisplay m_NotificationDisplay;
    Button m_Button;

    public Button Button => m_Button;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_NotificationDisplay = Finder.FindComponent<NotificationDisplay>(gameObject);
        m_Button = Finder.FindComponent<Button>(gameObject);
    }

    public void Initialize(EArenaType arenaType)
    {
        m_ArenaType = arenaType;

        base.Initialize();

        m_NotificationDisplay.Initialize(Finder.FindComponent<Image>(gameObject, "Background"), Vector2.one * 2);
        CheckNotificationActivation();
    }

    void CheckNotificationActivation()
    {
        if (NotificationCloudData.HasRewardsForArenaType(m_ArenaType))
            m_NotificationDisplay.Activate();
        else
            m_NotificationDisplay.Deactivate();
    }

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();

        m_Button.onClick.AddListener(OnArenaButtonClicked);
        NotificationCloudData.ArenaRewardChangedEvent = OnArenaRewardChanged;
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();

        m_Button.onClick.RemoveAllListeners();
        NotificationCloudData.ArenaRewardChangedEvent -= OnArenaRewardChanged;
    }

    void OnArenaRewardChanged()
    {
        CheckNotificationActivation();
    }

    void OnArenaButtonClicked()
    {
        Main.SetPopUp(EPopUpState.ArenaPathScreen, m_ArenaType);
    }

    #endregion
}
