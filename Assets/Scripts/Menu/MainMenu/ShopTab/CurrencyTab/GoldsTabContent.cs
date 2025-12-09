using Data.GameManagement;
using System.Collections.Generic;
using System.Linq;
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
                ShopManagementData.KeysShopData, 
                ShopManagementData.GoldsShopData, 
                ShopManagementData.XpShopData,
                ShopManagementData.GemsShopData
                //ShopManagementData.GemsShopData.GetRange(0, 2)
            });
        }

        #endregion
    }
}