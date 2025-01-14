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
        CharacterInfoButton     m_CharacterInfoButton;
        TMP_Text                m_CharacterName; 
        CollectionFillBar       m_XpBar;
        TMP_Text                m_CharacterLevelText;

        // -- left side
        GameObject              m_LeftSide;
        TemplateRuneItemUI      m_TemplateRuneButtonPrimal;
        TemplateRuneItemUI      m_TemplateRuneButtonMajor;
        TemplateRuneItemUI      m_TemplateRuneButtonMinor;
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
            m_CharacterInfoButton               = Finder.FindComponent<CharacterInfoButton>(gameObject, "CharacterInfoButton");
            m_CharacterName                     = Finder.FindComponent<TMP_Text>(gameObject, "CharacterName");
            m_XpBar                             = Finder.FindComponent<CollectionFillBar>(gameObject, "CharacterExperienceFillbar");
            m_CharacterLevelText                = Finder.FindComponent<TMP_Text>(m_XpBar.gameObject, "LevelValue");

            // init xp bar with current character cloud data
            m_XpBar.Initialize(InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter));
            // init CharacterInfoButton with current character cloud data
            m_CharacterInfoButton.Initialize(CharacterBuildsCloudData.SelectedCharacter);

            // specials components 
            SetUpCharacterSpells();
            SetUpLeftSideUI();

            // register listeners
            m_CharacterInfoButton.Button.onClick.AddListener(OnCharacterInfoButtonClicked);
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  += OnSelectedCharacterChanged;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  += OnCurrentRuneChanged;
            CharacterBuildsCloudData.CurrentRuneChangedEvent        += OnCurrentRuneChanged;
            ProfileCloudData.AccountLevelUpEvent                    += OnAccountLevelUp;
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
            m_CharacterInfoButton.Button.onClick.RemoveAllListeners();
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  -= OnSelectedCharacterChanged;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent  -= OnCurrentRuneChanged;
            CharacterBuildsCloudData.CurrentRuneChangedEvent        -= OnCurrentRuneChanged;
            ProfileCloudData.AccountLevelUpEvent                    -= OnAccountLevelUp;
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

            m_TemplateRuneButtonPrimal = Finder.FindComponent<TemplateRuneItemUI>(m_LeftSide, "TemplateRuneItemPrimal");
            m_TemplateRuneButtonMajor = Finder.FindComponent<TemplateRuneItemUI>(m_LeftSide, "TemplateRuneItemMajor");
            m_TemplateRuneButtonMinor = Finder.FindComponent<TemplateRuneItemUI>(m_LeftSide, "TemplateRuneItemMinor");
           
            RefreshRuneIcons();
        }

        /// <summary>
        /// Refresh icon of the rune to display current one
        /// </summary>
        void RefreshRuneIcons()
        {
            if (m_LeftSide == null)
                return;

            m_TemplateRuneButtonPrimal.Initialize(CharacterBuildsCloudData.CurrentRunes[0]);
            m_TemplateRuneButtonPrimal.SetIndex(0);

            m_TemplateRuneButtonMajor.Initialize(CharacterBuildsCloudData.CurrentRunes[1]);
            m_TemplateRuneButtonMajor.SetIndex(1);

            m_TemplateRuneButtonMinor.Initialize(CharacterBuildsCloudData.CurrentRunes[2]);
            m_TemplateRuneButtonMinor.SetIndex(2);
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
            m_CharacterLevelText.text = charData.Level.ToString();

            // refresh xp bar with new xp and max required xp
            m_XpBar.RefreshCloudData(InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter));
        }

        /// <summary>
        /// Spawn character preview in the container
        /// </summary>
        void SpawnCharPreview()
        {
            UIHelper.SpawnCharacter(StaticPlayerData.Character.ToString(), m_CharacterPreviewContainer);
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
            RefreshRuneIcons();

            // refresh the info button UI
            m_CharacterInfoButton.RefreshUI(CharacterBuildsCloudData.SelectedCharacter);

            // spawn preview
            SpawnCharPreview();
        }

        void OnCurrentRuneChanged()
        {
            if (m_TemplateRuneButtonPrimal == null)
                return;

            RefreshRuneIcons();
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

            // refresh the info button UI
            m_CharacterInfoButton.RefreshUI(CharacterBuildsCloudData.SelectedCharacter);

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

        void OnAccountLevelUp()
        {
            // refresh the info button UI
            m_CharacterInfoButton.RefreshUI(CharacterBuildsCloudData.SelectedCharacter);
            RefreshXpBarUI();
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

            // refresh the info button UI
            m_CharacterInfoButton.RefreshUI(CharacterBuildsCloudData.SelectedCharacter);
        }

        void OnCharacterInfoButtonClicked()
        {
            Main.SetPopUp(EPopUpState.CharacterInfoPopUp, CharacterBuildsCloudData.SelectedCharacter, InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level);
        }

        #endregion
    }
}