using Data.GameManagement;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Displayers
{
    public class PriceDisplay : MonoBehaviour
    {
        #region Members

        TMP_Text m_Price;
        Image m_CurrencyIcon;

        #endregion

        void FindComponents()
        {
            m_Price = Finder.FindComponent<TMP_Text>(gameObject);
            m_CurrencyIcon = Finder.FindComponent<Image>(gameObject, "CurrencyIcon");   
        }

        public void Initialize(SPriceData priceData)
        {
            FindComponents();

            if (priceData.Price == Mathf.Round(priceData.Price))
                m_Price.text = TextHandler.FormatNumericalString((int)priceData.Price);
            else
                m_Price.text = priceData.Price.ToString();

            m_CurrencyIcon.sprite = AssetLoader.LoadCurrencyIcon(priceData.Currency);
        }
    }
}