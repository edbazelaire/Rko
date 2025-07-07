using Assets.Scripts.Managers;
using Tools;
using Unity.Services.Authentication;
using UnityEngine.UI;


namespace Menu.PopUps
{
    public class LoginPopUp : PopUp
    {
        #region Members

        Button m_LoginButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_LoginButton = Finder.FindComponent<Button>(gameObject, "LoginButton");
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

            m_LoginButton.onClick.AddListener(LoginButtonPressed);
            AuthManager.LoginEvent += OnLogin;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_LoginButton.onClick.RemoveListener(LoginButtonPressed);
            AuthManager.LoginEvent -= OnLogin;
        }

        void LoginButtonPressed()
        {
            AuthManager.Instance.Login();
        }

        void OnLogin(bool login)
        {
            // exit the popup when loggin completed
            Exit();
        }

        #endregion
    }
}
