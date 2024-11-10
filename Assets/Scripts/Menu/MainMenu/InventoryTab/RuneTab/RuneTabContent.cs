using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common.Buttons;
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
    public class RuneTabContent : TabContent
    {
        #region Members

        const string RUNE_ITEM_NAME_FORMAT = "RuneItem_{0}";

        TemplateRuneItemUI m_TemplateItem;
        Dictionary<ERune, TemplateRuneItemUI> m_Items = new();

        SpellFiltersSection m_FiltersSection;
        GameObject          m_ParentContent;
        GameObject          m_ItemContainer;
        GameObject          m_LockedItemContainer;

        #endregion


        #region Init & End

        void Awake()
        {
            m_TemplateItem              = AssetLoader.LoadTemplateItem<TemplateRuneItemUI>();
            m_FiltersSection            = Finder.FindComponent<SpellFiltersSection>("RuneFiltersSection", false);
            m_ParentContent             = transform.parent.gameObject;
            m_ItemContainer             = Finder.Find(gameObject, "ItemContainer");
            m_LockedItemContainer       = Finder.Find(gameObject, "LockedItemContainer");

            // Listeners
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  += RefreshItemsDisplay;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  += RefreshItemsDisplay;
            CharacterBuildsCloudData.CurrentRuneChangedEvent        += RefreshItemsDisplay;
            InventoryManager.UnlockCollectableEvent                 += OnUnlocked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tabButton"></param>
        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            // remove content in spell items displayers
            UIHelper.CleanContent(m_ItemContainer);
            UIHelper.CleanContent(m_LockedItemContainer);

            // init Filters
            InitFilters();

            m_Items = new ();
            List<RuneData> runesData = CollectablesManagementData.OrderCollectable(SpellLoader.GetPlayerRunesData(), EOrderBy.Rarety);
            foreach (RuneData runeData in runesData)
            {
                if (runeData.Rune == ERune.None)
                    continue;

                SpawnRuneItem(runeData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            CharacterBuildsCloudData.SelectedCharacterChangedEvent  -= RefreshItemsDisplay;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  -= RefreshItemsDisplay;
            CharacterBuildsCloudData.CurrentRuneChangedEvent        -= RefreshItemsDisplay;
            InventoryManager.UnlockCollectableEvent                 -= OnUnlocked;
            
            if (m_FiltersSection != null)
            {
                m_FiltersSection.FilterChangedEvent -= OnFilterChanged;
                m_FiltersSection.SearchInputField.onValueChanged.RemoveListener(OnSearchValueChanged);
            }
        }

        #endregion


        #region GUI Manipulators

        public override void Activate(bool activate)
        {
            base.Activate(activate);

            if (m_FiltersSection != null)
                m_FiltersSection.gameObject.SetActive(activate);
        }

        protected void SpawnRuneItem(RuneData runeData)
        {
            // check if is unlocked or not
            bool isUnlocked = InventoryCloudData.Instance.GetCollectable(runeData.Rune).Level > 0;
            var parent = isUnlocked ? m_ItemContainer.transform : m_LockedItemContainer.transform;

            // spawn and init ui of the spell
            TemplateRuneItemUI collectableUI = Instantiate(m_TemplateItem, parent);
            collectableUI.gameObject.name = string.Format(RUNE_ITEM_NAME_FORMAT, runeData.Rune.ToString());
            collectableUI.Initialize(runeData.Rune);
            collectableUI.gameObject.SetActive(isUnlocked);
            m_Items.Add(runeData.Rune, collectableUI);

            // hide if spell is in current build
            if (CharacterBuildsCloudData.CurrentRunes.Contains(runeData.Rune))
                collectableUI.gameObject.SetActive(false);
        }

        protected void InitFilters()
        {
            if (m_FiltersSection == null)
                return;

            m_FiltersSection.Initialize();

            m_FiltersSection.FilterChangedEvent += OnFilterChanged;
            m_FiltersSection.SearchInputField.onValueChanged.AddListener(OnSearchValueChanged);
        }

        /// <summary>
        /// When a build or character selected is changed, refresh which spell item is displayed or not
        /// </summary>
        public void RefreshItemsDisplay()
        {
            //var allowedValues = m_FiltersSection.GetFilteredSpells();
            var allowedValues = new List<RuneData>();

            foreach (var item in m_Items)
            {
                bool activate = true;

                // CHECK : is in current build
                if (IsInCurrentBuild(item.Key))
                    activate = false;

                // CHECK : is allowed by filters
                if (activate && ! IsAllowedByFilter(item.Key, allowedValues))
                    activate = false;

                item.Value.gameObject.SetActive(activate);
            }

            // Force layout rebuild for both containers
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_LockedItemContainer.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ItemContainer.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ParentContent.GetComponent<RectTransform>());
        }

        #endregion


        #region Helpers

        bool IsInCurrentBuild(ERune rune)
        {
            return CharacterBuildsCloudData.CurrentRunes.Contains(rune);
        }

        bool IsAllowedByFilter(ERune rune, List<RuneData> allowedValues)
        {
            return true;
            //return allowedValues.Where(data => data.Name == item.Key).ToList().Count > 0;
        }

        #endregion


        #region Listeners

        /// <summary>
        /// When a spell is unlock, change its parent from Locked to normal SpellItemContainer
        /// </summary>
        /// <param name="spell"></param>
        void OnUnlocked(Enum collectable)
        {
            if (collectable.GetType() != typeof(ERune))
                return;

            // find the game object
            if (! m_Items.ContainsKey((ERune)collectable))
            {
                // if not found, try to spawn it
                ErrorHandler.Error("Unable to find rune " + collectable + " in list of items");
                SpawnRuneItem(SpellLoader.GetRuneData((ERune)collectable));
            }

            // activate the object
            m_Items[(ERune)collectable].gameObject.SetActive(true);

            // Force layout rebuild for both containers
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ItemContainer.GetComponent<RectTransform>());
        }

        void OnSearchValueChanged(string value)
        {
            RefreshItemsDisplay();
        }

        void OnFilterChanged()
        {
            // refresh filter only if no search input
            if (m_FiltersSection.SearchInputField.text != "")
                return;

            RefreshItemsDisplay();
        }

        #endregion
    }
}