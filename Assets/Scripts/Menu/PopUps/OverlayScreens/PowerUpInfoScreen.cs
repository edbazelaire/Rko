using Assets.Scripts.Managers;
using Data;
using Data.DataStructures.PowerEffects;
using Enums;
using Game.Loaders;
using Save;
using TMPro;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.PopUps.OverlayScreens
{
    public class PowerUpInfoScreen : OverlayScreen
    {
        #region Members

        [SerializeField] Button m_RefreshButtonPrefab;

        // Components
        GameObject      m_Container;
        Button          m_RefreshButton;
        TMP_Text        m_RefreshCtr;

        // Data
        SPowerEffect    m_PowerUpData;
        int             m_Index;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Container = Finder.Find(gameObject, "Container");
        }

        public virtual void Initialize(SPowerEffect powerUpData, int index)
        {
            m_PowerUpData = powerUpData;
            m_Index = index;
            base.Initialize(); 
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            UIHelper.CleanContent(m_Container);
            
            // -- instantiate PowerUpItem
            Instantiate(AssetLoader.LoadPowerUpItem(m_PowerUpData.RuneActivation), m_Container.transform).Initialize(m_PowerUpData);
            
            // -- instantiate RefreshButton
            m_RefreshButton = Instantiate(m_RefreshButtonPrefab, m_Container.transform);
            m_RefreshCtr = Finder.FindComponent<TMP_Text>(m_RefreshButton.gameObject, "RefreshCtr");
            RefreshRefreshButton();
        }

        #endregion


        #region GUI Manipulators

        void RefreshRefreshButton()
        {
            if (ProgressionCloudData.CurrentArena.RefreshTokens <= 0)
            {
                m_RefreshButton.gameObject.SetActive(false);
                return;
            }

            m_RefreshButton.gameObject.SetActive(true);
            m_RefreshCtr.text = ProgressionCloudData.CurrentArena.RefreshTokens.ToString();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            m_RefreshButton.onClick.AddListener(OnRefreshClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            m_RefreshButton.onClick.RemoveListener(OnRefreshClicked);
        }

        void OnRefreshClicked()
        {
            ScreenManager.ConfirmPopUp("Use one Refresh to reroll this Power Up ?", title: "Reroll Power Up", onValidate: RefreshPowerUp);
        }

        void RefreshPowerUp()
        {
            ERuneActivation runeActivation = m_PowerUpData.RuneActivation;
            if (SpellLoader.IsRune(m_PowerUpData.BaseName))
                runeActivation -= 1;

            ProgressionCloudData.AddCurrentArenaRefreshToken(-1);
            ScreenManager.PowerUpSelectionScreen(ProgressionCloudData.CurrentArena.ArenaType, m_Index, runeActivation);
            Exit();
        }

        #endregion
    }
}

