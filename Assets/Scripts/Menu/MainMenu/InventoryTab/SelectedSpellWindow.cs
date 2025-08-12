using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Menu.MainMenu;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Menu.MainMenu.InventoryTab
{
    public class SelectedSpellWindow : MonoBehaviour
    {
        #region Members

        GameObject m_SelectedCardContainer;
        CharacterPreviewSectionUI m_CharacterPreviewSection;
        bool m_IsInitialized;

        #endregion


        #region Init & End

        public void Initialize()
        {
            m_SelectedCardContainer = Finder.Find(gameObject, "SelectedCardContainer");
            m_CharacterPreviewSection = Finder.FindComponent<CharacterPreviewSectionUI>(transform.parent.parent.gameObject, "CharacterPreviewSection");
            m_IsInitialized = true;
            gameObject.SetActive(false);
        }

        #endregion


        #region Activation & Deactivation

        public void Activate(Enum collectable)
        {
            // activate game object and clean potentiel previous content
            gameObject.SetActive(true);
            UIHelper.CleanContent(m_SelectedCardContainer);

            // create spell item
            var spellItem = Instantiate(AssetLoader.LoadTemplateItem(collectable), m_SelectedCardContainer.transform).GetComponent<TemplateCollectableItemUI>();
            spellItem.Initialize(collectable, asIconOnly: true);

            // deactivate ALL buttons that can not be clicked
            RefreshButtonsActivations(collectable);

            // set game object anchors to match parent size
            UIHelper.SetFullSize(spellItem.gameObject);
        }

        public void Deactivate()
        {
            RefreshButtonsActivations(null);
            gameObject.SetActive(false);
        }

        #endregion


        #region Updates

        protected virtual void Update()
        {
            if (!m_IsInitialized)
                return;

            CheckTouch();
        }

        void CheckTouch()
        {
            if (Input.GetMouseButtonUp(0)) // Check for left mouse button click
            {
                // delay method by one frame (in case of clicking a button that needs the value
                CoroutineManager.DelayMethod(() => { CurrentBuildDisplayUI.SetCurrentSelectedItem(null); } );
            }
        }

        #endregion


        #region GUI Manipulators

        void RefreshButtonsActivations(Enum collectable)
        {
            if (collectable == null || collectable is ECharacter)
            {
                m_CharacterPreviewSection.LockRuneButtons(new List<ERuneActivation>());
                CurrentBuildDisplayUI.Instance.LockSpellButtons(false);
                return;
            }

            if (collectable is ESpell spell)
            {
                CurrentBuildDisplayUI.Instance.LockSpellButtons(false);
                m_CharacterPreviewSection.LockRuneButtons(new List<ERuneActivation>() { ERuneActivation.Minor, ERuneActivation.Major, ERuneActivation.Primal});
                return;
            }

            if (collectable is ERune rune)
            {
                CurrentBuildDisplayUI.Instance.LockSpellButtons();
                m_CharacterPreviewSection.LockRuneButtons(SpellLoader.GetRuneData(rune).GetNotAllowedActivations());
            }
        }

        #endregion


    }
}