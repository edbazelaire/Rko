using Assets;
using Assets.Scripts.Data.GameManagement;
using Enums;
using Game.GameManagers.ArenaModules;
using Game.UI.EndGameUI;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine.UI;


namespace Menu.PopUps.OverlayScreens
{
    public class PowerUpSelectionScreen_EM : PowerUpSelectionScreen
    {
        #region Members

        Button m_DowngradeButton;
        Button m_UpgradeButton;

        int m_CurrentStacks     => ProgressionCloudData.CurrentArena.GetMetaData<int>(EArenaMetadataKeys.CorruptionStacks.ToString());
        int m_DowngradeStacks   => (int)ArenaManagementData.EternalMenagerieUpgradeCosts[(int)m_PowerUpSection.RuneActivation - 1].x;
        int m_UpgradeStacks     => (int)ArenaManagementData.EternalMenagerieUpgradeCosts[(int)m_PowerUpSection.RuneActivation - 1].y;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_DowngradeButton = Finder.FindComponent<Button>(gameObject, "DowngradeButton");
            m_UpgradeButton = Finder.FindComponent<Button>(gameObject, "UpgradeButton");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            if (m_PowerUpSection.RuneActivation == ERuneActivation.Minor)
            {
                m_DowngradeButton.gameObject.SetActive(false);
            } else
            {
                var text = Finder.FindComponent<TMP_Text>(m_DowngradeButton.gameObject, "Text");
                text.text = string.Format(text.text, (m_PowerUpSection.RuneActivation - 1).ToString(), m_DowngradeStacks.ToString());
            }

            if (m_PowerUpSection.RuneActivation == ERuneActivation.Primal || (m_CurrentStacks + m_UpgradeStacks > 100 && ! Main.CheatMode))
            {
                m_UpgradeButton.gameObject.SetActive(false);
            } else
            {
                var text = Finder.FindComponent<TMP_Text>(m_UpgradeButton.gameObject, "Text");
                text.text = string.Format(text.text, (m_PowerUpSection.RuneActivation + 1).ToString(), m_UpgradeStacks.ToString());
            }
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_DowngradeButton.onClick.AddListener(Downgrade);
            m_UpgradeButton.onClick.AddListener(Upgrade);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_DowngradeButton.onClick.RemoveAllListeners();
            m_UpgradeButton.onClick.RemoveAllListeners();
        }

        void Downgrade()
        {
            m_DowngradeButton.gameObject.SetActive(false);
            m_UpgradeButton.gameObject.SetActive(false);

            // remove stacks of "CorruptedPower"
            ProgressionCloudData.SetArenaMetaData(EArenaMetadataKeys.CorruptionStacks.ToString(), (m_CurrentStacks - m_DowngradeStacks).ToString());

            // refresh the data
            m_PowerUpSection.RefreshPowerUps(m_PowerUpSection.RuneActivation - 1);
        }

        void Upgrade()
        {
            m_DowngradeButton.gameObject.SetActive(false);
            m_UpgradeButton.gameObject.SetActive(false);

            // pay the price in "CorruptedPower"
            if (!Main.CheatMode)
                ProgressionCloudData.SetArenaMetaData(EArenaMetadataKeys.CorruptionStacks.ToString(), (m_CurrentStacks + m_UpgradeStacks).ToString());

            // refresh the data
            m_PowerUpSection.RefreshPowerUps(m_PowerUpSection.RuneActivation + 1);
        }

        #endregion
    }
}

