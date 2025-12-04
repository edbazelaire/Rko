using Data.GameManagement;
using Enums;
using Inventory;
using Managers.Monetization.IAP;
using Menu.Common.Displayers;
using System;
using System.Linq;
using TMPro;
using Tools;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class ConfirmBuyPopUp : MessagePopUp
    {
        #region Members

        // Data
        protected string            m_ItemName;
        protected string            m_ProductId;
        protected SPriceData        m_PriceData;
        protected SRewardsData      m_RewardsData;
        protected bool              m_EnableWatchAd;

        // GameObjects & Components
        protected TMP_Text          m_ButtonsErrorMessage;
        protected Button            m_WatchAdButton;
        protected Button            m_BuyButton;
        protected PriceDisplay      m_BuyButtonDisplay;
        protected LootInfoSidebar   m_LootInfoSidebar;

        // Dependent Properties
        protected virtual string m_Context => "Shop." + m_ItemName;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ButtonsErrorMessage   = Finder.FindComponent<TMP_Text>(gameObject, "ButtonsErrorMessage");
            m_WatchAdButton         = Finder.FindComponent<Button>(m_Buttons, "WatchAdButton");
            m_BuyButton             = Finder.FindComponent<Button>(m_Buttons, "BuyButton");
            m_BuyButtonDisplay      = Finder.FindComponent<PriceDisplay>(m_BuyButton.gameObject);

            m_LootInfoSidebar = Finder.FindComponent<LootInfoSidebar>(gameObject, "LootInfoSidebar", false);
        }

        public void Initialize(string itemName, string productId, SPriceData priceData, bool watchAd, SRewardsData rewardsData, Action onValidate, Action onCancel)
        {
            m_ItemName      = itemName;
            m_ProductId     = productId;
            m_PriceData     = priceData;
            m_RewardsData   = rewardsData;
            //m_EnableWatchAd = watchAd;
            m_EnableWatchAd = false;

            base.Initialize(GetMessage(), "Confirm Buy", onValidate, onCancel);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_ButtonsErrorMessage.text = "";
            m_BuyButtonDisplay.Initialize(m_PriceData);

            m_WatchAdButton.gameObject.SetActive(m_EnableWatchAd);

            // ================================================================================
            // TODO : SET BACK AFTER TESTS 
            //if (m_PriceData.Price == 0)
            //    m_BuyButton.gameObject.SetActive(false);
            // ================================================================================

            SetupLootInfoSidebar();
        }

        #endregion


        #region GUI Manipulators

        protected virtual void SetupLootInfoSidebar()
        {
            if (m_LootInfoSidebar == null)
                return;

            /* ONLY display loot info IF there is only ONE chest. 
             * The display of multiple rewards is "handled" but not very tight (some cases are problematic) */
            if (m_RewardsData.Chests != null && m_RewardsData.Chests.Count == 1)
            {
                m_LootInfoSidebar.gameObject.SetActive(true);
                m_LootInfoSidebar.Initialize(m_ItemName, m_RewardsData.Chests, m_RewardsData.GetCurrency(ECurrency.Gold));
            }
            else
            {
                m_LootInfoSidebar.gameObject.SetActive(false);
            }
        }

        #endregion


        #region Helpers

        protected virtual string GetMessage()
        {
            return "";
        }

        #endregion


        #region Listeners

        protected override void OnUIButton(string bname)
        {
            switch (bname)
            {
                case ("BuyButton"):
                    OnBuyClicked();
                    break;

                case ("WatchAdButton"):
                    OnWatchAdClicked();
                    break;

                default:
                    base.OnUIButton(bname);
                    break;
            }
        }

        protected virtual void OnBuyClicked()
        {
            if (m_PriceData.Currency == Enums.ECurrency.Real)
            {
                IAPManager.Instance.BuyProduct(m_ProductId, m_OnValidate);
                Exit();
                return;
            }

            if (! InventoryManager.CanBuy(m_PriceData.Price, m_PriceData.Currency))
            {
                m_ButtonsErrorMessage.text = $"You do not have enough {m_PriceData.Currency} to buy this item";
                AnimationHandler.ClickErrorAnimation(m_BuyButton.gameObject);
                return;
            }

            InventoryManager.Spend(m_PriceData.Price, m_PriceData.Currency, m_Context);

            OnValidateButton();
        }

        protected virtual void OnWatchAdClicked()
        {
            //AdManager.Instance.ShowRewarded(m_OnValidate);
            Exit();
        }

        #endregion
    }
}