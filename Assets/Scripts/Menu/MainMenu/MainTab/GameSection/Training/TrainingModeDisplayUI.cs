using Assets;
using Enums;
using Game.AI.BehaviorTrees;
using Game.Spells;
using Menu.Common.Buttons;
using Save;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu.MainTab.GameSection.Training
{
    public class TrainingModeDisplayUI : MObject
    {
        #region Members

        Button                      m_OptionButton;

        GameObject                  m_CharacterSelectionContent;
        GameObject                  m_CharacterPreviewContainer;
        Button                      m_CharacterPreviewButton;
        GameObject                  m_RunePreviewContainer;
        GameObject                  m_BuildContainer;

        GameObject                  m_OptionsContent;
        GameObject                  m_ExtraVariablesContainer;
        TMP_Dropdown                m_DifficultyDropdown;
        SynchronizedSlider          m_DecisionRefreshSlider;
        SynchronizedSlider          m_RandomnessSlider;
        SynchronizedDoubleSlider    m_ReactionTimeSlider;
        SynchronizedDoubleSlider    m_MovementTimeSlider;
        SynchronizedDoubleSlider    m_MovementRefreshSlider;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_OptionButton                  = Finder.FindComponent<Button>(gameObject, "OptionButton");

            m_CharacterSelectionContent     = Finder.Find(gameObject, "CharacterSelectionContent");
            m_CharacterPreviewContainer     = Finder.Find(gameObject, "CharacterPreviewContainer");
            m_CharacterPreviewButton        = Finder.FindComponent<Button>(m_CharacterPreviewContainer);
            m_RunePreviewContainer          = Finder.Find(gameObject, "RunePreviewContainer");
            m_BuildContainer                = Finder.Find(gameObject, "BuildContainer");

            m_OptionsContent                = Finder.Find(gameObject, "OptionsContent");
            m_ExtraVariablesContainer       = Finder.Find(m_OptionsContent, "ExtraVariablesContainer");
            m_DifficultyDropdown            = Finder.FindComponent<TMP_Dropdown>(gameObject, "DifficultyDropdown");
            m_DecisionRefreshSlider         = Finder.FindComponent<SynchronizedSlider>(gameObject, "DecisionRefreshSlider");
            m_RandomnessSlider              = Finder.FindComponent<SynchronizedSlider>(gameObject, "RandomnessSlider");
            m_ReactionTimeSlider            = Finder.FindComponent<SynchronizedDoubleSlider>(gameObject, "ReactionTimeSlider");
            m_MovementTimeSlider            = Finder.FindComponent<SynchronizedDoubleSlider>(gameObject, "MovementTimeSlider");
            m_MovementRefreshSlider         = Finder.FindComponent<SynchronizedDoubleSlider>(gameObject, "MovementRefreshSlider");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // init dropdown
            if (! Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.TrainingDifficulty.ToString(), ELeague.Silver.ToString()), out ELeague league))
            {
                ErrorHandler.Error("Unable to parse Training league as league enum");
                league = ELeague.Silver;
            }
            UIHelper.SetUpDropdown(m_DifficultyDropdown, league, (ELeague value) => { PlayerPrefs.SetString(EPlayerPref.TrainingDifficulty.ToString(), value.ToString()); SetUpExtraVariablesSliders(); });

            // init sliders
            SetUpOptions();

            // refresh preview of rune and character
            CoroutineManager.DelayMethod(RefreshCharacterPreview);
            RefreshRunePreview();

            // create build items
            SetUpBuild();

            // hide options
            m_CharacterSelectionContent.SetActive(true);
            m_OptionsContent.SetActive(false);
            // -- hide option button for non admins
            m_OptionButton.gameObject.SetActive(ProfileCloudData.IsAdmin);
        }

        #endregion


        #region GUI Manipulators

        void SetUpOptions()
        {
            m_RandomnessSlider.Initialize("Randomness", EPlayerPref.TrainingRandomness.ToString(), 0.5f, 0f, 1f);
            m_DecisionRefreshSlider.Initialize("Decision Refresh", EPlayerPref.TrainingDecisionRefresh.ToString(), 0.1f, 0.05f, 1f);
            m_ReactionTimeSlider.Initialize("Reaction Time", EPlayerPref.TrainingReactionTime,
                baseMinValue: 0.05f,
                baseMaxValue: 0.2f,
                minValue: 0f,
                maxValue: 1f
            );
            m_MovementTimeSlider.Initialize("Movement Duration", EPlayerPref.TrainingMovementTime,
                baseMinValue: 0.5f,
                baseMaxValue: 2f,
                minValue: 0f,
                maxValue: 5f
            );
            m_MovementRefreshSlider.Initialize("Movement Refresh", EPlayerPref.TrainingMovementRefresh,
                baseMinValue: 0.05f,
                baseMaxValue: 0.5f,
                minValue: 0f,
                maxValue: 5f
            );

            SetUpExtraVariablesSliders();
        }

        void SetUpExtraVariablesSliders()
        {
            UIHelper.CleanContent(m_ExtraVariablesContainer);
            if (!Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.TrainingDifficulty.ToString()), out ELeague league))
            {
                ErrorHandler.Error("Unable to parse " + PlayerPrefs.GetString(EPlayerPref.TrainingDifficulty.ToString()) + " as League");
                return;
            }

            var allExtraVars = DefaultBotBT.GetExtraVariables(league);
            foreach (string extraVar in allExtraVars)
            {
                var slider = Instantiate(m_RandomnessSlider, m_ExtraVariablesContainer.transform);
                slider.Initialize(TextHandler.SplitCamelCase(extraVar), extraVar, 0f, -10f, 10f);
            }
        }

        void SetUpBuild()
        {
            UIHelper.CleanContent(m_BuildContainer);

            for (int i=0; i < 4; i++)
            {
                ESpell spell = PlayerPrefsHandler.GetString<ESpell>(EPlayerPref.TrainingSpell, i);
                int index = i;

                var template = Instantiate(AssetLoader.LoadTemplateItem(spell).GetComponent<TemplateSpellItemUI>(), m_BuildContainer.transform);
                template.Initialize(spell, asIconOnly: true);
                template.Button.interactable = true;
                template.Button.onClick.RemoveAllListeners();
                template.Button.onClick.AddListener(() => Main.SetCollectableSelectionPopUp<ESpell>(OnSpellSelectedCallback(template, index), false));

                template.SetBottomOverlay("Level 9");
            }
        }

        void RefreshCharacterPreview()
        {
            UIHelper.SpawnCharacter(PlayerPrefs.GetString(EPlayerPref.TrainingCharacter.ToString()), m_CharacterPreviewContainer);
        }

        void RefreshRunePreview()
        {
            UIHelper.CleanContent(m_RunePreviewContainer);

            for (int index = 0; index < 3; index++)
            {
                int runeIndex = index;
                ERune rune = PlayerPrefsHandler.GetString<ERune>(EPlayerPref.TrainingRune, index);

                var runeItem = Instantiate(AssetLoader.LoadTemplateItem(rune), m_RunePreviewContainer.transform).GetComponent<TemplateRuneItemUI>();
                runeItem.Initialize(rune, asIconOnly: true);
                runeItem.Button.interactable = true;
                runeItem.Button.onClick.RemoveAllListeners();
                runeItem.Button.onClick.AddListener(() => Main.SetCollectableSelectionPopUp<ERune>(OnRuneSelectedCallback(runeItem, runeIndex), false));
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_OptionButton.onClick.AddListener(ToggleOptions);
            m_CharacterPreviewButton.onClick.AddListener(() => Main.SetCollectableSelectionPopUp<ECharacter>(OnCharacterSelected, false));
            m_DecisionRefreshSlider.ValueChangedEvent   += OnDecisionRefreshValueChanged;
            m_RandomnessSlider.ValueChangedEvent        += OnRandomnessValueChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_OptionButton.onClick.RemoveAllListeners();
            m_CharacterPreviewButton.onClick.RemoveAllListeners();
            m_DecisionRefreshSlider.ValueChangedEvent   -= OnDecisionRefreshValueChanged;
            m_RandomnessSlider.ValueChangedEvent        -= OnRandomnessValueChanged;
        }

        void ToggleOptions()
        {
            m_CharacterSelectionContent.SetActive(m_OptionsContent.activeInHierarchy);
            m_OptionsContent.SetActive(!m_OptionsContent.activeInHierarchy);
        }

        void OnCharacterSelected(ECharacter character)
        {
            PlayerPrefs.SetString("TrainingCharacter", character.ToString());
            RefreshCharacterPreview();
        }

        Action<ERune> OnRuneSelectedCallback(TemplateRuneItemUI template, int index)
        {
            return (ERune rune) =>
            {
                template.SetUpCollectable(rune, 9, true);
                template.SetBottomOverlay("Level 9");
                template.Button.interactable = true;

                PlayerPrefsHandler.SetString(EPlayerPref.TrainingRune, rune.ToString(), index);
            };
        }

        Action<ESpell> OnSpellSelectedCallback(TemplateSpellItemUI template, int index)
        {
            return (ESpell spell) =>
            {
                template.SetUpCollectable(spell, 9, true);
                template.SetBottomOverlay("Level 9");
                template.Button.interactable = true;

                PlayerPrefsHandler.SetString(EPlayerPref.TrainingSpell, spell.ToString(), index);
            };
        }

        void OnDecisionRefreshValueChanged(float value)
        {
            PlayerPrefs.SetFloat(EPlayerPref.TrainingDecisionRefresh.ToString(), value);
            PlayerPrefs.Save();
        }

        void OnRandomnessValueChanged(float value)
        {
            PlayerPrefs.SetFloat(EPlayerPref.TrainingRandomness.ToString(), value);
            PlayerPrefs.Save();
        }

        #endregion
    }
}