
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;



namespace Menu.Common.Filters
{
    public class FilterIcon : MObject
    {
        #region Members

        // ======================================================================
        // Data
        protected string m_Value;

        // ======================================================================
        // GameObjects & Components
        protected Image     m_Icon;

        // ======================================================================
        // Public Accessors
        public string Value => m_Value;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
        }

        public virtual void Initialize(string value)
        {
            m_Value = value;
            name = GetName(value);      // rename GameObject to easily find / remove it
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Icon.sprite = AssetLoader.LoadFilterIcon(m_Value);
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Helpers

        public static string GetName(string value)
        {
            return value + "Filter";
        }

        #endregion

    }
}
    
