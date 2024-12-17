using Assets;
using Data.GameManagement;
using Enums;
using Save;
using System.Collections;
using UnityEngine;


namespace Managers.MainMenu
{
    public class RewardsManager : MObject
    {
        #region Members

        int m_CurrentXp = 0;

        #endregion


        #region Init & End

        protected void Awake()
        {
            base.Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // refresh current xp
            m_CurrentXp = InventoryCloudData.Instance.GetCurrency(ECurrency.TotalXp);

            if (NotificationCloudData.XpCollection > 0)
            {
                // collect xp from notifications
                NotificationCloudData.CollectXp();
            } 
            else if (CollectablesManagementData.IsAccountUpgradable)
            {
                LevelUpAccount(0);
            }
        }

        #endregion


        #region Rewards Displayers

        protected void LevelUpAccount(int gainedXp)
        {
            Main.SetPopUp(EPopUpState.LevelUpScreen, m_CurrentXp, CollectablesManagementData.GetCurrentAccountLevelData().RequiredXp, gainedXp);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            InventoryCloudData.CurrencyChangedEvent += OnCurrencyChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            InventoryCloudData.CurrencyChangedEvent -= OnCurrencyChanged;
        }

        void OnCurrencyChanged(ECurrency currency, int amount = 0)
        {
            switch (currency)
            {
                case ECurrency.TotalXp:
                    int gainedXp = amount - m_CurrentXp;

                    // if account can be upgraded : call level up screen
                    if (CollectablesManagementData.IsAccountUpgradable)
                        LevelUpAccount(gainedXp);

                    // refresh current xp
                    m_CurrentXp = InventoryCloudData.Instance.GetCurrency(ECurrency.TotalXp);
                    break;
            }   
        }

        #endregion
    }
}
