using Data.GameManagement;
using Enums;
using Inventory;
using Menu.Common;
using Save;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Menu
{
    public class AccountXpContainer : MonoBehaviour
    {
        #region Members

        CollectionFillBar       m_CollectionFillbar;
        TMP_Text                m_LevelText;

        int                     m_CurrentXp;

        #endregion


        #region Init & End

        protected void Awake()
        {
            m_CollectionFillbar = Finder.FindComponent<CollectionFillBar>(gameObject);
            m_LevelText = Finder.FindComponent<TMP_Text>(gameObject, "Level");

            RefreshUI();
            InventoryCloudData.CurrencyChangedEvent += OnCurrencyChanged;

            if (NotificationCloudData.XpCollection > 0)
                NotificationCloudData.CollectXp();
        }

        protected void OnDestroy()
        {
            InventoryCloudData.CurrencyChangedEvent -= OnCurrencyChanged;
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            if (ProfileCloudData.IsAccountMaxed)
            {
                m_CollectionFillbar.Initialize(0, 0);
                return;
            }

            m_CurrentXp = InventoryManager.GetCurrency(ECurrency.TotalXp);

            m_LevelText.text = ProfileCloudData.AccountLevel.ToString();
            m_CollectionFillbar.Initialize(InventoryManager.GetCurrency(ECurrency.TotalXp), CollectablesManagementData.GetCurrentAccountLevelData().RequiredXp);

            if (CollectablesManagementData.IsAccountUpgradable)
                LevelUpAccount();
        }

        IEnumerator CollectXpCoroutine(int xpGained)
        {
            yield return m_CollectionFillbar.CollectionAnimationCoroutine(xpGained);

            m_CurrentXp = InventoryManager.GetCurrency(ECurrency.TotalXp);

            if (CollectablesManagementData.IsAccountUpgradable)
                LevelUpAccount();
        }

        #endregion


        #region Rewards

        void LevelUpAccount()
        {
            Main.DisplayRewards(CollectablesManagementData.GetCurrentAccountLevelData().Rewards, "AccountLevelUp");
            ProfileCloudData.UpgradeAccountLevel();
            
            RefreshUI();
        }
 
        #endregion


        #region Listeners

        void OnCurrencyChanged(ECurrency currency, int amount = 0)
        {
            if (currency != ECurrency.TotalXp)
                return;

            int gainedXp = m_CurrentXp - amount;

            if (gainedXp > 0)
                StartCoroutine(CollectXpCoroutine(amount));
            else
                RefreshUI();
        }

        #endregion
    }
}