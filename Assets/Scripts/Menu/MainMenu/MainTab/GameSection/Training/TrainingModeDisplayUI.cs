using Assets;
using Enums;
using Game.Spells;
using Menu.Common.Buttons;
using System;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu.MainTab.GameSection.Training
{
    public class TrainingModeDisplayUI : MObject
    {
        #region Members

        GameObject      m_CharacterPreviewContainer;
        Button          m_CharacterPreviewButton;
        GameObject      m_RunePreviewContainer;
        GameObject      m_BuildContainer;

        SynchronizedSlider m_DecisionRefreshSlider;
        SynchronizedSlider m_RandomnessSlider;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CharacterPreviewContainer     = Finder.Find(gameObject, "CharacterPreviewContainer");
            m_CharacterPreviewButton        = Finder.FindComponent<Button>(m_CharacterPreviewContainer);
            m_RunePreviewContainer          = Finder.Find(gameObject, "RunePreviewContainer");
            m_BuildContainer                = Finder.Find(gameObject, "BuildContainer");

            m_DecisionRefreshSlider         = Finder.FindComponent<SynchronizedSlider>(gameObject, "DecisionRefreshSlider");
            m_RandomnessSlider              = Finder.FindComponent<SynchronizedSlider>(gameObject, "RandomnessSlider");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // init sliders
            m_RandomnessSlider.Initialize("Randomness", PlayerPrefs.GetFloat(EPlayerPref.TrainingRandomness.ToString(), 0f), 0f, 1f);
            m_DecisionRefreshSlider.Initialize("Decision Refresh", PlayerPrefs.GetFloat(EPlayerPref.TrainingDecisionRefresh.ToString(), 0.05f), 0.05f, 1f);

            // refresh preview of rune and character
            CoroutineManager.DelayMethod(RefreshCharacterPreview);
            RefreshRunePreview();

            // create build items
            SetUpBuild();
        }

        #endregion


        #region GUI Manipulators

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
            UIHelper.SpawnCharacter(PlayerPrefsHandler.GetString<ECharacter>(EPlayerPref.TrainingCharacter), m_CharacterPreviewContainer);
        }

        void RefreshRunePreview()
        {
            UIHelper.CleanContent(m_RunePreviewContainer);

            for (int index = 0; index < 3; index++)
            {
                ERune rune = PlayerPrefsHandler.GetString<ERune>(EPlayerPref.TrainingRune, index);

                var runeItem = Instantiate(AssetLoader.LoadTemplateItem(rune), m_RunePreviewContainer.transform).GetComponent<TemplateRuneItemUI>();
                runeItem.Initialize(rune, asIconOnly: true);
                runeItem.Button.interactable = true;
                runeItem.Button.onClick.RemoveAllListeners();
                runeItem.Button.onClick.AddListener(() => Main.SetCollectableSelectionPopUp<ERune>(OnRuneSelectedCallback(runeItem, index), false));
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_CharacterPreviewButton.onClick.AddListener(() => Main.SetCollectableSelectionPopUp<ECharacter>(OnCharacterSelected, false));
            m_DecisionRefreshSlider.ValueChangedEvent   += OnDecisionRefreshValueChanged;
            m_RandomnessSlider.ValueChangedEvent        += OnRandomnessValueChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_CharacterPreviewButton.onClick.RemoveAllListeners();
            m_DecisionRefreshSlider.ValueChangedEvent   -= OnDecisionRefreshValueChanged;
            m_RandomnessSlider.ValueChangedEvent        -= OnRandomnessValueChanged;
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
                template.SetUpCollectable(rune, true);
                template.SetBottomOverlay("Level 9");
                template.Button.interactable = true;

                PlayerPrefsHandler.SetString(EPlayerPref.TrainingRune, rune.ToString(), index);
            };
        }

        Action<ESpell> OnSpellSelectedCallback(TemplateSpellItemUI template, int index)
        {
            return (ESpell spell) =>
            {
                template.SetUpCollectable(spell, true);
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