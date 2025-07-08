using Assets.Scripts.Data.DataStructures.SpellRequirement;
using Assets.Scripts.Managers.Sound;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common.Buttons;
using Menu.Common.Displayers;
using Menu.Common.Infos;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class CollectableInfoPopUp : PopUp
    {
        #region Members

        const string UPGRADE_ANIMATION_ID = "UpgradeAnimation";

        // =========================================================================================
        // Data
        protected CollectableData                       m_Data;
        protected bool                                  m_InfoOnly;
        protected GameObject                            m_InfoPrefab;
        protected GameObject                            m_TemplateItemUI;
        protected GameObject                            m_TemplateInfoTitleSection;
        protected Dictionary<string, SpellInfoRowUI>    m_InfoRows;

        // =========================================================================================
        // GameObjects & Components
        protected Image                                 m_RaretyContainer;
        protected TMP_Text                              m_RaretyText;
        protected TemplateCollectableItemUI             m_CollectableItemUI;
        protected GameObject                            m_PreviewContainer;
        protected GameObject                            m_InfosSection;
        protected GameObject                            m_InfosContent;
        protected Button                                m_BuyButton;
        protected PriceDisplay                          m_PriceDisplay;
        protected Button                                m_UpgradeButton;
        protected TMP_Text                              m_CostText;

        // =========================================================================================
        // Dependent Members
        protected Enum m_Collectable                    => m_Data.Id;
        protected int m_Level                           => m_Data.Level;
        protected virtual bool m_IsUnlocked             => InventoryCloudData.Instance.IsUnlocked(m_Collectable);
        protected virtual bool m_IsMaxedLevel           => InventoryManager.IsMaxLevel(m_Collectable);
        protected virtual bool m_CanUpgrade             => InventoryManager.CanUpgrade(m_Collectable);
        protected virtual bool m_CanBuy                 => InventoryManager.CanBuy(m_Collectable);
        protected virtual SPriceData m_BuyPriceData     => ShopManagementData.GetPrice(m_Collectable);

        #endregion


        #region Init & End

        public void Initialize(Enum enumValue, int level, bool infoOnly = false)
        {
            SetupCollectable(enumValue, level);
            m_InfoOnly = infoOnly;

            base.Initialize();
        }

        public void Initialize(CollectableData collectableData, bool infoOnly = true)
        {
            m_Data = collectableData;
            m_InfoOnly = infoOnly;

            base.Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PreviewContainer      = Finder.Find(m_WindowContent, "PreviewContainer");
            m_InfosSection          = Finder.Find(m_WindowContent, "Infos", throwError: false);
            m_InfosContent          = Finder.Find(m_InfosSection, "InfosContent", throwError: false);

            m_BuyButton             = Finder.FindComponent<Button>(m_Buttons, "BuyButton", false);
            if (m_BuyButton != null)
                m_PriceDisplay      = Finder.FindComponent<PriceDisplay>(m_BuyButton.gameObject);

            m_UpgradeButton         = Finder.FindComponent<Button>(m_Buttons, "UpgradeSubButton", false);
            if (m_UpgradeButton != null)
                m_CostText              = Finder.FindComponent<TMP_Text>(m_UpgradeButton.gameObject, "CostText");

            m_RaretyContainer = Finder.FindComponent<Image>(m_TitleSection, "RaretyContainer", false);
            if (m_RaretyContainer != null)
                m_RaretyText = Finder.FindComponent<TMP_Text>(m_RaretyContainer.gameObject, "RaretyText", false);

            m_InfoPrefab = AssetLoader.Load<GameObject>("SpellInfoRow", AssetLoader.c_MainUIComponentsInfosPath);
            m_TemplateInfoTitleSection = AssetLoader.Load<GameObject>("InfoTitleSection", AssetLoader.c_MainUIComponentsInfosPath);

            // load data of the item
            m_TemplateItemUI = AssetLoader.LoadTemplateItem(m_Collectable);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            SetUpTitle();
            SetUpRarety();
            SetUpPreview();
            SetUpAllInfoRows();
            SetUpDescription();
            RefreshButtons();
        }

        public override void Exit()
        {
            AnimationHandler.EndAnimation(UPGRADE_ANIMATION_ID);
            Destroy(m_Data);

            base.Exit();
        }

        #endregion


        #region GUI Manipulators

        protected virtual void SetupCollectable(Enum enumValue, int level)
        {
            // destroy previous data
            if (m_Data != null)
                Destroy(m_Data);

            // load data of the item
            if (enumValue.GetType() == typeof(ECharacter))
                m_Data = CharacterLoader.GetCharacterData((ECharacter)enumValue, level, destroy: false);

            else if (enumValue.GetType() == typeof(ESpell))
                m_Data = SpellLoader.GetSpellData((ESpell)enumValue, level, destroy: false);

            else if (enumValue.GetType() == typeof(ERune))
                m_Data = SpellLoader.GetRuneData((ERune)enumValue, level, destroy: false);

            if (m_Data == null)
            {
                ErrorHandler.Error("Unable to retrieve collectable data : " + enumValue + " - of type : " + enumValue.GetType());
                Exit();                     // leave popup because of the Error
            }
        }

        protected override void AdjustAspectRatio()
        {
            base.AdjustAspectRatio();

            if (UIHelper.ScreenAspect == EScreenAspect.Square)
            {
                // get the RectTransform component of the GameObject
                RectTransform rectTransform = Finder.FindComponent<RectTransform>(m_PopUpWindow);

                // set the anchors to be at full length
                rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.y, rectTransform.anchorMin.y);
                rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.y, rectTransform.anchorMax.y);

                // remove delta size
                rectTransform.sizeDelta = Vector2.zero;
            }
        }

        protected virtual void SetUpTitle()
        {
            m_Title.text = TextLocalizer.SplitCamelCase(TextLocalizer.LocalizeText(m_Collectable.ToString()));
        }

        /// <summary>
        /// Instantiate Rarety box of the spell in TitleSection
        /// </summary>
        protected virtual void SetUpRarety()
        {
            if (m_RaretyContainer == null)
                return;

            var raretyData = SpellLoader.GetRaretyData(m_Data.Rarety);
            m_RaretyContainer.color = raretyData.Color;
            m_RaretyText.text = TextLocalizer.LocalizeText(raretyData.Rarety.ToString());
        }

        /// <summary>
        /// Instantiate preview of the spell
        /// </summary>
        protected virtual void SetUpPreview()
        {
            UIHelper.CleanContent(m_PreviewContainer);
            m_CollectableItemUI = Instantiate(m_TemplateItemUI, m_PreviewContainer.transform).GetComponent<TemplateCollectableItemUI>();
            m_CollectableItemUI.Initialize(m_Collectable, m_Level, asIconOnly: m_InfoOnly, removeListeners: m_InfoOnly);

            // deactivate button
            m_CollectableItemUI.Button.interactable = false;
        }

        /// <summary>
        /// Display infos of the spell
        /// </summary>
        protected virtual void SetUpAllInfoRows()
        {
            if (m_InfosContent == null)
                return;

            SetUpInfos(m_InfosContent, m_Data); 
        }

        protected virtual void SetUpInfos(GameObject container, CollectableData data, List<string> ignoredProperties = default)
        {
            if (container == null)
            {
                ErrorHandler.Error("Provided container is null");
                return;
            }

            // clean previous content
            UIHelper.CleanContent(container);
            m_InfoRows = new();

            // -- get new data if spell is updatable
            Dictionary<string, object> newDataInfos = null;
            if (!m_IsMaxedLevel)
                newDataInfos = data.Clone(m_Level + 1, true).GetInfo();

            var infos = data.GetInfo();
            foreach (var item in infos)
            {
                // check if key should be ignored
                if (!ignoredProperties.IsNullOrEmpty() && ignoredProperties.Contains(item.Key))
                    continue;

                m_Data.IsScalingProperty(item.Key, out EScalingDirection scaling);
                SetUpInfoRow(container, item.Key, item.Value, newDataInfos != null ? newDataInfos[item.Key] : null, scaling);
            }
        }

        /// <summary>
        /// Set up value of a singular Info row
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="newDataValue"></param>
        protected virtual void SetUpInfoRow(GameObject container, string key, object value, object newDataValue = null, EScalingDirection scaling = EScalingDirection.None)
        {
            if (key == "SpellRequirements")
            {
                if (newDataValue is not List<SpellRequirements> newSpellRequirements)
                    newSpellRequirements = null;

                if (value is List<SpellRequirements> spellRequirements)
                    SetupSpellRequirementsInfoRows(spellRequirements, newSpellRequirements);
                else
                    ErrorHandler.Warning("SpellRequirements was provided for " + m_Collectable.ToString() + " but unable to parse the value as SpellRequirements");

                return;
            }

            // spawn a spellRowInfo from prefab and init with spell data
            SpellInfoRowUI spellRowInfo = Instantiate(m_InfoPrefab, container.transform).GetComponent<SpellInfoRowUI>();
            spellRowInfo.Initialize(key, value, newDataValue, scaling);
            m_InfoRows.Add(key, spellRowInfo);
        }

        void SetupSpellRequirementsInfoRows(List<SpellRequirements> allSpellRequirements, List<SpellRequirements> newAllSpellRequirements = null)
        {
            for (int i = 0; i < allSpellRequirements.Count; i++)
            {
                for (int j = 0; j < allSpellRequirements[i].StateEffectRequirements.Count; j++)
                {
                    SetUpInfoRow(
                        m_InfosContent, 
                        allSpellRequirements[i].StateEffectRequirements[j].StateEffect,
                        allSpellRequirements[i].StateEffectRequirements[j].Stacks,
                        newAllSpellRequirements?[i].StateEffectRequirements[j].Stacks,
                        allSpellRequirements[i].StateEffectRequirements[j].ScalingDirection
                    );
                }
            }
        }

        #endregion


        #region Refresh UI

        protected virtual void RefreshUI()
        {
            SetUpAllInfoRows();
            SetUpDescription();
            RefreshButtons();
        }

        protected virtual void SetUpDescription() { }

        /// <summary>
        /// Set cost of the UpgradeButton + update UI to match the context
        /// </summary>
        protected virtual void RefreshButtons()
        {
            RefreshBuyButtonUI();
            RefreshUpgradeButtonUI();
        }

        /// <summary>
        /// Refresh all spell info rows with new value
        /// </summary>
        protected virtual void RefreshInfoRows()
        {
            // -- get new data if spell is updatable
            Dictionary<string, object> newSpelLDataInfos = null;
            if (!m_IsMaxedLevel)
            {
                var newSpell = m_Data.Clone(m_Level + 1);
                newSpelLDataInfos = newSpell.GetInfo();
                Destroy(newSpell);
            }

            foreach (var item in m_Data.GetInfo())
            {
                if (!m_InfoRows.ContainsKey(item.Key))
                    continue;

                // get spell row info
                var spellRowInfo = m_InfoRows[item.Key];
                spellRowInfo.RefreshValue(item.Value, newSpelLDataInfos != null ? newSpelLDataInfos[item.Key] : null);
            }
        }

        protected virtual void RefreshBuyButtonUI()
        {
            if (m_IsUnlocked || m_InfoOnly)
            {
                m_BuyButton.gameObject.SetActive(false);
                return;
            }

            m_BuyButton.gameObject.SetActive(true);
            m_BuyButton.interactable = m_CanBuy;
            m_PriceDisplay.Initialize(m_BuyPriceData);
        }

        /// <summary>
        /// Refresh UI to display if the UpgradeButton can be use or not
        /// </summary>
        protected virtual void RefreshUpgradeButtonUI()
        {
            if (m_UpgradeButton == null)
                return;

            if (! m_IsUnlocked)
            {
                m_UpgradeButton.gameObject.SetActive(false);
                return;
            }

            if (m_IsMaxedLevel || m_InfoOnly)
            {
                m_UpgradeButton.gameObject.SetActive(false);
                return;
            }

            if (m_IsMaxedLevel)
            {
                m_UpgradeButton.gameObject.SetActive(false);
                return;
            }

            m_UpgradeButton.gameObject.SetActive(true);
            m_UpgradeButton.interactable = m_CanUpgrade;
            m_CostText.text = CollectablesManagementData.GetLevelData(m_Collectable, m_Level).RequiredGold.ToString();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_InfoOnly)
                return;

            //InventoryManager.CollectableUpgradedEvent += OnLevelUp;
            InventoryCloudData.CollectableDataChangedEvent += OnCollectableDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_InfoOnly)
                return;

            //InventoryManager.CollectableUpgradedEvent -= OnLevelUp;
            InventoryCloudData.CollectableDataChangedEvent -= OnCollectableDataChanged;
        }

        protected override void OnUIButton(string bname)
        {
            switch (bname)
            {
                case "UpgradeSubButton":
                    OnUpgrade();
                    return;

                case "BuyButton":
                    OnBuy();
                    return;

                default:
                    base.OnUIButton(bname);
                    return;
            }
        }

        protected virtual void OnUpgrade()
        {
            if (!m_CanUpgrade)
                return;

            InventoryManager.Upgrade(m_Collectable);

            SoundFXManager.PlayOnce(SoundFXManager.LevelUpSoundFX);

            if (AnimationHandler.IsPlaying(UPGRADE_ANIMATION_ID))
                return;

            var pulse = m_CollectableItemUI.IconObject.AddComponent<Pulse>();
            pulse.Initialize(UPGRADE_ANIMATION_ID, duration: 2f, minSize: 0.9f, maxSize: 1.1f, pulseDuration: 0.5f, pauseDuration: 0f);

            var particles = m_CollectableItemUI.IconObject.AddComponent<ParticlesAnimation>();
            particles.Initialize(UPGRADE_ANIMATION_ID + "_2", duration: 2f);
        }

        /// <summary>
        /// When the "Buy" button is clicked
        /// </summary>
        protected virtual void OnBuy()
        {
            if (! m_CanBuy)
                return;

            // unlock the item
            InventoryCloudData.Instance.AddCollectableData(m_Collectable, unlock: true, save: true);

            // pay the cost
            var priceData = ShopManagementData.GetPrice(m_Collectable);
            InventoryManager.Spend(priceData, "Buying");

            // play animation
            SoundFXManager.PlayOnce(SoundFXManager.LevelUpSoundFX);

            if (AnimationHandler.IsPlaying(UPGRADE_ANIMATION_ID))
                return;

            var pulse = m_CollectableItemUI.IconObject.AddComponent<Pulse>();
            pulse.Initialize(UPGRADE_ANIMATION_ID, duration: 2f, minSize: 0.9f, maxSize: 1.1f, pulseDuration: 0.5f, pauseDuration:0f);

            var particles = m_CollectableItemUI.IconObject.AddComponent<ParticlesAnimation>();
            particles.Initialize(UPGRADE_ANIMATION_ID, duration: 2f, particlesName: "LevelUpAnimation", size: Vector2.one, layer: "Overlay");
        }

        /// <summary>
        /// When the value of this button's linked "SSpellCloudData" changes, reload it and apply changes to the UI
        /// </summary>
        /// <param name="spellCloudData"></param>
        protected virtual void OnLevelUp(Enum collectable, int level)
        {
            if (! collectable.Equals(m_Collectable))
                return;

            // reload data
            SetupCollectable(collectable, level);

            // refresh UI
            RefreshUI();
        }

        protected virtual void OnCollectableDataChanged(SCollectableCloudData collectableCloudData)
        {
            if (!collectableCloudData.CollectableName.Equals(m_Collectable.ToString()))
                return;

            // reload data
            SetupCollectable(collectableCloudData.GetCollectable(), collectableCloudData.Level);

            // refresh UI
            RefreshUI();
        }

        #endregion
    }
}