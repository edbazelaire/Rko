using Data;
using Save;
using TMPro;
using Tools;
using UnityEngine.UI;


namespace Game.UI.EndGameUI
{
    public class PowerUpItem : MObject
    {
        #region Members

        SRunePower m_PowerUpData;

        TMP_Text    m_Title;
        TMP_Text    m_Description;
        Image       m_Icon;
        Button      m_Button;

        public SRunePower PowerUpData => m_PowerUpData;
        public Button Button => m_Button;

        
        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Title         = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_Description   = Finder.FindComponent<TMP_Text>(gameObject, "Description");
            m_Icon          = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Button        = Finder.FindComponent<Button>(gameObject);
        }

        public void Initialize(SRunePower powerUpData)
        {
            // adapat level of the powerUp to level of the current character
            powerUpData.SetLevel(InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level);

            // save powerUpData
            m_PowerUpData = powerUpData;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Title.text        = TextHandler.SplitCamelCase(m_PowerUpData.RuneName) + " " + m_PowerUpData.RuneActivation;
            m_Description.text  = m_PowerUpData.GetDescription();
            m_Icon.sprite       = AssetLoader.LoadIcon(m_PowerUpData.RuneName);
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
