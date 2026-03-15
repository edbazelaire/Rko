using Assets.Scripts.Managers;
using Enums;
using Inventory;
using Menu.MainMenu;
using Save;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Menu
{
    public class CurrencyContainer : MonoBehaviour
    {
        #region Members

        [SerializeField] ECurrency m_Currency;

        Image       m_Icon;
        Button      m_Button;
        TMP_Text    m_TextValue;

        #endregion


        #region Init & End

        protected void Awake()
        {
            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Button = Finder.FindComponent<Button>(gameObject);
            m_TextValue = Finder.FindComponent<TMP_Text>(gameObject, "Value");

            m_Icon.sprite = AssetLoader.LoadCurrencyIcon(m_Currency);
            m_TextValue.text = TextHandler.FormatNumericalString(InventoryManager.GetCurrency(m_Currency));

            m_Button.onClick.AddListener(OnCurrencyButtonClicked);
            InventoryCloudData.CurrencyTotalChangedEvent += OnCurrencyChanged;
        }

        protected void OnDestroy()
        {
            m_Button.onClick.RemoveAllListeners();
            InventoryCloudData.CurrencyTotalChangedEvent -= OnCurrencyChanged;
        }

        #endregion


        #region Listeners

        void OnCurrencyChanged(ECurrency currency, int amount = 0)
        {
            if (currency != m_Currency)
                return;

            m_TextValue.text = InventoryManager.GetCurrency(m_Currency).ToString();
        }

        void OnCurrencyButtonClicked()
        {
            if (! ScreenManager.GoToScreen(EPopUpState.MainMenuScreen))
                return;

            ScreenManager.MainMenuManager.SelectTab(EMainMenuTabs.ShopTab, withAnim: false);
            ShopTabContent tab = ScreenManager.MainMenuManager.GetCurrentTab<ShopTabContent>();
            if (tab == null)
            {
                ErrorHandler.Error("Unable to select InventoryTab");
                return;
            }

            tab.SelectCurrency(m_Currency);
        }

        #endregion
    }
}