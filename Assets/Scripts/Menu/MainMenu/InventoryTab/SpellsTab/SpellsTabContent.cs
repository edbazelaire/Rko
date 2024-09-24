using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using Inventory;
using Menu.Common.Filters;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu
{
    public class SpellsTabContent : TabContent
    {
        #region Members

        const string SPELL_ITEM_NAME_FORMAT = "SpellItem_{0}";

        GameObject m_TemplateSpellItem;
        Dictionary<ESpell, TemplateSpellItemUI> m_SpellItems = new();

        SpellFiltersSection m_SpellFiltersSection;
        GameObject          m_ParentContent;
        GameObject          m_SpellItemContainer;
        GameObject          m_LockedSpellItemContainer;

        #endregion


        #region Init & End

        void Awake()
        {
            m_TemplateSpellItem             = AssetLoader.LoadTemplateItem("SpellItem");
            m_SpellFiltersSection           = Finder.FindComponent<SpellFiltersSection>("SpellFiltersSection");
            m_ParentContent                 = transform.parent.gameObject;
            m_SpellItemContainer            = Finder.Find(gameObject, "SpellItemContainer");
            m_LockedSpellItemContainer      = Finder.Find(gameObject, "LockedSpellItemContainer");

            // Listeners
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  += RefreshSpellItemsDisplay;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  += RefreshSpellItemsDisplay;
            CharacterBuildsCloudData.CurrentBuildValueChangedEvent  += RefreshSpellItemsDisplay;
            InventoryManager.UnlockCollectableEvent                 += OnUnlockedSpell;
            m_SpellFiltersSection.FilterChangedEvent                += OnFilterChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tabButton"></param>
        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            // remove content in spell items displayers
            UIHelper.CleanContent(m_SpellItemContainer);
            UIHelper.CleanContent(m_LockedSpellItemContainer);

            // init Filters
            m_SpellFiltersSection.Initialize();
            m_SpellFiltersSection.SearchInputField.onValueChanged.AddListener(OnSearchValueChanged);

            m_SpellItems = new Dictionary<ESpell, TemplateSpellItemUI>();
            var allSpellsData = SpellLoader.OrderSpells(SpellLoader.SpellsData, EOrderBy.Rarety);
            foreach (SpellData spellData in allSpellsData)
            {
                // skip if spell is linked to a character
                if (spellData.Linked)
                    continue;

                // check if is unlocked or not
                bool isUnlocked = InventoryCloudData.Instance.GetSpell(spellData.Spell).Level > 0;
                var parent = isUnlocked ? m_SpellItemContainer.transform : m_LockedSpellItemContainer.transform;

                // spawn and init ui of the spell
                TemplateSpellItemUI spellUI = Instantiate(m_TemplateSpellItem, parent).GetComponent<TemplateSpellItemUI>();
                spellUI.gameObject.name = string.Format(SPELL_ITEM_NAME_FORMAT, spellData.Name);
                spellUI.Initialize(spellData.Spell);
                spellUI.CollectionFillBar?.gameObject.SetActive(isUnlocked);

                if (isUnlocked)
                    m_SpellItems.Add(spellData.Spell, spellUI);

                // hide if spell is in current build
                if (CharacterBuildsCloudData.CurrentBuild.Contains(spellData.Spell))
                    spellUI.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            CharacterBuildsCloudData.SelectedCharacterChangedEvent  -= RefreshSpellItemsDisplay;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  -= RefreshSpellItemsDisplay;
            CharacterBuildsCloudData.CurrentBuildValueChangedEvent  -= RefreshSpellItemsDisplay;
            InventoryManager.UnlockCollectableEvent                 -= OnUnlockedSpell;
            m_SpellFiltersSection.FilterChangedEvent                -= OnFilterChanged;
            m_SpellFiltersSection.SearchInputField.onValueChanged.RemoveListener(OnSearchValueChanged);
        }

        #endregion


        #region GUI Manipulators

        public override void Activate(bool activate)
        {
            base.Activate(activate);    

            m_SpellFiltersSection.gameObject.SetActive(activate);
        }

        /// <summary>
        /// When a build or character selected is changed, refresh which spell item is displayed or not
        /// </summary>
        public void RefreshSpellItemsDisplay()
        {
            var allowedSpells = m_SpellFiltersSection.GetFilteredSpells();
            foreach (var item in m_SpellItems)
            {
                bool activate = true;

                // CHECK : is in current build
                if (CharacterBuildsCloudData.CurrentBuild.Contains(item.Key))
                    activate = false;

                // CHECK : is allowed by filters
                if (activate && allowedSpells.Where(data => data.Spell == item.Key).ToList().Count == 0)
                    activate = false;

                item.Value.gameObject.SetActive(activate);
            }

            // Force layout rebuild for both containers
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_LockedSpellItemContainer.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_SpellItemContainer.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ParentContent.GetComponent<RectTransform>());
        }

        #endregion


        #region Listeners

        /// <summary>
        /// When a spell is unlock, change its parent from Locked to normal SpellItemContainer
        /// </summary>
        /// <param name="spell"></param>
        void OnUnlockedSpell(Enum spell)
        {
            if (spell.GetType() != typeof(ESpell))
                return;

            // find the item
            TemplateSpellItemUI spellItemUI = Finder.FindComponent<TemplateSpellItemUI>(m_LockedSpellItemContainer, string.Format(SPELL_ITEM_NAME_FORMAT, spell.ToString()));
            if (spellItemUI == null)
                return;

            // change parent
            spellItemUI.transform.SetParent(m_SpellItemContainer.transform);
            spellItemUI.CollectionFillBar?.gameObject.SetActive(true);

            // Force layout rebuild for both containers
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_LockedSpellItemContainer.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_SpellItemContainer.GetComponent<RectTransform>());

            // add spellUI to dict of spell UIs
            m_SpellItems.Add(spellItemUI.Spell, spellItemUI);
        }

        void OnSearchValueChanged(string value)
        {
            RefreshSpellItemsDisplay();
        }

        void OnFilterChanged()
        {
            // refresh filter only if no search input
            if (m_SpellFiltersSection.SearchInputField.text != "")
                return;

            RefreshSpellItemsDisplay();
        }

        #endregion
    }
}