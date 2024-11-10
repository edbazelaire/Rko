using Assets.Scripts.Data.PowerUp;
using Game.UI.EndGameUI;
using System.Collections;
using Tools;
using UnityEngine;


namespace Menu.PopUps.OverlayScreens
{
    public class PowerUpSelectionScreen : OverlayScreen
    {
        #region Members

        PowerUpSection m_PowerUpSection;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PowerUpSection = Finder.FindComponent<PowerUpSection>(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_PowerUpSection.Initialize();
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

