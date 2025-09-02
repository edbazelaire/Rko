using Data;
using Data.DataStructures.PowerEffects;
using Save;
using TMPro;
using Tools;
using UnityEngine.UI;


namespace Game.UI.EndGameUI
{
    public class PowerUpItem : MObject
    {
        #region Members

        SPowerEffect    m_PowerUpData;

        TMP_Text        m_Title;
        TMP_Text        m_Description;
        Image           m_Icon;
        Image           m_IconOverlay;
        Button          m_Button;
        Button          m_RefreshButton;

        public SPowerEffect PowerUpData => m_PowerUpData;
        public Button Button            => m_Button;
        public Button RefreshButton     => m_RefreshButton;

        
        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Title         = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_Description   = Finder.FindComponent<TMP_Text>(gameObject, "Description");
            m_Icon          = Finder.FindComponent<Image>(gameObject, "Icon");
            m_IconOverlay   = Finder.FindComponent<Image>(gameObject, "IconOverlay");
            m_Button        = Finder.FindComponent<Button>(gameObject);
            m_RefreshButton = Finder.FindComponent<Button>(gameObject, "RefreshButton");
        }

        public void Initialize(SPowerEffect powerUpData, bool withRefreshButton = false)
        {
            base.Initialize();
            RefreshUI(powerUpData);
            
            m_RefreshButton.gameObject.SetActive(withRefreshButton);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        public void RefreshUI(SPowerEffect powerUpData)
        {
            m_PowerUpData = powerUpData;

            m_Title.text = TextHandler.SplitCamelCase(m_PowerUpData.BaseName);
            m_Description.text = m_PowerUpData.GetDescription();
            m_Icon.sprite = AssetLoader.LoadIcon(m_PowerUpData.BaseName);
            m_IconOverlay.sprite = AssetLoader.LoadPowerUpIconBorder(m_PowerUpData.RuneActivation);
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
