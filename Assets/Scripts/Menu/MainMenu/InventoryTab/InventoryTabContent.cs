using Enums;
using Menu.Common.Buttons;
using Save;
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

            // call late initialize
            CoroutineManager.DelayMethod(DelayedInitialize);
        }

        /// <summary>
        /// Initialization that takes place 1 frame after the init (to be sure every layouts are set properly)
        /// </summary>
        public void DelayedInitialize()
        {
            m_CharacterPreviewSectionUI.Initialize();

            // register listeners & buttons
            m_CharacterPreviewSectionUI.CharacterPreviewButton.onClick.AddListener(() => SelectTab(CharacterBuildsCloudData.SelectedCharacter));
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

            m_CharacterPreviewSectionUI.CharacterPreviewButton.onClick.RemoveAllListeners();
            TemplateCollectableItemUI.ButtonClickedEvent -= SelectTab;
        }

        #endregion


        #region Tab Selection

        void SelectTab(Enum collectable)
        {
            if (collectable.GetType() == typeof(ESpell))
                m_ItemsTabManager.SelectTab(EInvetoryItemTab.SpellsTab);

            else if (collectable.GetType() == typeof(ERune))
                m_ItemsTabManager.SelectTab(EInvetoryItemTab.RunesTab);

            else if (collectable.GetType() == typeof(ECharacter))
                m_ItemsTabManager.SelectTab(EInvetoryItemTab.CharactersTab);

            else
            {
                ErrorHandler.Error("Unahandle case : " + collectable.GetType());
                return;
            }
        }

        #endregion
    }
}
