using Assets;
using Assets.Scripts.Managers.Sound;
using Data.GameManagement;
using Enums;
using Managers.Monetization.IAP;
using Menu.Common.Displayers;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class TemplateShopItemUI : MObject
    {
        #region Members

        [SerializeField] int NRewardsPerRow = 4;

        // GameObjects & Components
        protected TMP_Text          m_TitleText;
        protected Image             m_Icon;
        protected Image             m_CurrencyIcon;
        protected RewardsDisplayer  m_RewardsDisplayer;
        protected TMP_Text          m_PriceText;
        protected TMP_Text          m_TimeCounterText;
        protected Button            m_Button;

        // Data
        protected EButtonState m_State;
        protected SShopData m_ShopData;
        protected STimeData? m_TimeData;
        protected string m_Title;

        /// <summary> EVENT DATA : collection context </summary>
        protected string m_Context => ERewardContext.Shop + "." + m_Title;
        protected string m_CostString { 
            get {
                if (m_ShopData.Cost <= 0)
                    return "Free";

                if (m_ShopData.Product != EProduct.None)
                    return IAPUtils.GetProductPriceString(m_ShopData.Product);

                string value = Mathf.Round(m_ShopData.Cost) == m_ShopData.Cost ? m_ShopData.Cost.ToString("0") : m_ShopData.Cost.ToString("F2");
                return value;
            } 
        }
    

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TitleText         = Finder.FindComponent<TMP_Text>(gameObject, "TitleText", false);
            m_Button            = Finder.FindComponent<Button>(gameObject);
            m_Icon              = Finder.FindComponent<Image>(gameObject, "Icon");
            m_TimeCounterText   = Finder.FindComponent<TMP_Text>(gameObject, "TimeCounter", false);
            m_PriceText         = Finder.FindComponent<TMP_Text>(gameObject, "Price", false);
            m_CurrencyIcon      = Finder.FindComponent<Image>(gameObject, "CurrencyIcon");
            m_RewardsDisplayer  = Finder.FindComponent<RewardsDisplayer>(gameObject, "RewardsDisplayer", false);
        }

        public void Initialize(SShopData shopData, STimeData? timeData = null)
        {
            if (!shopData.Check())
            {
                Destroy(gameObject); 
                return;
            }

            SetUpShopData(shopData);

            m_TimeData = timeData;

            // CHECK : provied data
            if (m_ShopData.Cost < 0)
            {
                ErrorHandler.Error("Cost set with negative value " + m_ShopData.Cost + " for item " + m_Title);
                m_ShopData.Cost = 0;
            }

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI(); 

            SetTitle();
            SetIcon();
            SetRewards();
            SetCurrencyIcon();
            SetPrice();

            SetUpTimeDataUI();
        }

        #endregion


        #region Update

        private void Update()
        {
            switch ( m_State)
            {
                case EButtonState.Locked:
                    UpdateLockedState();
                    break;
            }
        }

        void UpdateLockedState()
        {
            if (m_TimeData == null)
                return;

            if (m_TimeData.Value.IsCollectable())
            { 
                SetState(EButtonState.Normal);
                return;
            }
            
            if (m_TimeCounterText != null)
            {
                if (!m_TimeData.Value.IsExpired())
                    m_TimeCounterText.text = "Reseting in " + TextHandler.FormatTimestamp(m_TimeData.Value.ResetIn());
                else
                    m_TimeCounterText.gameObject.SetActive(false);
            }
        }

        #endregion


        #region GUI Manipulators

        protected virtual void SetUpShopData(SShopData shopData)
        {
            m_ShopData = shopData;
            m_Title    = shopData.Name;
            SetDefaultData();
        }

        protected virtual void SetDefaultData() { }

        protected virtual void SetTitle() 
        {
            if (m_TitleText == null)
                return;

            m_TitleText.text = TextHandler.Split(m_Title);
        }

        protected virtual void SetIcon()
        {
            if (m_ShopData.Icon == null)
               return;

            m_Icon.sprite = m_ShopData.Icon;
        }

        protected virtual void SetRewards()
        {
            if (m_RewardsDisplayer == null)
                return;

            m_RewardsDisplayer.Initialize(m_ShopData.Rewards, NRewardsPerRow);
        }

        protected virtual void SetCurrencyIcon()
        {
            if (m_ShopData.Cost <= 0 || m_ShopData.Currency == ECurrency.Real)
            {
                m_CurrencyIcon.gameObject.SetActive(false);
                return;
            }
            
            m_CurrencyIcon.sprite = AssetLoader.LoadCurrencyIcon(m_ShopData.Currency);
        }

        protected virtual void SetPrice() 
        {
            ErrorHandler.Warning("NotImplemented");
            return;
        }

        protected virtual void SetUpTimeDataUI()
        {
            if (m_TimeData != null && ! m_TimeData.Value.IsCollectable())
            {
                SetState(EButtonState.Locked);
                return;
            }

            SetState(EButtonState.Normal);
        }

        #endregion


        #region State Management

        protected virtual void SetState(EButtonState state)
        {
            switch (state)
            {
                case EButtonState.Normal:
                    NormalStateUI();
                    break;

                case EButtonState.Locked:
                    LockedStateUI();
                    break;

                default:
                    ErrorHandler.Warning("Unhandled case : " + state);
                    break;
            }

            m_State = state;
        }

        protected virtual void NormalStateUI() {}

        protected virtual void LockedStateUI() {}

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClick);
            if (m_TimeData != null)
                TimeCloudData.TimeDataChangedEvent += OnTimeDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveListener(OnClick);
            if (m_TimeData != null)
                TimeCloudData.TimeDataChangedEvent -= OnTimeDataChanged;
        }

        protected virtual void OnClick()
        {
            if (m_TimeData != null && m_TimeData.Value.NCollectionLeft <= 0)
            {
                // TODO !
                ErrorHandler.Warning("Cant collect - Already used");
                return;
            }

            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

            if (m_ShopData.Cost == 0)
            {
                OnPurchaseCompleted(true);
                return;
            }
            
            Main.ConfirmBuyRewards(m_ShopData.PrettyName, m_ShopData.ProductId, new SPriceData(m_ShopData.Cost, m_ShopData.Currency), m_ShopData.Rewards, OnPurchaseCompleted);
        }

        protected void OnPurchaseCompleted(bool success)
        {
            switch (success)
            {
                case true:
                    if (m_TimeData != null && ! TimeCloudData.CollectTimeData(m_TimeData.Value.Name))
                        return;
                    
                    Main.DisplayRewards(m_ShopData.Rewards, m_Context);
                    break;
                
                case false:
                    break;
            }   
        }

        protected void OnTimeDataChanged(string name)
        {
            if (m_TimeData == null || m_TimeData.Value.Name != name) 
                return;

            m_TimeData = TimeCloudData.GetTimeData(name);
            SetUpTimeDataUI();
        }

        #endregion
    }
}