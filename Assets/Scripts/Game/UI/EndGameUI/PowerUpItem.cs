using Assets.Scripts.Data.PowerUp;
using Data.GameManagement;
using Game.Loaders;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Game.UI.EndGameUI
{
    public class PowerUpItem : MObject
    {
        #region Members

        PowerUpData m_PowerUpData;

        TMP_Text    m_Title;
        TMP_Text    m_Description;
        Image       m_Icon;
        Button      m_Button;

        public PowerUpData PowerUpData => m_PowerUpData;
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

        public void Initialize(PowerUpData powerUpData)
        {
            m_PowerUpData = powerUpData;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Title.text        = TextHandler.SplitCamelCase(m_PowerUpData.BaseName);
            m_Description.text  = m_PowerUpData.GetDescription();
            m_Icon.sprite       = AssetLoader.LoadIcon(m_PowerUpData.BaseName);
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
