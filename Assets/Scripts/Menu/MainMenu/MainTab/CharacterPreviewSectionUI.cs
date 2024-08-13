using Menu.Common;
using Enums;
using Game.Loaders;
using Managers;
using Save;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using Inventory;
using System.Collections;
using Assets;
using Menu.Common.Buttons;
using System;

namespace Menu.MainMenu
{
    public class CharacterPreviewSectionUI : MonoBehaviour
    {
        #region Members

        bool m_Activated;

        GameObject              m_CharacterPreviewContainer;
        Button                  m_CharacterPreviewButton;
        RectTransform           m_CharacterPreviewRectTransform;
        TMP_Text                m_CharacterName; 
        CollectionFillBar       m_XpBar;
        TMP_Text                m_CharacterLevel;

        // -- left side
        GameObject              m_LeftSide;
        TemplateRuneItemUI      m_TemplateRuneButton;
        Button                  m_UpgradeSubButton;

        // -- spells container
        GameObject              m_CharacterSpellsContainer;
        TemplateSpellItemUI     m_UltimateButton;
        TemplateSpellItemUI     m_SpecialAbilityButton;
        TemplateSpellItemUI     m_AutoAttackButton;

        public Button CharacterPreviewButton => m_CharacterPreviewButton;

        #endregion


        #region Init & End

        public void Initialize()
        {
            m_CharacterPreviewContainer         = Finder.Find(gameObject, "CharacterPreviewContainer");
            m_CharacterPreviewButton            = Finder.FindComponent<Button>(m_CharacterPreviewContainer);
            m_CharacterPreviewRectTransform     = Finder.FindComponent<RectTransform>(m_CharacterPreviewContainer);
            m_CharacterName                     = Finder.FindComponent<TMP_Text>(gameObject, "CharacterName");
            m_XpBar                             = Finder.FindComponent<CollectionFillBar>(gameObject, "CharacterExperienceFillbar");
            m_CharacterLevel                    = Finder.FindComponent<TMP_Text>(m_XpBar.gameObject, "LevelValue");

            // init xp bar with current character cloud data
            m_XpBar.Initialize(InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter));

            // specials components 
            SetUpCharacterSpells();
            SetUpLeftSideUI();

            // register listeners
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  += OnSelectedCharacterChanged;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  += OnCurrentRuneChanged;
            CharacterBuildsCloudData.CurrentRuneChangedEvent        += OnCurrentRuneChanged;
            InventoryManager.CollectableUpgradedEvent               += OnCharacterLeveledUp;
            InventoryCloudData.CurrencyChangedEvent                 += OnCurrencyChanged;

            // setup ui of current selected character
            OnSelectedCharacterChanged();
        }

        public void Activate(bool activate)
        {
            m_Activated = activate;
        }

        private void OnDestroy()
        {
            m_CharacterPreviewButton.onClick.RemoveAllListeners();
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  -= OnSelectedCharacterChanged;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  -= OnCurrentRuneChanged;
            CharacterBuildsCloudData.CurrentRuneChangedEvent        -= OnCurrentRuneChanged;
            InventoryManager.CollectableUpgradedEvent               -= OnCharacterLeveledUp;
            InventoryCloudData.CurrencyChangedEvent                 -= OnCurrencyChanged;

            if (m_UpgradeSubButton != null)
                m_UpgradeSubButton.onClick.RemoveAllListeners();   
        }

        #endregion


        #region GUI Manipulators

        void SetUpCharacterSpells()
        {
            // -- spells container
            m_CharacterSpellsContainer = Finder.Find(gameObject, "CharacterSpellsContainer", false);       // can be null, if not set or inactive, it will not be used
            if (m_CharacterPreviewContainer != null)
            {
                m_UltimateButton        = Finder.FindComponent<TemplateSpellItemUI>(m_CharacterSpellsContainer, "UltimateSpellItem");
                m_SpecialAbilityButton  = Finder.FindComponent<TemplateSpellItemUI>(m_CharacterSpellsContainer, "SpecialAbilitySpellItem");
                m_AutoAttackButton      = Finder.FindComponent<TemplateSpellItemUI>(m_CharacterSpellsContainer, "AutoAttackSpellItem");
            }
        }

        void SetUpLeftSideUI()
        {
            m_LeftSide = Finder.Find(gameObject, "LeftSide", false);
            if (m_LeftSide == null)
                return;

            m_TemplateRuneButton = Finder.FindComponent<TemplateRuneItemUI>(m_LeftSide, throwError: false);
            m_UpgradeSubButton = Finder.FindComponent<Button>(m_LeftSide, "UpgradeSubButton", throwError: false);

            if (m_TemplateRuneButton != null)
            {
                m_TemplateRuneButton.Initialize(CharacterBuildsCloudData.CurrentRune);
                m_TemplateRuneButton.Button.onClick.AddListener(OnRuneIconButtonClicked);
            }

            if (m_UpgradeSubButton != null)
                m_UpgradeSubButton.onClick.AddListener(OnUpgradeButtonClicked);
        }

