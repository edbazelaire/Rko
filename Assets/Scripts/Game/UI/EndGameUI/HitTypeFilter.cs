using Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Game.UI.EndGameUI
{
    public class HitTypeFilter : MObject
    {
        #region Members

        // =======================================================================================
        // Data
        private HashSet<EHitType> m_VisibleHitTypes = new();

        // =======================================================================================
        // GameObjects & Components
        // TODO : AddCheckbox for each HitType with name attached that activate visibility

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
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


        #region Filters

        public void SetVisibility(EHitType hitType, bool isVisible)
        {
            if (isVisible)
                m_VisibleHitTypes.Add(hitType);
            else
                m_VisibleHitTypes.Remove(hitType);
        }

        public bool IsHitTypeVisible(EHitType hitType)
        {
            return m_VisibleHitTypes.Contains(hitType);
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
