using Assets;
using Data.GameManagement;
using Enums;
using Inventory;
using Menu.Common.Buttons.TemplateItemButtons.Collectables;
using Menu.MainMenu;
using Save;
using System;
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
        public static Action<Enum> ButtonClickedEvent;
        public Action ThisButtonClickedEvent;

        // GameObjects & Components
        protected CollectablesSubButtons m_SubButtons = null;
        protected CollectionFillBar m_CollectionFillBar = null;
        protected SCollectableCloudData m_CollectableCloudData;

        protected Enum m_Collectable                        => m_CollectableCloudData.GetCollectable();
        protected int m_Level                               => m_CollectableCloudData.Level;
        public SCollectableCloudData CollectableCloudData   => m_CollectableCloudData;
        public CollectionFillBar CollectionFillBar          => m_CollectionFillBar;

        public Enum Collectable => m_Collectable;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SubButtons = Finder.FindComponent<CollectablesSubButtons>(gameObject, throwError: false);
            m_Border = Finder.FindComponent<Image>(gameObject, "IconContainer");
            m_CollectionFillBar = Finder.FindComponent<CollectionFillBar>(gameObject, throwError: false);
        }

        public virtual void Initialize(Enum collectable, bool asIconOnly = false)
        {
            base.Initialize();

            SetUpCollectable(collectable, asIconOnly);

            if (m_SubButtons != null && !asIconOnly) 
                m_SubButtons.Initialize(this);
        }

        #endregion


        #region GUI Manipulators

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
        }

        /// <summary>
        /// Refresh UI that could have changed with cloud data (level, State, ...)
        /// </summary>
        protected virtual void RefreshUI()
        {
            m_BottomText.text = string.Format(LEVEL_FORMAT, m_CollectableCloudData.Level);

            // check context of state and apply it
            UpdateState();
        }

        public virtual void SetUpCollectable(Enum collectable, bool asIconOnly = false)
        {
            // load cloud data of the collectable
            m_CollectableCloudData = InventoryCloudData.Instance.GetCollectable(collectable);

            // setup ui elements (icon, collection fillbar, ...)
            SetUpUI(asIconOnly);

            // remove extra features if this is only requested as icon
            AsIconOnly(asIconOnly);

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
                SetBottomOverlay(string.Format(LEVEL_FORMAT, m_CollectableCloudData.Level));
                m_Icon.color = Color.white;
            }
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

            if (m_CollectableCloudData.Level == 0)
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

        public override void AsIconOnly(bool activate = false)
        {
            base.AsIconOnly(activate);

            m_CollectionFillBar?.gameObject.SetActive(!activate);
        }

        /// <summary>
        /// Switch display between SubButtons and CollectionFillBar
        /// </summary>
        protected virtual void ToggleSubButtons()
        {
            if (m_SubButtons == null)
                return;
            
            m_SubButtons.Toggle();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            InventoryCloudData.CollectableDataChangedEvent  += OnCollectableDataChanged;
            InventoryManager.CollectableUpgradedEvent       += OnCollectableUpgraded;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

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
            RefreshUI();
        }

        protected virtual void OnCollectableUpgraded(Enum collectable, int level) { }

        protected virtual void OnClickLocked()
        {
            Main.ConfirmBuyCollectable(ShopManagementData.GetPrice(m_Collectable), m_Collectable, 1, OnPurchased);
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