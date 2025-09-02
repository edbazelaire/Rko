using Data;
using Data.DataStructures.PowerEffects;
using Save;
using Tools;
using Unity.VisualScripting;
using UnityEngine;


namespace Menu.PopUps.OverlayScreens
{
    public class PowerUpInfoScreen : OverlayScreen
    {
        #region Members

        SPowerEffect    m_PowerUpData;
        GameObject      m_Container;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Container = Finder.Find(gameObject, "Container");
        }

        public virtual void Initialize(SPowerEffect powerUpData)
        {
            m_PowerUpData = powerUpData;
            base.Initialize();
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            UIHelper.CleanContent(m_Container);
            Instantiate(AssetLoader.LoadPowerUpItem(m_PowerUpData.RuneActivation), m_Container.transform).Initialize(m_PowerUpData); 
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

