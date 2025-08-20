using Assets;
using Data.GameManagement;
using Enums;
using Game.Spells;
using Inventory;
using Menu.Common.Buttons.TemplateItemButtons.Collectables;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public enum EButtonState
    {
        Locked,
        Normal,
        Updatable
    }

    public class TemplateCollectableItemUI : TemplateItemButton
    {
        #region Members
        /// <summary> event that the button has been clicked </summary>
        public static Action<Enum>          ButtonClickedEvent;
        public Action                       ThisButtonClickedEvent;

        // GameObjects & Components
        protected CollectablesSubButtons    m_CSubButtons           = null;
        protected SubButtons                m_SubButtons            = null;
        protected CollectionFillBar         m_CollectionFillBar     = null;
        protected SCollectableCloudData     m_CollectableCloudData;
        protected HoldOnTrigger             m_HoldOnTrigger         = null;

        protected int m_Level;
        protected bool m_RemoveAllListeners = false;

        protected Enum m_Collectable                        => m_CollectableCloudData.GetCollectable();
        public SCollectableCloudData CollectableCloudData   => m_CollectableCloudData;
        public CollectionFillBar CollectionFillBar          => m_CollectionFillBar;
        public CollectablesSubButtons CSubButtons           => m_CSubButtons;
        public HoldOnTrigger HoldOnTrigger                  => m_HoldOnTrigger;

        public Enum Collectable => m_Collectable;
        public bool AsIconOnly => m_AsIconOnly;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Border                = Finder.FindComponent<Image>(gameObject, "IconContainer");
            m_CSubButtons           = Finder.FindComponent<CollectablesSubButtons>(gameObject,  throwError: false);
            m_CollectionFillBar     = Finder.FindComponent<CollectionFillBar>(gameObject,       throwError: false);
            m_HoldOnTrigger         = Finder.FindComponent<HoldOnTrigger>(gameObject,           throwError: false);
        }

        public virtual void Initialize(Enum collectable, bool asIconOnly = false)
        {
            m_AsIconOnly = asIconOnly;
            m_RemoveAllListeners = false;
            m_Level = 0;
            m_CollectableCloudData = GetCollectableCloudData(collectable);

            base.Initialize();

            ActivateHoldOnTrigger(false);
        }

        public virtual void Initialize(Enum collectable, int level, bool asIconOnly = false, bool removeListeners = true)
        {
            m_AsIconOnly            = asIconOnly;
            m_RemoveAllListeners    = removeListeners;
            m_Level                 = level;
            m_CollectableCloudData  = GetCollectableCloudData(collectable, level);

            base.Initialize();

            ActivateHoldOnTrigger(false);
        }

        protected override void OnInitialisationCompleted() 
        {
            SetUpCollectable(m_Collectable, m_Level, m_AsIconOnly);
        }


        protected virtual void SetLevel(int level)
        {
            if (level > 0)
            {
                m_Level = level;
                return;
            }

            m_Level = m_CollectableCloudData.Level;
        }

        /// <summary>
        /// Set UI elements that wont change even if cloud data is updating (icon, color, ...)
        /// </summary>
        /// <param name="asIconOnly"></param>
        protected virtual void SetUpUI(bool asIconOnly = false)
        {
            base.SetUpUI();

            SetIcon(AssetLoader.LoadIcon(m_Collectable));
            SetColor(CollectablesManagementData.GetRaretyData(m_Collectable).Color);
            SetUpCollectionFillBar(!asIconOnly);

            if (m_CSubButtons != null)
            {
                if (!asIconOnly)
                    m_CSubButtons.Initialize(this);
                m_CSubButtons.gameObject.SetActive(false);
            }
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Refresh UI that could have changed with cloud data (level, State, ...)
        /// </summary>
        protected virtual void RefreshUI()
        {
            m_BottomText.text = string.Format(LEVEL_FORMAT, m_Level);

            // check context of state and apply it
            UpdateState();
        }

        public virtual void SetUpCollectable(Enum collectable, int level, bool asIconOnly = false)
        {
            // load cloud data of the collectable
            m_CollectableCloudData = GetCollectableCloudData(collectable, level);
            SetLevel(level);

            // setup ui elements (icon, collection fillbar, ...)
            SetUpUI(asIconOnly);

            // remove extra features if this is only requested as icon
            SetAsIconOnly(asIconOnly);

            // setup ui depending on context
            RefreshUI();
        }

        public virtual void SetUpCollectionFillBar(bool activate)
        {
            if (m_CollectionFillBar == null)
                return;

            m_CollectionFillBar.gameObject.SetActive(activate);

            if (!activate)
                return;

            m_CollectionFillBar.Initialize(m_CollectableCloudData);
        }

        /// <summary>
        /// Set Icon as Mystery Icon ? 
        /// </summary>
        /// <param name="activate"></param>
        public virtual void SetMysteryIcon(bool activate)
        {
            if (activate)
            {
                SetIcon(AssetLoader.Load<Sprite>("MysteryIcon", "Sprites/UI/"));
                SetBottomOverlay("???");
                m_Icon.color = CollectablesManagementData.GetRaretyData(m_Collectable).Color;
            }

            else
            {
                SetIcon(AssetLoader.LoadIcon(m_Collectable));
                SetBottomOverlay(string.Format(LEVEL_FORMAT, m_Level));
                m_Icon.color = Color.white;
            }
        }

        public void ActivateHoldOnTrigger(bool activate)
        {
            if (m_HoldOnTrigger == null)
            {
                if (activate)
                {
                    ErrorHandler.Warning("Trying to activate HoldOnTrigger but the component is null");
                }
                return;
            }

            m_HoldOnTrigger.gameObject.SetActive(activate);
        }

        public void SetInteractable(bool interactable)
        {
            if (m_Button == null || m_Icon == null)
                return;

            m_Button.interactable = interactable;

            if (! interactable)
                m_Icon.color = new Color(0.3f, 0.3f, 0.3f);
            else
                m_Icon.color = new Color(1f, 1f, 1f);
        }

        #endregion


        #region State Manipulators

        /// <summary>
        /// Check context to define which state the button is in - set state accordingly
        /// </summary>
        protected virtual void UpdateState()
        {
            if (m_AsIconOnly)
            {
                SetState(EButtonState.Normal);
                return;
            }

            if (m_Level == 0)
            {
                SetState(EButtonState.Locked);
                return;
            }

            if (m_CollectableCloudData.IsUpgradable())
            {
                SetState(EButtonState.Updatable);
                return;
            }

            SetState(EButtonState.Normal);
        }

        public override void SetAsIconOnly(bool activate = false)
        {
            base.SetAsIconOnly(activate);

            m_CollectionFillBar?.gameObject.SetActive(!activate);
        }

        /// <summary>
        /// Switch display between SubButtons and CollectionFillBar
        /// </summary>
        public virtual void ToggleSubButtons()
        {
            if (m_SubButtons != null)
            {
                m_SubButtons.Toggle();
                return;
            }

            if (m_CSubButtons != null)
                m_CSubButtons.Toggle();
        }

        public void AddSubButtons(List<SubButton> subButtons)
        {
            if (m_CSubButtons != null)
            {
                Destroy(m_CSubButtons.gameObject);
            }

            if (m_SubButtons == null)
            {
                m_SubButtons = Instantiate(AssetLoader.Load<SubButtons>(AssetLoader.c_MainUISubButtonsPath), transform);
                m_SubButtons.Initialize();
            }

            m_SubButtons.SetButtons(subButtons);
            m_SubButtons.name = "NEW_SUB_BUTTONS";
            m_SubButtons.Hide();

            // Toggle subbuttons on main click
            m_Button.interactable = true;
            m_Button.onClick.AddListener(ToggleSubButtons);
        }


        #endregion


        #region Helpers

        SCollectableCloudData GetCollectableCloudData(Enum collectable, int level = 0)
        {
            if (level <= 0)
                return InventoryCloudData.Instance.GetCollectable(collectable);

            int qty = 0;
            if (! m_AsIconOnly)
                qty = InventoryCloudData.Instance.GetCollectable(collectable).Qty;

            return new SCollectableCloudData(collectable, level, qty);
        }

        public virtual void OpenInfoPopUp(int? level = null)
        {
            
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_RemoveAllListeners)
                return;

            InventoryCloudData.CollectableDataChangedEvent  += OnCollectableDataChanged;
            InventoryManager.CollectableUpgradedEvent       += OnCollectableUpgraded;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_RemoveAllListeners)
                return;

            InventoryCloudData.CollectableDataChangedEvent  -= OnCollectableDataChanged;
            InventoryManager.CollectableUpgradedEvent       -= OnCollectableUpgraded;
        }

        protected override void OnClick()
        {
            base.OnClick();

            // normal behavior
            switch (m_State)
            {
                case EButtonState.Locked:
                    OnClickLocked();
                    break;

                case EButtonState.Normal:
                case EButtonState.Updatable:
                    ToggleSubButtons();
                    break;
            }

            ButtonClickedEvent?.Invoke(m_Collectable);
        }

        protected virtual void OnCollectableDataChanged(SCollectableCloudData data)
        {
            if (!data.GetCollectable().Equals(m_Collectable))
                return;

            m_CollectableCloudData = data;
            m_Level = data.Level;
            RefreshUI();
        }

        protected virtual void OnCollectableUpgraded(Enum collectable, int level) 
        {
            if (collectable != m_Collectable)
                return;

            // refresh spell cloud data
            m_CollectableCloudData = InventoryCloudData.Instance.GetCollectable(collectable);
            m_Level = level;

            RefreshUI();
        }

        protected virtual void OnClickLocked() 
        {
            OpenInfoPopUp(0);
        }

        protected virtual void OnPurchased(bool success)
        {
            if (!success)
                return;

            // on success of purchase : unlock collectable
            InventoryManager.AddCollectable(m_Collectable, 1);

            // for character and runes -> set directly
            if (m_Collectable is ECharacter character)
                CharacterBuildsCloudData.SetSelectedCharacter(character);
        }

        #endregion
    }
}