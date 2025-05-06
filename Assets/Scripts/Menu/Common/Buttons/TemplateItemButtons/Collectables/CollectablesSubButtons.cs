using Assets;
using Data.GameManagement;
using Enums;
using Inventory;
using Menu.MainMenu;
using Save;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;


namespace Menu.Common.Buttons.TemplateItemButtons.Collectables
{
    public class CollectablesSubButtons : MObject
    {
        #region Members

        // ========================================================================================
        // GameObjects & Components
        TemplateCollectableItemUI m_CollectableItemUI;

        GameObject  m_SubButtons;
        Button      m_UseButton;
        Button      m_RemoveButton;
        Button      m_InfosButton;
        Button      m_UpgradeButton;
        Image       m_UpgradeButtonImage;
        TMP_Text    m_UpgradeButtonCostText;

        // ========================================================================================
        // Dependent Accessors
        bool IsInCurrentBuild => CharacterBuildsCloudData.IsInCurrentBuild(m_CollectableItemUI.Collectable);

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SubButtons            = gameObject;
            m_UseButton             = Finder.FindComponent<Button>(m_SubButtons, "UseSubButton");
            m_RemoveButton          = Finder.FindComponent<Button>(m_SubButtons, "RemoveSubButton");
            m_InfosButton           = Finder.FindComponent<Button>(m_SubButtons, "InfosSubButton");
            m_UpgradeButton         = Finder.FindComponent<Button>(m_SubButtons, "UpgradeSubButton");
            m_UpgradeButtonImage    = Finder.FindComponent<Image>(m_UpgradeButton.gameObject);
            m_UpgradeButtonCostText = Finder.FindComponent<TMP_Text>(m_UpgradeButton.gameObject, "CostText");

            // -- hide sub buttons by default
            m_SubButtons.SetActive(false);
        }

