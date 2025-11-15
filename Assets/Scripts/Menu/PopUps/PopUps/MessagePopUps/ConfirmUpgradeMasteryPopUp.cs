using Menu.Common.Displayers;
using Tools;
using UnityEngine.UI;
using UnityEngine;
using Data.GameManagement;
using System;
using Inventory;
using Game.Loaders;
using Enums;
using MyBox;

namespace Menu.PopUps
{
    public class ConfirmUpgradeMasteryPopUp : ConfirmBuyBundlePopUp
    {
        #region Members
       
        // Data
        Enum m_Collectable;
        int m_Mastery;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public void Initialize(Enum collectable, int mastery, SPriceData priceData, bool watchAd = false)
        {
            // only Character mastery is handled atm
            if (collectable is not ECharacter character)
            {
                ErrorHandler.Error("Trying to display mastery for a collectable that is NOT a CHARACTER : " + m_Collectable);
                Exit();
                return;
            }

            m_Collectable = collectable;
            m_Mastery = mastery;

            base.Initialize(
                itemName:       collectable.ToString(),
                productId:      "",
                priceData:      priceData,
                watchAd:        watchAd,
                rewardsData:    AchievementLoader.GetAllRewardsAtMastery(character, m_Mastery),
                onValidate:     () => InventoryManager.UpgradeMastery(collectable, mastery),
                onCancel:       null
            );
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_Title.text = "Mastery " + TextHandler.ToRoman(m_Mastery);

            string reductionText = "";
            if (CollectablesManagementData.TryGetMasteryPriceReduction(m_Collectable, m_Mastery, out SPriceData priceReduction, withError: true))
            {
                var color = ShopManagementData.GetCurrencyColor(priceReduction.Currency);
                reductionText = $"\n\nLevel up cost : <color={color.ToHex()}>-{priceReduction.Price * 100}% {priceReduction.Currency}</color>";
            }

            m_MessageText.text  = $"Upgrade {m_Collectable} to mastery {m_Mastery}." +
                reductionText +
                $"\n\nUnlock the next set of achievements, that contains the following rewards";

            m_MessageContainer.SetActive(true);
        }

        #endregion


            #region GUI Manipulators


            #endregion
    }
}