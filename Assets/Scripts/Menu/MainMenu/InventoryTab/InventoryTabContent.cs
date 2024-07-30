using System;
using Tools;
using UnityEngine;


namespace Menu.MainMenu
{
    public class InventoryTabContent : MainMenuTabContent
    {
        #region Members

        /// <summary> handles the preview of the character (Prefab, name, level, ...) </summary>
        CharacterPreviewSectionUI   m_CharacterPreviewSectionUI;
        /// <summary> handles the Tabs of the inventory (spells, characters, ...) </summary>
        ItemsTabManager             m_ItemsTabManager;

        #endregion


        #region Init & End

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            m_CharacterPreviewSectionUI = Finder.FindComponent<CharacterPreviewSectionUI>(gameObject, "CharacterPreviewSection");
            m_ItemsTabManager = Finder.FindComponent<ItemsTabManager>(gameObject, "ItemsTabManager");

            // initialization
            m_ItemsTabManager.Initialize();
            CoroutineManager.DelayMethod(m_CharacterPreviewSectionUI.Initialize);

            // register listeners & buttons
            TemplateSpellItemUI.ButtonClickedEvent += SelectSpellItemsTab;
            // delay method by one frame to avoid Awake() issues
            CoroutineManager.DelayMethod(() => { m_CharacterPreviewSectionUI.CharacterPreviewButton.onClick.AddListener(SelectCharacterItemsTab); });
        }

        public override void Activate(bool activate)
        {
            base.Activate(activate);

            m_CharacterPreviewSectionUI.Activate(activate);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            TemplateSpellItemUI.ButtonClickedEvent -= SelectSpellItemsTab;
        }

        #endregion


        #region Listeners

        void SelectSpellItemsTab(Enum spell)
        {
            m_ItemsTabManager.SelectTab(EInvetoryItemTab.SpellsTab);
        }

        void SelectCharacterItemsTab()
        {
            m_ItemsTabManager.SelectTab(EInvetoryItemTab.CharactersTab);
        }

        #endregion
    }
}
