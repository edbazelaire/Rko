using Enums;
using Menu.Common.Buttons;
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
            TemplateCollectableItemUI.ButtonClickedEvent += SelectTab;
        }

        public override void Activate(bool activate)
        {
            base.Activate(activate);

            m_CharacterPreviewSectionUI.Activate(activate);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            TemplateCollectableItemUI.ButtonClickedEvent -= SelectTab;
        }

        #endregion


        #region Listeners

        void SelectTab(Enum collectable)
        {
            if (collectable.GetType() == typeof(ESpell))
                m_ItemsTabManager.SelectTab(EInvetoryItemTab.SpellsTab);

            else if (collectable.GetType() == typeof(ERune))
                m_ItemsTabManager.SelectTab(EInvetoryItemTab.RunesTab);
        }

        #endregion
    }
}
