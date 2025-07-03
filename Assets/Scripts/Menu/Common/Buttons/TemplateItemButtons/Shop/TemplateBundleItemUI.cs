using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class TemplateBundleItemUI : TemplateShopItemUI
    {
        #region Members

        GameObject  m_IconContainer;
        Button      m_BuyButton;
        GameObject  m_LockState;
        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_IconContainer     = Finder.Find(gameObject, "IconContainer");
            m_BuyButton         = Finder.FindComponent<Button>(gameObject, "BuyButton", false);
            m_LockState         = Finder.Find(gameObject, "LockState", false);
        }

        #endregion


        #region GUI Manipulators

        protected override void SetIcon()
        {
            if (m_ShopData.Icon == null)
            {
                m_IconContainer.SetActive(false);
                return;
            }

            m_Icon.sprite = m_ShopData.Icon;
        }

        protected override void SetPrice()
        {
            if (m_PriceText == null)
            {
                ErrorHandler.Warning("Trying to set price for " + name + " but CostText was not provided");
                return;
            }

            m_PriceText.text = m_CostString;
        }

        #endregion


        #region State Management

        protected override void NormalStateUI()
        {
            base.LockedStateUI();

            m_LockState?.gameObject.SetActive(false);
        }

        protected override void LockedStateUI()
        {
            base.LockedStateUI();

            m_LockState?.gameObject.SetActive(true);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_BuyButton != null)
                m_BuyButton.onClick.AddListener(OnClick);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_BuyButton != null)
                m_BuyButton.onClick.RemoveAllListeners();
        }

        #endregion
    }
}