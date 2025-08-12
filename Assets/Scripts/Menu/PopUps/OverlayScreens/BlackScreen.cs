using Assets.Scripts.Managers;
using Tools;
using UnityEngine.UI;


namespace Menu.PopUps.OverlayScreens
{
    public class BlackScreen : OverlayScreen
    {
        #region Members

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public override void Initialize()
        {
            ScreenManager.AddScreen(this);
            gameObject.SetActive(true);
            m_Initialized = true;
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
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