        /// <summary>
        /// Refresh icon of the rune to display current one
        /// </summary>
        void RefreshRuneIcon()
        {
            if (m_TemplateRuneButton == null)
                return;

            m_TemplateRuneButton.RefreshRune(CharacterBuildsCloudData.CurrentRune);
        }

        /// <summary>
        /// Update the name, level, xpbar of the currently selected character
        /// </summary>
        void UpdateCharInfos()
        {
            m_CharacterName.text = CharacterBuildsCloudData.SelectedCharacter.ToString();
            RefreshXpBarUI();
        }

        /// <summary>
        /// Display spells of the character (if container exists)
        /// </summary>
        void UpdateCharSpells()
        {
            // if does not exists or is not active : skip
            if (m_CharacterSpellsContainer == null || ! m_CharacterSpellsContainer.gameObject.activeInHierarchy)
                return;

            // get current character data
            var charData = CharacterLoader.GetCharacterData(CharacterBuildsCloudData.SelectedCharacter, destroy: true);

            // init ultimate button
            if (m_UltimateButton != null)
            {
                m_UltimateButton.gameObject.SetActive(true);
                m_UltimateButton.Initialize(charData.Ultimate);
            }

            // refresh auto attack
            if (m_SpecialAbilityButton != null)
            {
                m_SpecialAbilityButton.gameObject.SetActive(true);
                m_SpecialAbilityButton.Initialize(charData.SpecialAbility);
            }

            // refresh auto attack
            if (m_AutoAttackButton != null)
            {
                m_AutoAttackButton.gameObject.SetActive(true);
                m_AutoAttackButton.Initialize(charData.AutoAttack);
            }
        }

        void RefreshXpBarUI()
        {
            var charData = InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter);

            // update char level display
            m_CharacterLevel.text = charData.Level.ToString();

            // refresh xp bar with new xp and max required xp
            m_XpBar.RefreshCloudData(InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter));
        }

        /// <summary>
        /// Spawn character preview in the container
        /// </summary>
        void SpawnCharPreview()
        {
            UIHelper.SpawnCharacter(StaticPlayerData.Character, m_CharacterPreviewContainer);
        }

        #endregion


        #region Coroutines

        IEnumerator CharacterLevelUpCoroutine(ECharacter character)
        {
            // wait gain xp animation over
            while (m_XpBar.IsAnimated)
            {
                yield return null;
            }

            // display level up
            if (character == CharacterBuildsCloudData.SelectedCharacter)
                RefreshXpBarUI();
        }

        #endregion


        #region Listeners

        /// <summary>
        /// When a character is seleted :
        ///     - change info
        ///     - setup new Preview 
        /// </summary>
        /// <param name="character"></param>
        void OnSelectedCharacterChanged()
        {
            // update infos : name, xpbar, level, ...
            UpdateCharInfos();

            // update character spells
            UpdateCharSpells();
           
            // refresh rune icon of current build
            RefreshRuneIcon();

            // spawn preview
            SpawnCharPreview();
        }

        void OnCurrentRuneChanged()
        {
            if (m_TemplateRuneButton == null)
                return;

            RefreshRuneIcon();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="xp"></param>
        void OnCurrencyChanged(ECurrency currency, int xp)
        {
            if (currency != ECurrency.Xp)
                return;

            if (m_Activated && xp <= m_XpBar.CurrentCollection) 
            {
                RefreshXpBarUI();
                return;
            }

            if (m_Activated)
                m_XpBar.AddCollectionAnimation(xp - m_XpBar.CurrentCollection);
            else
                m_XpBar.Add(xp - m_XpBar.CurrentCollection);
        }

        /// <summary>
        /// 
        /// </summary>
        void OnCharacterLeveledUp(Enum character, int level)
        {
            if (character.GetType() != typeof(ECharacter))
                return;

            if (m_Activated)
                // wait for xp animation to end before displaying level up
                StartCoroutine(CharacterLevelUpCoroutine((ECharacter)character));
            else
                // instant refresh xp bar
                RefreshXpBarUI();
        }

        void OnRuneIconButtonClicked()
        {
            Main.SetPopUp(EPopUpState.RuneSelectionPopUp);
        }

        void OnUpgradeButtonClicked()
        {
            Main.SetPopUp(EPopUpState.CollectableInfoPopUp, CharacterBuildsCloudData.SelectedCharacter, InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level);
        }

        #endregion
    }
}