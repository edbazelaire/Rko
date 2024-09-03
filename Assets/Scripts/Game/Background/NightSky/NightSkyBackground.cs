using Game.Background.NightSky;
using Tools;


namespace Game.Background
{
    public class NightSkyBackground : ArenaBackground
    {
        #region Members

        CloudSpawner m_CloudSpawner;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CloudSpawner = Finder.FindComponent<CloudSpawner>(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();

            m_CloudSpawner.Initialize();
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