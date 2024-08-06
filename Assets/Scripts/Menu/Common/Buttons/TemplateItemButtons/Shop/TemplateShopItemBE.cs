using Assets;
using Data.GameManagement;
using Enums;
using Inventory;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class TemplateShopItemBE : TemplateItemButton
    {
        #region Members

        // GameObjects & Components
        Image m_CurrencyIcon;

        // Data
        private string          m_Name;
        private ECurrency       m_Currency;
        private float           m_Cost;
        private SRewardsData    m_Rewards;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CurrencyIcon = Finder.FindComponent<Image>(m_BottomOverlay.gameObject, "CurrencyIcon");
        }

        public void Initialize(string name, Sprite icon, ECurrency currency, float cost, SRewardsData rewards)
        {
            Color? titleColor = null;
            if (name == null || name == "") 
            {
                if (rewards.Currencies.Count == 1)
                {
                    name = TextHandler.FormatNumericalString(rewards.Currencies[0].Qty);
                    titleColor = ShopManagementData.GetCurrencyColor(rewards.Currencies[0].Currency);
                }
            }

            if (name == "")
            {
                ErrorHandler.Warning("No name found for TemplateShopItem " + this.name);
            }

            m_Name = name;
            m_Currency = currency;
            m_Cost = cost;
            m_Rewards = rewards;

            SetTitle(m_Name, textColor: titleColor);
            SetBorderColor(titleColor);
            SetBottomOverlay(Mathf.Round(m_Cost) == m_Cost ? m_Cost.ToString("0") : m_Cost.ToString("F2"), textColor: ShopManagementData.GetCurrencyColor(m_Currency));
            SetIcon(icon);

            m_CurrencyIcon.sprite = AssetLoader.LoadCurrencyIcon(m_Currency);

            m_LockState.SetActive(false);
        }


        #endregion


        #region Listeners

        protected override void OnClick()
        {
            if (m_Currency == ECurrency.Dollars)
            {
                // TODO : for now money transactions are automatic success
                OnPurchaseCompleted(true);
                return;
            }
            
            Main.ConfirmBuyRewards(m_Name, new SPriceData((int)m_Cost, m_Currency), m_Rewards, OnPurchaseCompleted);            
        }

        protected void OnPurchaseCompleted(bool success)
        {
            switch (success)
            {
                case true:
                    Main.DisplayRewards(m_Rewards, ERewardContext.Shop.ToString());
                    break;
                
                case false:
                    break;
            }   
        }

        #endregion
    }
}