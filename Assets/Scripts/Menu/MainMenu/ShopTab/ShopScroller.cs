using Data.GameManagement;
using Menu.Common.Buttons;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Menu.MainMenu.ShopTab
{
    public class ShopScroller : MonoBehaviour
    {
        #region Members

        GameObject m_ContentContainer;

        List<SShopData>[] m_ShopDataList;

        #endregion


        #region Init & End

        public void Initialize(List<SShopData>[] shopData)
        {
            m_ContentContainer = this.gameObject;
            m_ShopDataList = shopData;

            SetUpScroller();
        }

        #endregion


        #region GUI Manipulators

        void SetUpScroller()
        {
            UIHelper.CleanContent(m_ContentContainer);

            foreach (List<SShopData> dataList in m_ShopDataList)
            {
                foreach (SShopData data in dataList)
                {
                    if (data.Abort)
                        continue;

                    var templateShopItem = GetTemplate(data);
                    if (templateShopItem == null)
                        continue;

                    var shopItem = Instantiate(GetTemplate(data), m_ContentContainer.transform);
                    shopItem.Initialize(data);
                }
            }
        }

        /// <summary>
        /// Get appropriate TemplateShopItem for the type of shop data that we present
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected TemplateShopItemUI GetTemplate(SShopData data)
        {
            if (data.Rewards.Count == 0)
            {
                ErrorHandler.Error("No rewards provided for shop data : " + data.Name);
                return null;
            }

            // Single reward : Card type
            if (data.Rewards.Count == 1)
            {
                return AssetLoader.LoadShopTemplateItem<TemplateCardShopItemUI>();
            }
            
            // Multiple rewards : Bundle
            return AssetLoader.LoadShopTemplateItem<TemplateBundleItemUI>("TemplateBundleItem");
        }

        #endregion
    }
}