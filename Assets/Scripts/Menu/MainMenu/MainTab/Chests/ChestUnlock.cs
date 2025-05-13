using Assets;
using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Inventory;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{

    public class ChestUnlock : MObject
    {
        #region Members

        const string c_EmptySlot                = "EmptySlot";
        const string c_ChestPreviewContainer    = "ChestPreviewContainer";
        const string c_ChestContainer           = "ChestContainer";
        const string c_ChestPreview             = "ChestPreview";
        const string c_ChestTimer               = "ChestTimer";

        [SerializeField] Color m_EmptyColor;
        [SerializeField] Color m_LockedColor;
        [SerializeField] Color m_UnlockingColor;
        [SerializeField] Color m_BoostedColor;
        [SerializeField] Color m_ReadyColor;

        Button      m_Button;
        GameObject  m_EmptySlot;
        Image       m_Background;
        GameObject  m_ChestPreviewContainer;
        GameObject  m_ChestContainer;
        ChestUI     m_ChestUI;
        TMP_Text    m_ChestTimer;

        EChestLockState m_PreviousState;

        int m_Index                 = 0;

        ChestData m_ChestData   => (ChestsCloudData.Instance.Data[ChestsCloudData.KEY_CHESTS] as ChestData[])[m_Index];
        EChestLockState m_State => m_ChestData == null ? EChestLockState.Empty : m_ChestData.GetState();
        int UnlockedIn => (int)(m_ChestData.GetUnlockedTime() - DateTimeOffset.UtcNow.ToUnixTimeSeconds());


        #endregion


        #region Init & End

        public void Initialize(int index)
        {
            base.Initialize();

            m_Index = index;
            RefreshState(true);
        }

        protected override void FindComponents()
        {
            base.FindComponents(); 

            m_Button                    = Finder.FindComponent<Button>(gameObject);
            m_EmptySlot                 = Finder.Find(gameObject, c_EmptySlot);
            m_Background                = Finder.FindComponent<Image>(gameObject, "Background");
            m_ChestPreviewContainer     = Finder.Find(gameObject, c_ChestPreviewContainer);
            m_ChestContainer            = Finder.Find(m_ChestPreviewContainer, c_ChestContainer);
            m_ChestTimer                = Finder.FindComponent<TMP_Text>(m_ChestPreviewContainer, c_ChestTimer);
        }

        #endregion


        #region Updates

        private void Update()
        {
            RefreshState();

            if (m_State == EChestLockState.Unlocking)
                RefreshCounter();
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Setup new ChestData and refresh UI to match it
        /// </summary>
        /// <param name="chestData"></param>
        public void RefreshChestUI()
        {
            // create prefab in the preview
            UIHelper.CleanContent(m_ChestContainer);

            if (m_ChestData != null)
                m_ChestUI = ItemLoader.GetChestRewardData(m_ChestData.ChestType).Instantiate(m_ChestContainer);
        }

        /// <summary>
        /// Activate / Deactivate button depending on having chest data
        /// </summary>
        /// <param name="activate"></param>
        void Activate(bool activate)
        {
            m_EmptySlot.SetActive(! activate);
            m_ChestPreviewContainer.SetActive(activate);
            m_Button.interactable = activate;
        }

        /// <summary>
        /// Refresh the display of the counter
        /// </summary>
        void RefreshCounter()
        {
            m_ChestTimer.text = TextLocalizer.GetAsCounter(UnlockedIn);
            return;
        }

        #endregion


        #region State Management

        /// <summary>
        /// Change the State and Adapdt UI depending on it
        /// </summary>
        /// <param name="state"></param>
        void RefreshState(bool force=false)
        {
            if (!force && m_PreviousState == m_State)
                return;

            RefreshChestUI();
            Activate(m_State != EChestLockState.Empty);

            switch (m_State)
            {
                case EChestLockState.Empty:
                    m_Background.color = m_EmptyColor;
                    break;

                case EChestLockState.Locked:
                    m_Background.color = m_LockedColor;
                    m_ChestTimer.text = TextLocalizer.LocalizeText("Locked");
                    break;

                case EChestLockState.Unlocking:
                    m_Background.color = TimeCloudData.HasBoost(EBoost.ChestSpeedBoost) ? m_BoostedColor : m_UnlockingColor;
                    // delay on frame because gameobject might no be init yet
                    CoroutineManager.DelayMethod(() => { m_ChestUI.ActivateIdle(false); });
                    break;

                case EChestLockState.Ready:
                    m_Background.color = m_ReadyColor;
                    m_ChestTimer.text = TextLocalizer.LocalizeText("Ready");
                    // delay on frame because gameobject might no be init yet
                    CoroutineManager.DelayMethod(() => { m_ChestUI.ActivateIdle(false, isLocal: true); });
                    break;
            }
            m_PreviousState = m_State;
        }

        #endregion


        #region Fast Unlock

        void DisplayUnlockPopUp()
        {
            Main.ConfirmBuyRewards("UnlockChest", GetUnlockPrice(), CreateRewardsData(), (bool success) => { if (success) UnlockChest(); });
        }

        /// <summary>
        /// Price (depending on remaning time) to unlock a chest
        /// </summary>
        /// <returns></returns>
        SPriceData GetUnlockPrice()
        {
            var remainingTime = m_State == EChestLockState.Unlocking ? UnlockedIn : ItemLoader.GetChestRewardData(m_ChestData.ChestType).UnlockTime;
            if (TimeCloudData.HasBoost(EBoost.ChestSpeedBoost))
                remainingTime /= 2;
            int price = Math.Max((int)Math.Round(remainingTime * ShopManagementData.FastUnlockChestPrice / 3600), 1);
            return new SPriceData(price, ECurrency.Gems);
        }

        /// <summary>
        /// Format chest into SRewardsData
        /// </summary>
        /// <returns></returns>
        SRewardsData CreateRewardsData()
        {
            var reward = new SRewardsData();
            reward.Add(m_ChestData.ChestType, 1);
            return reward;
        }

        void UnlockChest()
        {
            // display rewards
            Main.DisplayRewards(CreateRewardsData(), ERewardContext.EndGameChest.ToString());

            // remove chest in inventory
            InventoryManager.RemoveChestAtIndex(m_Index);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnButtonClicked);
            TimeCloudData.BoostChangedEvent += OnBoostChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Button != null)
                m_Button.onClick.RemoveAllListeners();

            TimeCloudData.BoostChangedEvent -= OnBoostChanged;
        }

        void OnButtonClicked()
        {
            switch (m_State)
            {
                case EChestLockState.Empty:
                    return;

                case EChestLockState.Locked:
                    // check if a chest is not already unlocking
                    if (ChestsCloudData.IsChestWaitingUnlock())
                    {
                        DisplayUnlockPopUp();
                        return;
                    }

                    m_ChestData.SetUnlockTime();
                    ChestsCloudData.Instance.SaveValue(ChestsCloudData.KEY_CHESTS);
                    return;

                case EChestLockState.Unlocking:
                    DisplayUnlockPopUp();
                    return;

                case EChestLockState.Ready:
                    UnlockChest();
                    return;
            }
        }

        void OnBoostChanged(string boostName, bool activate)
        {
            if (m_State != EChestLockState.Unlocking)
                return;

            if (boostName != EBoost.ChestSpeedBoost.ToString())
                return;

            m_Background.color = activate ? m_BoostedColor : m_UnlockingColor;
        }

        #endregion
    }
}