        public void Initialize(TemplateCollectableItemUI collectableItemUI)
        {
            m_CollectableItemUI = collectableItemUI;

            base.Initialize();
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Switch display between SubButtons and CollectionFillBar
        /// </summary>
        public void Toggle()
        {
            bool alreadyActivated = m_SubButtons.activeInHierarchy;
            m_SubButtons.SetActive(!alreadyActivated);

            RefreshSubButtonUI();

            if (!alreadyActivated)
            {
                CoroutineManager.DelayMethod(() => MainMenuManager.OnClickedEvent += CloseSubButtons);
            } 
            else
            {
                MainMenuManager.OnClickedEvent -= CloseSubButtons;
            }
        }

        /// <summary>
        /// Force closing sub buttons
        /// </summary>
        protected void CloseSubButtons()
        {
            if (m_SubButtons == null)
                return;

            m_SubButtons.SetActive(false);
            MainMenuManager.OnClickedEvent -= CloseSubButtons;
        }

        /// <summary>
        /// + Activate / Deactivate allowed sub buttons 
        /// + Handle states (interactable, color, ...) 
        /// + Refresh values (like cost) 
        /// </summary>
        protected void RefreshSubButtonUI()
        {
            // check active 
            if (!m_SubButtons.activeInHierarchy)
                return;

            // USE & REMOVE BUTTONS
            m_UseButton.gameObject.SetActive(!IsInCurrentBuild);            // USE BUTTON : NOT in current build and NOT a linked spell (cant be added or removed)
            m_RemoveButton.gameObject.SetActive(IsInCurrentBuild);          // REMOVE BUTTON : in current build and NOT a linked spell

            // UPGRADE / INFOS BUTTONS
            if (m_CollectableItemUI.State == EButtonState.Updatable)
            {
                // deactivate InfosButton
                m_InfosButton.gameObject.SetActive(false);

                // activate UpgradeButton
                m_UpgradeButton.gameObject.SetActive(true);
                int cost = CollectablesManagementData.GetLevelData(m_CollectableItemUI.Collectable, InventoryCloudData.Instance.GetCollectable(m_CollectableItemUI.Collectable).Level).RequiredGold;
                m_UpgradeButtonImage.color = InventoryManager.CanBuy(cost) ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                m_UpgradeButtonCostText.text = cost.ToString();
            }
            else
            {
                // activate InfosButton
                m_InfosButton.gameObject.SetActive(true);
                // deactivate UpgradeButton
                m_UpgradeButton.gameObject.SetActive(false);
            }

            // always hide REMOVE button for characters
            if (m_CollectableItemUI.Collectable.GetType() == typeof(ECharacter))
                m_RemoveButton.gameObject.SetActive(false);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_UseButton.onClick.AddListener(OnUseButtonClicked);
            m_RemoveButton.onClick.AddListener(OnRemoveButtonClicked);
            m_InfosButton.onClick.AddListener(OnInfosButtonClicked);
            m_UpgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (!m_Initialized)
                return;

            m_UseButton.onClick.RemoveAllListeners();
            m_RemoveButton.onClick.RemoveAllListeners();
            m_InfosButton.onClick.RemoveAllListeners();
            m_UpgradeButton.onClick.RemoveAllListeners();

            // in case it was registered
            MainMenuManager.OnClickedEvent -= CloseSubButtons;
        }

        /// <summary>
        /// When the use button is clicked : set spell to current build
        /// </summary>
        public void OnUseButtonClicked()
        {
            CloseSubButtons();

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ESpell))
            {
                // try to find empty slot
                if (CurrentBuildDisplayUI.UseFirstEmptySlot((ESpell)m_CollectableItemUI.Collectable))
                    return;

                // if no empty slot, set card as current selected
                CurrentBuildDisplayUI.SetCurrentSelectedItem(m_CollectableItemUI.Collectable);
                return;
            }

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ERune))
            {
                // if no empty slot, set card as current selected
                CurrentBuildDisplayUI.SetCurrentSelectedItem(m_CollectableItemUI.Collectable);
                return;
            }

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ECharacter))
            {
                // if no empty slot, set card as current selected
                CharacterBuildsCloudData.SetSelectedCharacter((ECharacter)m_CollectableItemUI.Collectable);
                return;
            }
        }

        /// <summary>
        /// When the remove button is clicked : remove spell from current build
        /// </summary>
        public void OnRemoveButtonClicked()
        {
            CloseSubButtons();

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ESpell))
            {
                // remove the spell from the current build display
                CurrentBuildDisplayUI.RemoveSpell((ESpell)m_CollectableItemUI.Collectable);
                return;
            }

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ERune))
            {
                if (! CharacterBuildsCloudData.TryGetRuneActivationInBuild((ERune)m_CollectableItemUI.Collectable, out ERuneActivation activation))
                {
                    ErrorHandler.Error("Try to remove rune " + m_CollectableItemUI.Collectable + " but this rune was not found in build");
                    return;
                }

                // remove the spell from the current build display
                CharacterBuildsCloudData.SetCurrentRune(ERune.None, activation);
                return;
            }
        }

        /// <summary>
        /// On clicking the "Info Button" : display info of the collectable
        /// </summary>
        public void OnInfosButtonClicked()
        {
            CloseSubButtons();

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ESpell))
            {
                // display info
                Main.SetPopUp(EPopUpState.SpellInfoPopUp, (ESpell)m_CollectableItemUI.Collectable, m_CollectableItemUI.CollectableCloudData.Level);
                return;
            }

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ERune))
            {
                // display info
                CharacterBuildsCloudData.TryGetRuneActivationInBuild((ERune)m_CollectableItemUI.Collectable, out ERuneActivation runeActivation);
                Main.SetPopUp(EPopUpState.RuneInfoPopUp, (ERune)m_CollectableItemUI.Collectable, m_CollectableItemUI.CollectableCloudData.Level, runeActivation);
                return;
            }

            if (m_CollectableItemUI.Collectable.GetType() == typeof(ECharacter))
            {
                // display info
                Main.SetPopUp(EPopUpState.CharacterInfoPopUp, (ECharacter)m_CollectableItemUI.Collectable, m_CollectableItemUI.CollectableCloudData.Level);
                return;
            }
        }

        /// <summary>
        /// On clicking the "Upgrade Button" : display info of the collectable
        /// </summary>
        public void OnUpgradeButtonClicked()
        {
            OnInfosButtonClicked();
        }

        #endregion
    }

}