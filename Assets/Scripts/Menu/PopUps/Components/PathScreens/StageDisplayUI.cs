using Assets.Scripts.Managers.Sound;
using Data.GameManagement;
using Menu.Common.Displayers;
using Menu.Common.Notifications;
using Menu.MainMenu.MainTab;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public enum EStageRewardState
    {
        Locked,
        Unlocked,
        Collected
    }

    public class StageDisplayUI : MObject
    {
        #region Members

        protected EStageRewardState     m_State;
        protected NotificationParticles   m_NotificationDisplay;

        protected StageSectionUI        m_StageSectionUI;
        protected RewardsDisplayer      m_RewardsDisplayer;
        protected Image                 m_RewardDisplayerBackground;
        protected GameObject            m_OverlayScreen;
        protected Button                m_CollectButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_StageSectionUI                = Finder.FindComponent<StageSectionUI>(gameObject);
            m_RewardsDisplayer              = Finder.FindComponent<RewardsDisplayer>(gameObject);
            m_RewardDisplayerBackground     = Finder.FindComponent<Image>(m_RewardsDisplayer.gameObject);
            m_NotificationDisplay           = Finder.FindComponent<NotificationParticles>(m_RewardsDisplayer.gameObject);
            m_OverlayScreen                 = Finder.Find(m_RewardsDisplayer.gameObject, "OverlayScreen");
            m_CollectButton                 = Finder.FindComponent<Button>(gameObject, "CollectButton");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_NotificationDisplay.Initialize(Finder.FindComponent<Image>(m_RewardsDisplayer.gameObject), Vector2.one * 2);

            RefreshState();
            SetUpRewards();
        }

        #endregion


        #region GUI Manipulators

        void SetUpRewards()
        {
            m_RewardsDisplayer.Initialize(GetRewards(), GetRewards().Count <= 4 ? 2 : 3);
        }

        #endregion


        #region State

        protected virtual void RefreshState() { }

        protected virtual void SetState(EStageRewardState state)
        {
            m_State = state;

            switch (state)
            {
                case EStageRewardState.Locked:
                    SetLockedState();
                    return;

                case EStageRewardState.Unlocked:
                    SetUnlockedState();
                    return;
                    
                case EStageRewardState.Collected:
                    SetCollectedState();
                    return;

                default: 
                    ErrorHandler.Error("Unknown state : " + state);
                    return;
            }
        }

        protected virtual void SetLockedState()
        {
            m_NotificationDisplay.Deactivate();
            m_CollectButton.gameObject.SetActive(false);
            m_OverlayScreen.SetActive(false);
        }

        protected virtual void SetUnlockedState()
        {
            m_NotificationDisplay.Activate();
            m_CollectButton.gameObject.SetActive(true);
            var pulse = m_CollectButton.AddComponent<Pulse>();
            pulse.Initialize("", -1, pauseDuration: 1.5f);

            m_OverlayScreen.SetActive(false);
        }

        protected virtual void SetCollectedState()
        {
            m_NotificationDisplay.Deactivate();
            m_CollectButton.gameObject.SetActive(false);
            m_OverlayScreen.SetActive(true);
        }

        #endregion


        #region Rewards

        protected virtual SRewardsData GetRewards() { return default; }

        protected virtual void CollectReward() { }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
          
            m_CollectButton.onClick.AddListener(OnCollectedButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
         
            m_CollectButton.onClick.RemoveListener(OnCollectedButtonClicked);
        }

        protected virtual void OnCollectedButtonClicked()
        {
            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);
            CollectReward();
            RefreshState();
        }

        #endregion
    }
}