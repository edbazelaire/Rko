using Data;
using Enums;
using Game.Loaders;
using Managers;
using Menu.Common.Buttons;
using MyBox;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Menu.PopUps.Components
{
    public class RandomCollectableSelector : MObject
    {
        #region Members

        // =====================================================================
        // GameObjects & Components
        GameObject m_ChoicesContainer;

        // =====================================================================
        // Local data
        ECollectableType    m_CollectableType;
        int                 m_NSelection;
        int                 m_NPropositions;
        List<TemplateCollectableItemUI> m_Templates;
        List<Enum>          m_Values;
        List<Enum>          m_Selection;

        public List<Enum> FinalSelection => m_Selection.Where((Enum collectable) => collectable != null && collectable.ToString() != "None").ToList();

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ChoicesContainer = Finder.Find(gameObject, "ChoicesContainer");
        }

        public virtual void Initialize(ECollectableType collectableType, int nSelection, int nPropositions)
        {
            m_CollectableType = collectableType;
            m_NSelection = nSelection;
            m_NPropositions = nPropositions;

            m_Templates = new();
            m_Values    = new();
            m_Selection = new();

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // create a list of values for the enum
            RefreshRandomValues();

            // display allowed choices
            DisplayChoices();
        }

        #endregion


        #region GUI Manipulators

        void DisplayChoices()
        {
            TemplateCollectableItemUI template = AssetLoader.LoadTemplateCollectableItem(m_CollectableType);
            m_Templates = new();
            m_Selection = new();
            UIHelper.CleanContent(m_ChoicesContainer);

            // display list of values
            for (int i = 0; i < m_Values.Count; i++)
            {
                // create template
                var templateItem = Instantiate(template, m_ChoicesContainer.transform);
                // init template with values
                templateItem.Initialize(m_Values[i], ProfileCloudData.AccountLevel, asIconOnly: true, removeListeners: true);
                templateItem.SetBottomOverlay(m_Values[i].ToString());
                templateItem.OverrideOnClickListener(() => { ToggleSelection(templateItem); });

                // add to list of templates
                m_Templates.Add(templateItem);
            }
        }

        void ToggleSelection(TemplateCollectableItemUI template)
        {
            if (m_Selection.Contains(template.Collectable))
            {
                Deselect(template);
            }
            else
            {
                Select(template);
            }
        }

        void Select(TemplateCollectableItemUI template)
        {
            int index = m_Selection.FirstIndex((Enum collectable) => collectable == null || collectable.ToString() == "None");
            if (index == -1 && m_Selection.Count >= m_NSelection)
                return;

            if (template is TemplateRuneItemUI templateRune)
            {
                ERuneActivation runeActivation = (ERuneActivation)(3 - (index < 0 ? m_Selection.Count : index));
                templateRune.SetRuneActivation(runeActivation);
            }

            if (index == -1)
                m_Selection.Add(template.Collectable);
            else
                m_Selection[index] = template.Collectable;

            template.SetSelected(true);
            RefreshAllSelections();
        }

        void Deselect(TemplateCollectableItemUI template)
        {
            if (template is TemplateRuneItemUI templateRune)
            {
                templateRune.SetRuneActivation(ERuneActivation.None);
            }

            template.SetSelected(false);
            int index = m_Selection.FirstIndex((Enum collectable) => template.Collectable.Equals(collectable));

            m_Selection[index] = null;
            RefreshAllSelections();
        }

        void RefreshAllSelections()
        {
            foreach (TemplateCollectableItemUI template in m_Templates)
            {
                bool isSelected = m_Selection.Contains(template.Collectable);
                template.SetSelected(isSelected);

                var buttonState = EButtonState.Normal;
                if (!isSelected && FinalSelection.Count == m_NSelection)
                    buttonState = EButtonState.Locked;
                    
                template.SetState(buttonState);
            }
        }

        #endregion


        #region Values & Selection

        public List<T> GetSelection<T>() where T : Enum
        {
            var selection = new List<T>();
            foreach (var value in FinalSelection)
            {
                selection.Add((T)value);
            }

            return selection;
        }

        /// <summary>
        /// Get the selection and check it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selection"></param>
        /// <returns></returns>
        public bool TryGetSelection<T>(out List<T> selection) where T : Enum
        {
            selection = GetSelection<T>();
            if (selection.Count != m_NSelection)
                return false;

            return true;
        }

        void RefreshRandomValues()
        {
            m_Values = new List<Enum>();

            var currentBuild = ProgressionCloudData.CurrentArena.BuildData;
            for (int i = 0; i < m_NPropositions; i++)
            {
                Enum value;
                do
                {
                    switch (m_CollectableType)
                    {
                        case ECollectableType.Spell:
                            value = SpellLoader.GetRandomSpell().Spell;
                            if (i < m_NSelection)
                            {
                                currentBuild.Spells[i] = (ESpell)value;
                                currentBuild.SpellLevels[i] = ProfileCloudData.AccountLevel;
                            }
                            break;

                        case ECollectableType.Rune:
                            value = SpellLoader.GetRandomRune().Rune;
                            if (i < m_NSelection)
                            {
                                currentBuild.Runes[i] = (ERune)value;
                                currentBuild.RuneLevels[i] = ProfileCloudData.AccountLevel;
                            }
                            break;

                        case ECollectableType.Character:
                            value = CharacterLoader.GetRandomCharacter().Character;
                            if (i < m_NSelection)
                            {
                                currentBuild.Character = value.ToString();
                                currentBuild.CharacterLevel = ProfileCloudData.AccountLevel;
                            }
                            break;

                        default:
                            ErrorHandler.Error("Unhandled case : " + m_CollectableType);
                            return;
                    }
                } while (m_Values.Contains(value));

                ProgressionCloudData.SetCurrentArenaBuild(currentBuild);
                m_Values.Add(value);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}

