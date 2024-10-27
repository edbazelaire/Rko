using Assets;
using Assets.Scripts.Data.PowerUp;
using Enums;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class PowerUpSmallDisplay : MObject
    {
        #region Members

        PowerUpData m_PowerUpData;

        Image m_Background;
        Image m_Icon;
        Button m_Button;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Background = Finder.FindComponent<Image>(gameObject, "Background");
            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Button = Finder.FindComponent<Button>(gameObject);
        }

        public virtual void Initialize(PowerUpData powerUpData)
        {
            m_PowerUpData = powerUpData;
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            RefreshUI(m_PowerUpData);
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI(PowerUpData powerUpData)
        {
            m_PowerUpData = powerUpData;

            SetUpBackground();
            SetUpIcon();
        }

        void SetUpBackground()
        {
            if (m_PowerUpData == null)
            {
                m_Background.color = new Color(0.2f, 0.2f, 0.2f);
                return;
            }

            m_Background.color = new Color(1f, 1f, 1f);
        }

        void SetUpIcon()
        {
            if (m_PowerUpData == null)
            {
                m_Icon.gameObject.SetActive(false);
                return;
            }

            m_Icon.gameObject.SetActive(true);
            m_Icon.sprite = AssetLoader.LoadIcon(m_PowerUpData.BaseName);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClickButton);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveAllListeners();
        }

        protected void OnClickButton()
        {
            if (m_PowerUpData == null)
                return;

            Main.SetPopUp(EPopUpState.PowerUpInfoScreen, m_PowerUpData);
        }

        #endregion
    }
}

