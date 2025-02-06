using Assets;
using Assets.Scripts.Managers;
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

        void OnCurrencyChanged(ECurrency currency, int total = 0)
        {
            switch (currency)
            {
                case ECurrency.TotalXp:
                    int gainedXp = total - m_CurrentXp;
                    m_CurrentXp = total;

                    // if account can be upgraded : call level up screen
                    if (gainedXp > 0 && CollectablesManagementData.IsAccountUpgradable)
                    {
                        // if is in "RewardScreen" -> wait for rewards to be over before displaying level up
                        if (ScreenManager.CurrentScreen != null && ScreenManager.CurrentScreen.PopUpState == EPopUpState.RewardsScreen)
                            ScreenManager.StoreEvent(ScreenManager.Screens.Count > 1 ? ScreenManager.Screens[^2].PopUpState : EPopUpState.MainMenuScreen, () => LevelUpAccount(gainedXp));
                        else
                            LevelUpAccount(gainedXp);

                        return;
                    }

                    break;
            }   
        }

        #endregion
    }
}
