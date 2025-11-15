using Assets.Scripts.Managers;
using Data.GameManagement;
using Menu.Common.Displayers;
using Save;
using TMPro;
using Tools;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public enum EDailyButtonState
    {
        Locked,
        Ready,
        Collected
    }

    public class DailyRewardItem : MObject
    {
        #region Members

        int m_Index;
        EDailyButtonState m_ButtonState;
        SRewardsData m_RewardsData;

        Button              m_Button;
        RewardsDisplayer    m_RewardsDisplayer;
        TMP_Text            m_Title;
        Image               m_Overlay;
        Image               m_Lock;
        Image               m_Validate;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button            = Finder.FindComponent<Button>(gameObject);
            m_RewardsDisplayer  = Finder.FindComponent<RewardsDisplayer>(gameObject, "RewardsDisplayer");
            m_Title             = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_Overlay           = Finder.FindComponent<Image>(gameObject, "Overlay");
            m_Lock              = Finder.FindComponent<Image>(gameObject, "Lock");
            m_Validate          = Finder.FindComponent<Image>(gameObject, "Validate");
        }

        public void Initialize(int index)
        {
            m_Index = index;
            m_RewardsData = TimeCloudData.GetRewardAtIndex(m_Index).Value;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_RewardsDisplayer.Initialize(m_RewardsData);
            m_Title.text = "Day " + (m_Index + 1).ToString();

            if (TimeCloudData.IsCollected(m_Index))
                SetState(EDailyButtonState.Collected);
            else if (TimeCloudData.DailyRewards.CurrentIndex == m_Index && TimeCloudData.DailyRewards.CanCollect())
                SetState(EDailyButtonState.Ready);
            else
                SetState(EDailyButtonState.Locked);
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            switch (m_ButtonState)
            {
                case EDailyButtonState.Locked:
                    m_Lock.gameObject.SetActive(true);
                    m_Validate.gameObject.SetActive(false);
                    m_Overlay.gameObject.SetActive(false);
                    break;

                case EDailyButtonState.Ready:
                    m_Lock.gameObject.SetActive(false);
                    m_Validate.gameObject.SetActive(false);
                    m_Overlay.gameObject.SetActive(false);
                    break;

                case EDailyButtonState.Collected:
                    m_Lock.gameObject.SetActive(false);
                    m_Validate.gameObject.SetActive(true);
                    m_Overlay.gameObject.SetActive(true);
                    break;
            }
        }

        void SetState(EDailyButtonState state)
        {
            m_ButtonState = state;
            RefreshUI();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveListener(OnButtonClicked);
        }

        void OnButtonClicked()
        {
            switch (m_ButtonState)
            {
                case EDailyButtonState.Locked:
                    ScreenManager.QuickRewardMessage("This reward can not be collected yet, please come back another day.", m_RewardsData);
                    return;

                case EDailyButtonState.Ready:
                    var reward = TimeCloudData.CollectCurrentReward();
                    SetState(EDailyButtonState.Collected);
                    if (reward.HasValue)
                        ScreenManager.DisplayRewards(reward.Value, "DailyReward");

                    return;

                case EDailyButtonState.Collected:
                    ScreenManager.QuickMessage("This reward has already been collected");
                    return;
            }
        }

        #endregion
    }
}
    
