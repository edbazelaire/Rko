using Data.GameManagement;
using Enums;
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

            TextAlignmentOptions alignment = m_CurrencyIcon.gameObject.activeInHierarchy ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Center;
            m_Price.text = TextHandler.FormatPrice(priceData.Price, priceData.Currency);

            if (priceData.Currency == ECurrency.Real)
            {
                m_CurrencyIcon.transform.parent.gameObject.SetActive(false);
            } 
            else
            {
                m_CurrencyIcon.transform.parent.gameObject.SetActive(true);
                m_CurrencyIcon.sprite = AssetLoader.LoadCurrencyIcon(priceData.Currency);
            }
        }
    }
}