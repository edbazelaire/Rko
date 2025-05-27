using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Game.Loaders;
using Game.Spells;
using Inventory;
using Menu.Common.Displayers;
using Save;
using System.Collections;
using Tools;
using UnityEngine;


namespace Menu.Common.Rewards
{
    public class PowerOrbContainer : MObject
    {
        #region Members

        // ================================================================================
        // Data
        protected SPowerOrb m_PowerOrbData;
        protected bool m_ActivateIdle = true;

        // ================================================================================
        // GameObjects & Components
        protected PowerOrbUI m_PowerOrbUI;

        public SPowerOrb    PowerOrbData => m_PowerOrbData;
        public PowerOrbUI   PowerOrbUI => m_PowerOrbUI;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public virtual void Initialize(SPowerOrb powerOrb, bool activateIdle = true)
        {
            m_PowerOrbData = powerOrb;
            m_ActivateIdle = activateIdle;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // delay so the canvas is setup when adjusting layout and size of the orb
            CoroutineManager.DelayMethod(RefreshUI, 1);
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI()
        {
            m_PowerOrbUI = UIHelper.SpawnItem(m_PowerOrbData.LoadTemplate().gameObject, gameObject).GetComponent<PowerOrbUI>();
            m_PowerOrbUI.Initialize();
            m_PowerOrbUI.ActivateIdle(m_ActivateIdle, withAura: true, withSound: false);
        }

        public void ActivateIdle(bool activateIdle = true)
        {
            m_ActivateIdle = activateIdle;
            m_PowerOrbUI.ActivateIdle(m_ActivateIdle, withAura: true, withSound: false);
        }

        #endregion


        #region Animations

        public IEnumerator UpgradeSuccessAnimation()
        {
            yield return null;

            RefreshUI();

            StartCoroutine(m_PowerOrbUI.UpgradeSuccessAnimation());
        }

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

