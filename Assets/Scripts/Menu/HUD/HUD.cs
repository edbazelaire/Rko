using Enums;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Menu
{
    public class HUD : MObject
    {
        #region Members

        GameObject m_ButtonsContainer;
        Button m_MessagerieButton;
        Button m_SettingsButton;

        #endregion


        #region Init & End

        private void Awake()
        {
            Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ButtonsContainer = Finder.Find(gameObject, "ButtonsContainer");
            m_MessagerieButton = Finder.FindComponent<Button>(m_ButtonsContainer, "MessagerieButton");
            m_SettingsButton = Finder.FindComponent<Button>(m_ButtonsContainer, "SettingsButton");
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_MessagerieButton.onClick.AddListener(() => Main.SetPopUp(EPopUpState.MessageriePopUp));
            m_SettingsButton.onClick.AddListener(() => Main.SetPopUp(EPopUpState.SettingsPopUp));
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_SettingsButton.onClick.RemoveAllListeners();
        }

        #endregion
    }
}