using Inventory;
using Menu.Common.Rewards;
using System.Collections;
using TMPro;
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
        protected TMP_Text                  m_BonusPower;

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
            m_BonusPower = Finder.FindComponent<TMP_Text>(gameObject, "BonusPower", false);
        }

        public virtual void Initialize(SPowerOrb powerOrb, int maxPower, float bonusPower = 1f)
        {
            m_PowerOrb = powerOrb;

            base.Initialize();

            m_OrbFillBar.Initialize(powerOrb.Power, maxPower);
            m_PowerOrbUI.Initialize(powerOrb);

            m_BonusPower.gameObject.SetActive(bonusPower != 1);
            if (bonusPower != 1)
                m_BonusPower.text = "(Bonus: +" + Mathf.Round((bonusPower - 1) * 100) + "%)";
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

