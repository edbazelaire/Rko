using Enums;
using Game.UI.EndGameUI;
using Tools;


namespace Menu.PopUps.OverlayScreens
{
    public class PowerUpSelectionScreen : OverlayScreen
    {
        #region Members

        protected PowerUpSection m_PowerUpSection;
        int m_Index;
        ERuneActivation? m_RuneActivation = null;

        public PowerUpSection PowerUpSection => m_PowerUpSection;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PowerUpSection = Finder.FindComponent<PowerUpSection>(gameObject);
        }

        public void Initialize(int index = -1, ERuneActivation? runeActivation = null)
        {
            m_Index = index;
            m_RuneActivation = runeActivation;
            base.Initialize();
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_PowerUpSection.Initialize(m_Index, m_RuneActivation);
            m_PowerUpSection.Activate(true);
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_PowerUpSection.OnEndEvent += Exit;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_PowerUpSection.OnEndEvent -= Exit;
        }

        #endregion
    }
}

