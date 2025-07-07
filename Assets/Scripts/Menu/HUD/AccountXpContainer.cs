using Data.GameManagement;
using Enums;
using Inventory;
using Menu.Common;
using Save;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;

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
            }
            else
            {
                m_CollectionFillbar.Initialize(InventoryManager.GetCurrency(ECurrency.TotalXp), CollectablesManagementData.GetCurrentAccountLevelData().RequiredXp);
            }

            m_CurrentXp = (int)InventoryManager.GetCurrency(ECurrency.TotalXp);
            m_LevelText.text = ProfileCloudData.AccountLevel.ToString();
        }

        IEnumerator CollectXpCoroutine(int xpGained)
        {
            yield return m_CollectionFillbar.CollectionAnimationCoroutine(xpGained);

            RefreshUI();
        }

        #endregion


        #region Listeners

        void OnCurrencyChanged(ECurrency currency, int newValue = 0)
        {
            if (currency != ECurrency.TotalXp)
                return;

            int gainedXp = newValue - m_CurrentXp;

            if (gainedXp > 0)
                StartCoroutine(CollectXpCoroutine(gainedXp));
            else
                RefreshUI();
        }

        #endregion
    }
}