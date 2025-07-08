using Assets;
using Data.GameManagement;
using Menu.Common;
using Save;
using System.Collections.Generic;
using TMPro;
using Tools;

namespace Menu.PopUps
{
    public class LevelUpScreen : OverlayScreen
    {
        #region Members

        // =====================================================================================
        // GameObjects & Components
        CollectionFillBar       m_XpBar;
        TMP_Text                m_LevelText;

        // =====================================================================================
        // Data
        SRewardsData            m_Rewards = new();
        int                     m_BaseXp;
        int                     m_MaxXp;
        int                     m_BonusXp;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_XpBar = Finder.FindComponent<CollectionFillBar>(gameObject, "CollectionFillBar");
            m_LevelText = Finder.FindComponent<TMP_Text>(gameObject, "Level");
        }

        public void Initialize(int baseXp, int maxXp, int bonusXp)
        {
            m_Rewards = new();
            m_BaseXp    = baseXp;
            m_MaxXp     = maxXp;
            m_BonusXp   = bonusXp;

            // update account as many time as possible, and stack all rewards
            while (ProfileCloudData.IsAccountUpgradable)
            {
                m_Rewards.Add(CollectablesManagementData.GetCurrentAccountLevelData().Rewards);
                ProfileCloudData.UpgradeAccountLevel(false);
            }

            // SAVE values at the end of the loop to avoid conflicts
            InventoryCloudData.Instance.SaveValue(InventoryCloudData.KEY_TOTAL_XP);
            InventoryCloudData.Instance.SaveValue(InventoryCloudData.KEY_XP);
            ProfileCloudData.Instance.SaveValue(ProfileCloudData.KEY_CURRENT_PROFILE_DATA);

            // add golds to inventory manager
            base.Initialize();
        }

        #endregion


        #region GUI Manipulators

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_LevelText.text = (ProfileCloudData.AccountLevel - 1).ToString();
            m_XpBar.Initialize(m_BaseXp, m_MaxXp);
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();

            m_XpBar.AddCollectionAnimation(m_BonusXp);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_XpBar.CollectionEndedEvent += OnCollectionEnded;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

             m_XpBar.CollectionEndedEvent -= OnCollectionEnded;
        }

        void OnCollectionEnded()
        {
            Main.DisplayRewards(m_Rewards, "AccountLevelUp", title: "Level up !");
            Exit();
        }

        #endregion
    }
}