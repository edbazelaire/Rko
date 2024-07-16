using Data.GameManagement;
using Menu.MainMenu.ShopTab;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Menu.MainMenu
{
    public class GoldsTabContent : ShopSubTabContent
    {
        #region Init & End

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            m_Scroller.Initialize(new List<SShopData>[] { 
                ShopManagementData.GoldsShopData, 
                ShopManagementData.XpShopData, 
                //ShopManagementData.GemsShopData       // TODO : SET BACK when Monetization is activated
            });
        }

        #endregion
    }
}