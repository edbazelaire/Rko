using System;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Dots
{
    public class DotUI : MObject
    {
        #region Members

        // =============================================================================================
        // Serialize Fields
        [SerializeField] protected Color m_DiseableColor;
        [SerializeField] protected Color m_IconDiseableColor;

        // =============================================================================================
        // GameObjects & Components
        Image m_Container;
        Image m_Icon;

        // =============================================================================================
        // Data
        bool m_IsActivated;
        Color m_BaseColor;
        Color m_BaseIconColor;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Container = Finder.FindComponent<Image>(gameObject);
            if (m_Container != null)
                m_BaseColor = m_Container.color;

            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon", false);
            if (m_Icon != null)
                m_BaseIconColor = m_Icon.color;
        }

        public virtual void Initialize(bool activated)
        {
            base.Initialize();

            Activate(activated);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        public virtual void Activate(bool activate)
        {
            m_IsActivated = activate;

            if (m_Container != null)
                m_Container.color = activate ? m_BaseColor : m_DiseableColor;

            if (m_Icon != null)
                m_Icon.color = activate ? m_BaseIconColor : m_IconDiseableColor;
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
