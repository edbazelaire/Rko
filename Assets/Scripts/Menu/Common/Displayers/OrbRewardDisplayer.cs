using Inventory;
using Menu.Common.Rewards;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.Common.Displayers
{
    public class OrbRewardDisplayer : MObject
    {
        #region Members

        SPowerOrb m_PowerOrb;

        // ================================================================================
        // Components & GameObjects
        protected PowerOrbContainer         m_PowerOrbUI;
        protected CollectionFillBar         m_OrbFillBar;

        // ================================================================================
        // Public Accessors
        public CollectionFillBar OrbFillbar => m_OrbFillBar;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PowerOrbUI = Finder.FindComponent<PowerOrbContainer>(gameObject, "PowerOrbUI");
            m_OrbFillBar = Finder.FindComponent<CollectionFillBar>(gameObject, "OrbFillBar");
        }

        public virtual void Initialize(SPowerOrb powerOrb, int maxPower)
        {
            m_PowerOrb = powerOrb;

            base.Initialize();

            m_OrbFillBar.Initialize(powerOrb.Power, maxPower);
            m_PowerOrbUI.Initialize(powerOrb);
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

