using Assets;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common.Buttons;
using Save;
using System;
using System.Linq;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu
{
    public class TemplateSpellItemUI : TemplateSpellButton

    {
        #region Members

        // ========================================================================================
        // GameObjects & Components
        GameObject          m_SubButtons;
        Button              m_UseButton;
        Button              m_RemoveButton;
        Button              m_InfosButton;
        Button              m_UpgradeButton;
        Image               m_UpgradeButtonImage;
        TMP_Text            m_UpgradeButtonCostText;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SubButtons            = Finder.Find(gameObject, "SubButtons");
            m_UseButton             = Finder.FindComponent<Button>(m_SubButtons, "UseSubButton");
            m_RemoveButton          = Finder.FindComponent<Button>(m_SubButtons, "RemoveSubButton");
            m_InfosButton           = Finder.FindComponent<Button>(m_SubButtons, "InfosSubButton");
            m_UpgradeButton         = Finder.FindComponent<Button>(m_SubButtons, "UpgradeSubButton");
            m_UpgradeButtonImage    = Finder.FindComponent<Image>(m_UpgradeButton.gameObject);
            m_UpgradeButtonCostText = Finder.FindComponent<TMP_Text>(m_UpgradeButton.gameObject, "CostText");

            // -- hide sub buttons by default
            m_SubButtons.SetActive(false);
        }

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_UseButton.onClick.AddListener(OnUseButtonClicked);
            m_RemoveButton.onClick.AddListener(OnRemoveButtonClicked);
            m_InfosButton.onClick.AddListener(OnInfosButtonClicked);
            m_UpgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            InventoryCloudData.SpellDataChangedEvent    += OnCollectableDataChanged;
            TemplateSpellItemUI.ButtonClickedEvent      += OnAnyItemButtonClicked;
        }

        protected override void UnRegisterListeners()
        {
           if (! m_IsInitialized)
                return;

            base.UnRegisterListeners();

            m_UseButton.onClick.RemoveAllListeners();
            m_RemoveButton.onClick.RemoveAllListeners();
            m_InfosButton.onClick.RemoveAllListeners();
            m_UpgradeButton.onClick.RemoveAllListeners();

            InventoryCloudData.SpellDataChangedEvent    -= OnCollectableDataChanged;
            TemplateSpellItemUI.ButtonClickedEvent      -= OnAnyItemButtonClicked;
        }

        #endregion


        #region GUI Manipulators

        public override void AsIconOnly(bool activate = true)
        {
            base.AsIconOnly(activate);
        }

        public override void SetUpCollectionFillBar(bool activate = true)
        {
            base.SetUpCollectionFillBar(activate && ! SpellLoader.GetSpellData(Spell, destroy: true).Linked);    
        }

        /// <summary>
        /// Switch display between SubButtons and CollectionFillBar
        /// </summary>
        void ToggleSubButtons()
        {
            bool alreadyActivated = m_SubButtons.activeInHierarchy;
            m_SubButtons.SetActive(! alreadyActivated);

            RefreshSubButtonUI();
        }

        /// <summary>
        /// Force closing sub buttons
        /// </summary>
        void CloseSubButtons()
        {
            m_SubButtons.SetActive(false);
        }

        /// <summary>
        /// + Activate / Deactivate allowed sub buttons 
        /// + Handle states (interactable, color, ...) 
        /// + Refresh values (like cost) 
        /// </summary>
        void RefreshSubButtonUI()
        {
            // check active 
            if (!m_SubButtons.activeInHierarchy)
                return;

            // if linked : deactivate all sub buttons but Infos
            if (m_IsLinked)
            {
                m_UseButton.gameObject.SetActive(false);
                m_RemoveButton.gameObject.SetActive(false);
                m_UpgradeButton.gameObject.SetActive(false);
                m_InfosButton.gameObject.SetActive(true);
                return;
            }

            // USE & REMOVE BUTTONS
            m_UseButton.gameObject.SetActive(!IsInCurrentBuild);            // USE BUTTON : NOT in current build and NOT a linked spell (cant be added or removed)
            m_RemoveButton.gameObject.SetActive(IsInCurrentBuild);          // REMOVE BUTTON : in current build and NOT a linked spell

            // UPGRADE / INFOS BUTTONS
            if (m_State == EButtonState.Updatable)
            {
                // deactivate InfosButton
                m_InfosButton.gameObject.SetActive(false);

                // activate UpgradeButton
                m_UpgradeButton.gameObject.SetActive(true);
                int spellCost = SpellLoader.GetSpellLevelData(Spell).RequiredGolds;
                m_UpgradeButtonImage.color = InventoryManager.CanBuy(spellCost) ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                m_UpgradeButtonCostText.text = spellCost.ToString();
            } 
            else
            {
                // activate InfosButton
                m_InfosButton.gameObject.SetActive(true);
                // deactivate UpgradeButton
                m_UpgradeButton.gameObject.SetActive(false);
            }
        }

        #endregion


        #region State Management

        /// <summary>
        /// Check context to define which state the button is in - set state accordingly
        /// </summary>
        protected override void UpdateState()
        {
            // linked spell are always in "Normal" state
            if (SpellLoader.GetSpellData(Spell, destroy: true).Linked)
            {
                SetState(EButtonState.Normal);
                return;
            }

            base.UpdateState();
        }

        /// <summary>
        /// Set UI according to the provided state
        /// </summary>
        /// <param name="state"></param>
        public override void SetState(EButtonState state)
        {
            base.SetState(state);

            switch (state)
            {
                case (EButtonState.Locked):
                    SetBottomOverlay("");
                    break;

                case (EButtonState.Updatable):
                case (EButtonState.Normal):
                    SetBottomOverlay(string.Format(LEVEL_FORMAT, m_CollectableCloudData.Level));
                    break;

                default:
                    ErrorHandler.Error("UnHandled state " + state);
                    break;
            }
        }

        #endregion


        #region Listeners

        /// <summary>
        /// Action happening when the button is clicked on - depending on button context
        /// </summary>
        protected override void OnClick()
        {
            base.OnClick();
            
            // behavior when the USE button was clicked and this is one of the current build spells
            if (CurrentBuildDisplayUI.CurrentSelectedCard != null && CharacterBuildsCloudData.CurrentBuild.Contains(Spell))
            {
                CurrentBuildDisplayUI.ReplaceSpell(Spell);
                return;
            }

            // normal behavior
            switch (m_State)
            {
                case EButtonState.Locked:
                    break;

                case EButtonState.Normal:
                case EButtonState.Updatable:
                    if (m_IsLinked)
                        OnInfosButtonClicked();
                    else
                        ToggleSubButtons();

                    break;
            }
        }

        /// <summary>
        /// Do nothing on clicked while beeing locked
        /// </summary>
        protected override void OnClickLocked()
        {
            // TODO : lock message ?
            return;
        }

        /// <summary>
        /// When the use button is clicked : set spell to current build
        /// </summary>
        void OnUseButtonClicked()
        {
            CloseSubButtons();

            // try to find empty slot
            if (CurrentBuildDisplayUI.UseFirstEmptySlot(Spell))
                return;

            // if no empty slot, set card as current selected
            CurrentBuildDisplayUI.SetCurrentSelectedCard(Spell);
        }

        /// <summary>
        /// When the remove button is clicked : remove spell from current build
        /// </summary>
        void OnRemoveButtonClicked()
        {
            CloseSubButtons();

            // remove the spell from the current build display
            CurrentBuildDisplayUI.RemoveSpell(Spell);
        }

        /// <summary>
        /// On clicking the "Info Button" : display info of the spell
        /// </summary>
        void OnInfosButtonClicked()
        {
            CloseSubButtons();
            Main.SetPopUp(EPopUpState.SpellInfoPopUp, Spell, m_CollectableCloudData.Level);
        }

        /// <summary>
        /// On clicking the "Upgrade Button" : upgrade the spell's level and close sub buttons
        /// </summary>
        void OnUpgradeButtonClicked()
        {
            CloseSubButtons();
            Main.SetPopUp(EPopUpState.SpellInfoPopUp, Spell, m_CollectableCloudData.Level);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        protected override void OnCollectableUpgraded(Enum collectable, int level)
        {
            base.OnCollectableUpgraded(collectable, level);

            // for link spell, when a character is upgraded, the spell is too
            if (!m_IsLinked || collectable.GetType() != typeof(ECharacter))
                return;

            // this applies only for linked spell of the current selected character
            if (! CharacterBuildsCloudData.SelectedCharacter.Equals((ECharacter)collectable))
                return;

            // refresh spell cloud data
            m_CollectableCloudData = InventoryManager.GetSpellData(Spell);
            RefreshUI();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spell"></param>
        void OnAnyItemButtonClicked(Enum collectable)
        {
            if (! collectable.Equals(Spell)) 
                CloseSubButtons();
        }

        #endregion


        #region Dependent Members

        bool IsInCurrentBuild => CharacterBuildsCloudData.CurrentBuild.Contains((ESpell)m_CollectableCloudData.GetCollectable());

        #endregion
    }
}