using Data.GameManagement;
using Enums;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.Common.Buttons
{
    public class TemplateCardShopItemUI : TemplateShopItemUI
    {
        #region Members

        // GameObjects & Components
        TemplateItemButton m_TemplateItemButton;

        // Data
        Color? m_TitleColor = null;
        Color? m_BorderColor = null;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TemplateItemButton = Finder.FindComponent<TemplateItemButton>(gameObject);
            m_TemplateItemButton.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetUpBorder();
        }

        #endregion


        #region Data Setter

        /// <summary>
        /// Check missing or empty data  after initialization to replace with default data
        /// </summary>
        protected override void SetDefaultData()
        {
            if (m_ShopData.Rewards.Count > 1)
                ErrorHandler.Warning("Unhandled case : multiple rewards in Cards ShopItemUI");

            // CURRENCIES
            if (m_ShopData.Rewards.Currencies.Count > 0)
            {
                if (m_Title == "")
                    m_Title = m_ShopData.Rewards.Currencies[0].Qty.ToString();

                if (m_ShopData.Icon == null)
                    m_ShopData.Icon = AssetLoader.LoadCurrencyIcon(m_ShopData.Rewards.Currencies[0].Currency, m_ShopData.Rewards.Currencies[0].Qty);

                m_TitleColor = ShopManagementData.GetCurrencyColor(m_ShopData.Rewards.Currencies[0].Currency);
            }

            // CHESTS
            else if (m_ShopData.Rewards.Chests.Count > 0)
            {
                if (m_Title == "")
                    m_Title = m_ShopData.Rewards.Chests[0].ToString() + " Chest";

                if (m_ShopData.Icon == null)
                    m_ShopData.Icon = AssetLoader.LoadChestIcon(m_ShopData.Rewards.Chests[0]);
            }

            // COLLECTABLES
            else if (m_ShopData.Rewards.Collectables.Count > 0)
            {
                if (m_Title == "")
                    m_Title = m_ShopData.Rewards.Collectables[0].Qty.ToString();

                var collectable = CollectablesManagementData.Cast(m_ShopData.Rewards.Collectables[0].CollectableName, m_ShopData.Rewards.Collectables[0].CollectableType);
                if (m_ShopData.Icon == null)
                    m_ShopData.Icon = AssetLoader.LoadIcon(collectable);

                var raretyData = CollectablesManagementData.GetRaretyData(collectable);
                if (raretyData.Rarety > ERarety.Common)
                {
                    m_TitleColor = raretyData.Color;
                }
                m_BorderColor = raretyData.Color;
            }

            // ERROR
            else
                ErrorHandler.Error("Unhandled case : reward title");
        }

        #endregion


        #region GUI Manipulators 

        protected override void SetTitle()
        {
            if (m_Title == null || m_Title == "")
                return;

            m_TemplateItemButton.SetTitle(m_Title, m_TitleColor);
        }

        protected override void SetIcon()
        {
            if (m_ShopData.Icon == null)
                return;

            m_TemplateItemButton.SetIcon(m_ShopData.Icon);

            if (m_ShopData.Rewards.Count == 1 && m_ShopData.Rewards.Collectables.Count == 1)
            {
                m_TemplateItemButton.SetIconProportions(60, 60);
            }
        }

        protected override void SetPrice()
        {
            TextAlignmentOptions alignment = m_CurrencyIcon.gameObject.activeInHierarchy ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Center;
            m_TemplateItemButton.SetBottomOverlay(m_CostString, textColor: ShopManagementData.GetCurrencyColor(m_ShopData.Currency), alignment: alignment);
        }

        protected void SetUpBorder()
        {
            if (m_BorderColor == null)
                return;
            
            m_TemplateItemButton.SetBorderColor(m_BorderColor);
        }

        #endregion


        #region State Management

        protected override void SetState(EButtonState state)
        {
            m_TemplateItemButton.SetState(state);
        }

        #endregion
    }
}