using Data;
using Enums;
using Game.Loaders;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.Common.Filters
{
    public class SpellFiltersSection : MObject
    {
        #region Members

        // ==========================================================================
        // Events
        public Action FilterChangedEvent;

        // ==========================================================================
        // GameObjects & Components
        TMP_InputField        m_SearchInputField;
        SpellFilterDropdown   m_ElementsDropdown;
        SpellFilterDropdown   m_RaretyDropdown;

        // ==========================================================================
        // Public Accessors
        public TMP_InputField SearchInputField => m_SearchInputField;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SearchInputField  = Finder.FindComponent<TMP_InputField>(gameObject, "SearchInputField");
            m_ElementsDropdown  = Finder.FindComponent<SpellFilterDropdown>(gameObject, "ElementsDropdown");
            m_RaretyDropdown    = Finder.FindComponent<SpellFilterDropdown>(gameObject, "RaretyDropdown");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_ElementsDropdown.Initialize(typeof(ESpellElement), defaultValue: SpellFilterDropdown.CLEAR_VALUE);
            m_RaretyDropdown.Initialize(typeof(ERarety), defaultValue: SpellFilterDropdown.CLEAR_VALUE);
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Spell Selection

        public List<SpellData> GetFilteredSpells()
        {
            // Search Input Field overrides filters
            if (m_SearchInputField.text != "")
            {
                return SpellLoader.OrderSpells(SpellLoader.FilterSpells(
                    unlocked: true, 
                    containsName: m_SearchInputField.text
                ), EOrderBy.Rarety);
            }
            else
            {
                return SpellLoader.OrderSpells(SpellLoader.FilterSpells(
                    raretyFilters:          m_RaretyDropdown.GetValues<ERarety>(),
                    spellElementFilters:    m_ElementsDropdown.GetValues<ESpellElement>(),
                    unlocked:               true
                ), EOrderBy.Rarety);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_ElementsDropdown.Dropdown.onValueChanged.AddListener(OnFilterValueChanged);
            m_RaretyDropdown.Dropdown.onValueChanged.AddListener(OnFilterValueChanged);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_ElementsDropdown.Dropdown.onValueChanged.RemoveListener(OnFilterValueChanged);
            m_RaretyDropdown.Dropdown.onValueChanged.RemoveListener(OnFilterValueChanged);
        }

        protected void OnFilterValueChanged(int value)
        {
            FilterChangedEvent?.Invoke();
        }

        #endregion
    }
}

    
