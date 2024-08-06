using Assets;
using Assets.Scripts.Menu.Common.Buttons.SubButtons;
using Assets.Scripts.Menu.Common.Buttons.TemplateItemButtons;
using Data.GameManagement;
using Enums;
using Inventory;
using Menu.Common.Buttons;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class ConfirmBuyItemPopUp : ConfirmBuyPopUp
    {
        #region Members

        // Data
        Enum            m_Item;
        int             m_Qty;
        bool            m_IsUnlock;

        // GameObjects & Components
        GameObject      m_PreviewContainer;
        TMP_Text        m_QtyText;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PreviewContainer = Finder.Find(m_WindowContent, "PreviewContainer");
            m_QtyText = Finder.FindComponent<TMP_Text>(m_WindowContent, "QtyText");
        }

        public void Initialize(SPriceData priceData, Enum item, int qty, Action onValidate, Action onCancel)
        {
            // if item is a Collectable, and current level is 0 -> this is an unlock 
            m_IsUnlock = CollectablesManagementData.TryGetCollectableType(item, out var temp) && InventoryCloudData.Instance.GetCollectable(item).Level == 0;
            m_Item = item;
            m_Qty = qty;

            SRewardsData rewardsData = new SRewardsData();
            rewardsData.Add(item, qty);

            base.Initialize(m_Item.ToString(), priceData, rewardsData, onValidate, onCancel);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            UIHelper.CleanContent(m_PreviewContainer);

            // handle chests
            if (m_Item.GetType() ==  typeof(EChest)) 
            {
                var item = Instantiate(AssetLoader.LoadTemplateItem(m_Item), m_PreviewContainer.transform);
                item.GetComponent<Image>().sprite = AssetLoader.LoadChestIcon(m_Item.ToString());
                return;
            }

            // handle collectables
            var templateItem = Instantiate(AssetLoader.LoadTemplateItem(m_Item), m_PreviewContainer.transform).GetComponent<TemplateItemButton>();
            if (m_Item.GetType() == typeof(ECurrency)) 
            {
                (templateItem as TemplateCurrencyItem).Initialize((ECurrency)m_Item, m_Qty);
            } else if (CollectablesManagementData.TryGetCollectableType(m_Item, out var cType))
            {
                (templateItem as TemplateCollectableItemUI).Initialize(m_Item, asIconOnly: true);
                (templateItem as TemplateCollectableItemUI).ForceState(EButtonState.Normal);
            } 
            else {
                ErrorHandler.Error("Unhandled type of item : " + m_Item.GetType());
                Exit();
            }

            if (m_Qty <= 1)
            {
                m_QtyText.gameObject.SetActive(false);  
            } else
            {
                m_QtyText.gameObject.SetActive(true);
                m_QtyText.text = "x" + m_Qty.ToString();
            }
        }

        #endregion


        #region Helpers

        protected override string GetMessage()
        {
            string action = m_IsUnlock ? "unlock" : "buy";
            string qty = m_Qty <= 1 ? "" : " x" + m_Qty;
            return $"Do you want to {action} {m_Item}{qty} ?";
        }

        #endregion
    }
}