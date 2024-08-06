using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using UnityEngine;


namespace Menu.Common.Filters
{
    public class SpellFilterDropdown : MObject
    {
        #region Members

        // =============================================================================================
        // Constants
        public const string NO_FILTER_VALUE = "No Filter";

        // =============================================================================================
        // Data
        [SerializeField] protected FilterIcon m_IconTemplate;
        protected Type m_EnumType;
        protected List<string> m_CurrentValues = new();

        // =============================================================================================
        // GameObjects & Components
        protected TMP_Dropdown  m_Dropdown;
        protected GameObject    m_Content;
        protected TMP_Text      m_Text;
        protected GameObject    m_ValuesContainer;

        // =============================================================================================
        // Dependent Accessors
        public virtual Type EnumType => m_EnumType;
        public TMP_Dropdown Dropdown => m_Dropdown;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Dropdown          = Finder.FindComponent<TMP_Dropdown>(gameObject);
            m_Text              = Finder.FindComponent<TMP_Text>(gameObject, "Text");
            m_ValuesContainer   = Finder.Find(gameObject, "ValuesContainer");
        }

        public virtual void Initialize(Type enumType)
        {
            m_EnumType = enumType;
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            
            UIHelper.CleanContent(m_ValuesContainer);
            m_Text.gameObject.SetActive(true);
            m_ValuesContainer.SetActive(false);

            UIHelper.SetUpMultiDropdown(m_Dropdown, Enum.GetNames(EnumType).ToList(), "", OnValueChanged);
        }

        #endregion


        #region Values

        public List<T> GetValues<T>()
        {
            List<T> enumValues = new();
            foreach (string value in m_CurrentValues)
            {
                if (!Enum.TryParse(typeof(T), value, out object enumValue))
                {
                    ErrorHandler.Error("Unable to parse " + value + " in " + typeof(T));
                    continue;
                }

                enumValues.Add((T)enumValue);
            }

            return enumValues;
        }

        #endregion


        #region GUI Manipulators

        protected virtual void ClearContent()
        {
            UIHelper.CleanContent(m_ValuesContainer);
            m_CurrentValues.Clear();
            m_Text.gameObject.SetActive(true);
            m_ValuesContainer.SetActive(false);
            return;
        }

        protected virtual void AddFilterIcon(string value)
        {
            var filterIcon = Instantiate(m_IconTemplate, m_ValuesContainer.transform);
            filterIcon.Initialize(value);
        } 

        protected virtual void RemoveFilterIcon(string value)
        {
            var filterIcon = Finder.Find(m_ValuesContainer, FilterIcon.GetName(value));
            if (filterIcon == null)
                return;

            Destroy(filterIcon);
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

        protected virtual void OnValueChanged(List<string> newValues)
        {
            if (newValues.Count == 0)
            {
                ClearContent();
                return;
            }

            UIHelper.CleanContent(m_ValuesContainer);

            if (m_CurrentValues.Count == 0)
            {
                m_Text.gameObject.SetActive(false);
                m_ValuesContainer.SetActive(true);
            }

            foreach (string value in newValues)
            {
                AddFilterIcon(value);
            }

            m_CurrentValues = newValues;
        }

         #endregion
    }
}
