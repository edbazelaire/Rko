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
        // Actions
        public Action<string> OnDropdownValueChanged;

        // =============================================================================================
        // Constants
        public const string CLEAR_VALUE = "Clear";
        public const string NO_FILTER_VALUE = "No Filter";

        // =============================================================================================
        // Data
        [SerializeField] protected FilterIcon m_IconTemplate;
        protected Type m_EnumType;
        protected string m_DefaultValue;
        bool m_EnableClearValue;
        bool m_EnableNoneValue;

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

        public virtual void Initialize(Type enumType, string defaultValue, bool withClearValue = true, bool allowNone = true)
        {
            m_EnumType          = enumType;
            m_DefaultValue      = defaultValue;
            m_EnableClearValue  = withClearValue;
            m_EnableNoneValue   = allowNone;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            
            UIHelper.CleanContent(m_ValuesContainer);
            m_Text.gameObject.SetActive(true);
            m_ValuesContainer.SetActive(false);

            // init list of values (with CLEAR VALUE first if requested)
            List<string> values = new List<string>() {  };
            if (m_EnableClearValue)
                values.Add(CLEAR_VALUE);

            // add values of enum in dropdown
            values.AddRange(Enum.GetNames(EnumType).ToList());

            // remove "None" if requested
            if (!m_EnableNoneValue && values.Contains("None"))
                values.Remove("None");

            // set dropdown with values
            UIHelper.SetUpDropdown(m_Dropdown, values, m_DefaultValue, OnValueChanged);
        }

        #endregion


        #region Values

        public List<T> GetValues<T>()
        {
            var enumValues = new List<T>();

            string value = m_Dropdown.options[m_Dropdown.value].text;
            if (value == CLEAR_VALUE)
                return enumValues;

            if (! Enum.TryParse(typeof(T), value, out object enumValue))
            {
                ErrorHandler.Error("Unable to parse " + value + " in " + typeof(T));
                return enumValues;
            }

            enumValues.Add((T)enumValue);
            return enumValues;
        }

        #endregion


        #region GUI Manipulators

        protected virtual void ClearContent()
        {
            UIHelper.CleanContent(m_ValuesContainer);
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

        protected virtual void OnValueChanged(string newValue)
        {
            if (newValue == CLEAR_VALUE)
            {
                ClearContent();
            } 
            else
            {
                UIHelper.CleanContent(m_ValuesContainer);

                if (m_Dropdown.value != 0)
                {
                    m_Text.gameObject.SetActive(false);
                    m_ValuesContainer.SetActive(true);
                }

                AddFilterIcon(newValue);
            }

            OnDropdownValueChanged?.Invoke(newValue);
        }

         #endregion
    }
}
