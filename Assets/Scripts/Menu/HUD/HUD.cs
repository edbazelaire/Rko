using Assets.Scripts.Managers;
using Enums;
using Menu.Common.Notifications;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Menu
{
    public class HUD : MObject
    {
        #region Members

        GameObject m_ButtonsContainer;
        Button m_LoginButton;
        Button m_GiftButton;
        Button m_MessagerieButton;
        Button m_SettingsButton;
        Button m_DailyRewardsButton;

        #endregion


        #region Init & End

        private void Awake()
        {
            Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ButtonsContainer      = Finder.Find(gameObject, "ButtonsContainer");
            m_LoginButton           = Finder.FindComponent<Button>(m_ButtonsContainer, "LoginButton");
            m_GiftButton            = Finder.FindComponent<Button>(m_ButtonsContainer, "GiftButton");
            m_MessagerieButton      = Finder.FindComponent<Button>(m_ButtonsContainer, "MessagerieButton");
            m_SettingsButton        = Finder.FindComponent<Button>(m_ButtonsContainer, "SettingsButton");
            m_DailyRewardsButton    = Finder.FindComponent<Button>(m_ButtonsContainer, "DailyRewardsButton");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_LoginButton.gameObject.SetActive(! AuthManager.Instance.IsLoggedIn);
            CoroutineManager.DelayMethod(() => SetMessagesNotification());
            CoroutineManager.DelayMethod(() => SetDailyRewardNotification());
        }

        #endregion


        #region GUI Manipulator

        protected void SetMessagesNotification()
        {
            int nMessages = NotificationCloudData.NotSeenMessagesCount();
            if (nMessages > 0)
            {
                NotificationPulse.Add(
                    baseGameObject: m_MessagerieButton.gameObject,
                    animationTarget: Finder.FindComponent<Image>(m_MessagerieButton.gameObject).gameObject,
                    redDotTarget: m_MessagerieButton.gameObject,
                    counter: nMessages,
                    size: 1f
                );
            } 
        }

        protected void SetDailyRewardNotification()
        {
            if (! TimeCloudData.DailyRewards.CanCollect())
                return;

            NotificationPulse.Add(
                baseGameObject:     m_DailyRewardsButton.gameObject,
                animationTarget:    m_DailyRewardsButton.gameObject,
                redDotTarget:       m_DailyRewardsButton.gameObject,
                counter: 1,
                size: 1f
            );
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_LoginButton.onClick.AddListener(OnLoginButtonClicked);
            m_MessagerieButton.onClick.AddListener(() => Main.SetPopUp(EPopUpState.MessageriePopUp));
            m_GiftButton.onClick.AddListener(() => Main.SetPopUp(EPopUpState.PromoCodePopUp));
            m_SettingsButton.onClick.AddListener(() => Main.SetPopUp(EPopUpState.SettingsPopUp));
            m_DailyRewardsButton.onClick.AddListener(() => ScreenManager.SetPopUp(EPopUpState.DailyRewardsPopUp));

            AuthManager.LoginEvent += OnLogin;
            NotificationCloudData.MessageSeenEvent += OnMessageSeen;
            NotificationCloudData.MessageCountChangedEvent += OnMessageCountChanged;
            TimeCloudData.DailyRewardCollected += OnDailyRewardCollected;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_LoginButton.onClick.RemoveAllListeners();
            m_MessagerieButton.onClick.RemoveAllListeners();
            m_GiftButton.onClick.RemoveAllListeners();
            m_SettingsButton.onClick.RemoveAllListeners();
            m_DailyRewardsButton.onClick.RemoveAllListeners();

            AuthManager.LoginEvent -= OnLogin;
            NotificationCloudData.MessageSeenEvent -= OnMessageSeen;
            NotificationCloudData.MessageCountChangedEvent -= OnMessageCountChanged;
            TimeCloudData.DailyRewardCollected -= OnDailyRewardCollected;
        }

        protected void OnMessageSeen(string _)
        {
            OnMessageCountChanged();
        }

        protected void OnMessageCountChanged()
        {
            int nMessages = NotificationCloudData.NotSeenMessagesCount();
            if (nMessages <= 0)
            {
                NotificationPulse.Remove(m_MessagerieButton.gameObject);
            }
            else
            {
                NotificationPulse.UpdateCounter(m_MessagerieButton.gameObject, nMessages);
            }
        }

        void OnLoginButtonClicked()
        {
            ScreenManager.SetPopUp(EPopUpState.LoginPopUp);
        }

        void OnLogin(bool login)
        {
            m_LoginButton.gameObject.SetActive(!login);
        }

        void OnDailyRewardCollected()
        {
            NotificationPulse.Remove(m_DailyRewardsButton.gameObject);
        }

        #endregion
    }
}