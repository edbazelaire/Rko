using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common.Filters;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu
{
    /// <summary>
    /// Version B - Ultra-optimisée
    /// - Instancie chaque SpellItem UNE SEULE FOIS à l'Initialize
    /// - Maintient une liste triée (m_OrderedSpells) pour définir les indices
    /// - Ne détruit/instancie plus rien ensuite : change parent, SetActive, SetSiblingIndex
    /// - Throttle le LayoutRebuilder (rebuild 1x/frame au maximum)
    /// - Minimise les allocations et appels lourds
    ///
    /// Remarques d'intégration:
    /// - TemplateSpellItemUI doit exposer une méthode UpdateFromData(ESpell) si besoin
    /// - SpellLoader.OrderSpells(...) doit retourner l'ordre souhaité
    /// - Ce script assume que les containers ont un LayoutGroup vertical/horizontal
    /// </summary>
    public class SpellsTabContent : TabContent
    {
        const string SPELL_ITEM_NAME_FORMAT = "SpellItem_{0}";

        GameObject          m_TemplateSpellItem;
        Transform           m_SpellItemContainer;
        Transform           m_LockedSpellItemContainer;
        SpellFiltersSection m_SpellFiltersSection;

        // Runtime
        Dictionary<ESpell, TemplateSpellItemUI> m_SpellItems = new();
        List<SpellData> m_OrderedSpells = new();

        // Layout rebuild throttling
        bool m_LayoutDirty = false;
        Coroutine m_LayoutCoroutine = null;

        // Cache parent rects for faster access
        RectTransform m_ParentContentRect;
        RectTransform m_SpellContainerRect;
        RectTransform m_LockedContainerRect;

        protected override void FindComponents()
        {
            base.FindComponents();

            if (m_TemplateSpellItem == null)
                m_TemplateSpellItem = AssetLoader.LoadTemplateItem("SpellItem");

            if (m_SpellFiltersSection == null)
                m_SpellFiltersSection = Finder.FindComponent<SpellFiltersSection>("SpellFiltersSection");

            if (m_SpellItemContainer == null)
                m_SpellItemContainer = Finder.Find(gameObject, "SpellItemContainer").transform;

            if (m_LockedSpellItemContainer == null)
                m_LockedSpellItemContainer = Finder.Find(gameObject, "LockedSpellItemContainer").transform;

            m_ParentContentRect = transform.parent.GetComponent<RectTransform>();
            m_SpellContainerRect = m_SpellItemContainer.GetComponent<RectTransform>();
            m_LockedContainerRect = m_LockedSpellItemContainer.GetComponent<RectTransform>();
        }

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            // initialize filter section
            m_SpellFiltersSection.Initialize();
            m_SpellFiltersSection.SearchInputField.onValueChanged.AddListener(OnSearchValueChanged);

            // Build order once (can be cached outside if desired)
            m_OrderedSpells = SpellLoader.OrderSpells(SpellLoader.SpellsData, EOrderBy.Rarety)
                .Where(sd => !sd.Linked) // keep only usable spells
                .ToList();

            // Instantiate all SpellItem UI once
            BuildSpellItemsOnce();

            // Initial refresh
            RefreshSpellItemsDisplay();
        }

        void BuildSpellItemsOnce()
        {
            // Clear previous if any
            m_SpellItems.Clear();

            // Ensure containers are empty (only do this at initialize to avoid destroying prefabs later)
            UIHelper.CleanContent(m_SpellItemContainer.gameObject);
            UIHelper.CleanContent(m_LockedSpellItemContainer.gameObject);

            for (int index = 0; index < m_OrderedSpells.Count; index++)
            {
                SpellData spellData = m_OrderedSpells[index];

                bool isUnlocked = InventoryCloudData.Instance.GetSpell(spellData.Spell).Level > 0;
                Transform parent = isUnlocked ? m_SpellItemContainer : m_LockedSpellItemContainer;

                var go = Instantiate(m_TemplateSpellItem, parent);
                go.name = string.Format(SPELL_ITEM_NAME_FORMAT, spellData.Name);

                var ui = go.GetComponent<TemplateSpellItemUI>();
                ui.Initialize(spellData.Spell);
                ui.CollectionFillBar?.gameObject.SetActive(isUnlocked);

                // store reference
                m_SpellItems.Add(spellData.Spell, ui);

                // hide if in current build
                if (CharacterBuildsCloudData.CurrentSpells.Contains(spellData.Spell))
                    ui.gameObject.SetActive(false);

                // set sibling index to match the global ordering so the container has correct order initially
                // If parent = locked container, compute index among locked items: we keep same relative order
                int siblingIndex = ComputeSiblingIndexForInitialization(spellData.Spell, parent == m_SpellItemContainer);
                ui.transform.SetSiblingIndex(siblingIndex);
            }
        }

        int ComputeSiblingIndexForInitialization(ESpell spell, bool isUnlockedContainer)
        {
            // Place index based on ordering but relative to its container.
            // This keeps the initial visual order correct.
            int idx = 0;
            foreach (var sd in m_OrderedSpells)
            {
                bool unlocked = InventoryCloudData.Instance.GetSpell(sd.Spell).Level > 0;
                if (unlocked == isUnlockedContainer)
                {
                    if (sd.Spell == spell)
                        return idx;
                    idx++;
                }
            }
            return idx;
        }

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            // CharacterBuildsCloudData.SelectedCharacterChangedEvent += RefreshSpellItemsDisplay;
            // CharacterBuildsCloudData.CurrentBuildIndexChangedEvent += RefreshSpellItemsDisplay;
            // CharacterBuildsCloudData.CurrentBuildValueChangedEvent += RefreshSpellItemsDisplay;
            InventoryManager.UnlockCollectableEvent += OnUnlockedSpell;
            m_SpellFiltersSection.FilterChangedEvent += OnFilterChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            // CharacterBuildsCloudData.SelectedCharacterChangedEvent -= RefreshSpellItemsDisplay;
            // CharacterBuildsCloudData.CurrentBuildIndexChangedEvent -= RefreshSpellItemsDisplay;
            // CharacterBuildsCloudData.CurrentBuildValueChangedEvent -= RefreshSpellItemsDisplay;
            InventoryManager.UnlockCollectableEvent -= OnUnlockedSpell;
            m_SpellFiltersSection.FilterChangedEvent -= OnFilterChanged;
            m_SpellFiltersSection.SearchInputField.onValueChanged.RemoveListener(OnSearchValueChanged);
        }

        public override void Activate(bool activate)
        {
            base.Activate(activate);
            m_SpellFiltersSection.gameObject.SetActive(activate);
        }

        #region Display Refresh

        /// <summary>
        /// Refresh which spell item is displayed. This is light-weight: we only SetActive and optionally reparent / reindex.
        /// </summary>
        public void RefreshSpellItemsDisplay()
        {
            Debug.LogWarning("RefreshSpellItemsDisplay()");

            var allowedSpells = m_SpellFiltersSection.GetFilteredSpells();
            string searchText = m_SpellFiltersSection.SearchInputField.text;

            // We'll compute visible ordering for the unlocked container to maintain order after filtering
            List<ESpell> visibleUnlockedInOrder = new List<ESpell>();

            // First pass: evaluate each spell's visibility and gather unlocked visible spells in order
            foreach (var sd in m_OrderedSpells)
            {
                if (!m_SpellItems.TryGetValue(sd.Spell, out var ui))
                    continue; // should not happen

                bool activated = true;

                // hidden if in current build
                //if (CharacterBuildsCloudData.CurrentSpells.Contains(sd.Spell))
                //    activated = false;

                // hidden if not matching search
                if (activated && !string.IsNullOrWhiteSpace(searchText))
                {
                    string lower = searchText.Trim().ToLowerInvariant();
                    if (!sd.Name.ToLowerInvariant().Contains(lower) && !sd.Spell.ToString().ToLowerInvariant().Contains(lower))
                        activated = false;
                }

                // hidden if filter excludes
                if (activated && allowedSpells.Where(d => d.Spell == sd.Spell).ToList().Count == 0)
                    activated = false;

                // apply active
                ui.gameObject.SetActive(activated);

                // collect unlocked visible spells in the global order so we can reindex
                bool unlocked = InventoryCloudData.Instance.GetSpell(sd.Spell).Level > 0;
                if (activated && unlocked)
                    visibleUnlockedInOrder.Add(sd.Spell);
            }

            // Second pass: ensure sibling indices for unlocked visible items reflect the global order
            // We'll move only items that are parented to unlocked container.
            for (int i = 0; i < visibleUnlockedInOrder.Count; i++)
            {
                var spell = visibleUnlockedInOrder[i];
                var ui = m_SpellItems[spell];
                if (ui.transform.parent != m_SpellItemContainer)
                    ui.transform.SetParent(m_SpellItemContainer, false);

                // Only change sibling index if different to avoid layout churn
                if (ui.transform.GetSiblingIndex() != i)
                    ui.transform.SetSiblingIndex(i);
            }

            // If filters show locked spells in the locked container we could optionally reorder them similarly
            // For simplicity we'll keep locked container order as originally built.
            MarkLayoutDirty();
        }

        #endregion


        #region Unlock / Reparent

        void OnUnlockedSpell(Enum spellEnum)
        {
            if (spellEnum.GetType() != typeof(ESpell))
                return;

            ESpell spell = (ESpell)spellEnum;

            if (!m_SpellItems.TryGetValue(spell, out var ui))
            {
                // Unexpected: item missing (maybe initialization didn't include it). Fallback: create it.
                // In our "B" design we expect everything to be created at initialize; still handle gracefully.
                CreateMissingSpellItem(spell);
                if (!m_SpellItems.TryGetValue(spell, out ui))
                    return;
            }

            ui.CollectionFillBar?.gameObject.SetActive(true);

            // Compute its place in the unlocked container based on global ordering and current filters
            int targetIndex = GetTargetIndexForUnlocked(spell);

            ui.transform.SetParent(m_SpellItemContainer, false);
            ui.transform.SetSiblingIndex(targetIndex);

            // ensure active state follows filters/build
            RefreshSpellItemsDisplay();
        }

        void CreateMissingSpellItem(ESpell spell)
        {
            // Find SpellData
            var sd = m_OrderedSpells.Find(s => s.Spell == spell);
            if (sd == null) return;

            var go = Instantiate(m_TemplateSpellItem, m_SpellItemContainer);
            go.name = string.Format(SPELL_ITEM_NAME_FORMAT, sd.Name);
            var ui = go.GetComponent<TemplateSpellItemUI>();
            ui.Initialize(spell);
            ui.CollectionFillBar?.gameObject.SetActive(true);
            m_SpellItems.Add(spell, ui);
        }

        int GetTargetIndexForUnlocked(ESpell spell)
        {
            // Target index is number of unlocked spells (in global order) that are visible before this spell
            int idx = 0;
            foreach (var sd in m_OrderedSpells)
            {
                if (sd.Spell == spell)
                    return idx;

                bool unlocked = InventoryCloudData.Instance.GetSpell(sd.Spell).Level > 0;
                bool visible = true;

                // skip if hidden by current build
                if (CharacterBuildsCloudData.CurrentSpells.Contains(sd.Spell))
                    visible = false;

                // skip if hidden by search/filter
                if (visible && m_SpellFiltersSection.SearchInputField.text != "")
                {
                    string lower = m_SpellFiltersSection.SearchInputField.text.Trim().ToLowerInvariant();
                    if (!sd.Name.ToLowerInvariant().Contains(lower) && !sd.Spell.ToString().ToLowerInvariant().Contains(lower))
                        visible = false;
                }

                var allowed = m_SpellFiltersSection.GetFilteredSpells();
                if (visible && allowed.Where(d => d.Spell == sd.Spell).ToList().Count == 0)
                    visible = false;

                if (unlocked && visible)
                    idx++;
            }
            return idx;
        }

        #endregion


        #region Filters / Listeners

        void OnSearchValueChanged(string value)
        {
            // We keep behaviour: if search field has text, filter change events don't rerun
            RefreshSpellItemsDisplay();
        }

        void OnFilterChanged()
        {
            if (m_SpellFiltersSection.SearchInputField.text != "")
                return;

            RefreshSpellItemsDisplay();
        }

        #endregion


        #region Layout Throttling

        void MarkLayoutDirty()
        {
            if (m_LayoutDirty) return;
            m_LayoutDirty = true;
            if (m_LayoutCoroutine != null) StopCoroutine(m_LayoutCoroutine);
            m_LayoutCoroutine = StartCoroutine(DelayedRebuild());
        }

        IEnumerator DelayedRebuild()
        {
            // wait one frame to batch multiple changes
            yield return null;

            // Force rebuild bottom-up
            if (m_LockedContainerRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_LockedContainerRect);
            if (m_SpellContainerRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_SpellContainerRect);
            if (m_ParentContentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_ParentContentRect);

            m_LayoutDirty = false;
            m_LayoutCoroutine = null;
        }

        #endregion
    }
}
