using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.PopUps.OverlayScreens
{
    public class BossInfoScreen : OverlayScreen
    {
        #region Members

        protected Button m_PreviousButton;
        protected Button m_HomeButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PreviousButton = Finder.FindComponent<Button>("PreviousButton");
            m_HomeButton = Finder.FindComponent<Button>("HomeButton");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_PreviousButton.onClick.AddListener(OnPreviousButtonClicked);
            m_HomeButton.onClick.AddListener(OnHomeButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        void OnPreviousButtonClicked()
        {
            OnExit();
        }

        void OnHomeButtonClicked()
        {
            OnExit();
        }

        #endregion
    }
}
