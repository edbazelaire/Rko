using Data;
using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class RuneInfoPopUp : CollectableInfoPopUp
    {
        #region Members

        // =========================================================================================
        // GameObjects & Components
        RuneInfosTabManager     m_RuneInfoTabManager;
        TMP_Text                m_MinorRuneDescription;
        TMP_Text                m_MajorRuneDescription;
        TMP_Text                m_PrimalRuneDescription;
        GameObject              m_SpawnsSection;
        GameObject              m_SpawnsContainer;

        // =========================================================================================
        // Data
        ERuneActivation m_RuneActivation;

        // =========================================================================================
        // Dependent Members
        RuneData m_RuneData         => m_Data as RuneData;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_RuneInfoTabManager = Finder.FindComponent<RuneInfosTabManager>(gameObject);
            m_MinorRuneDescription = Finder.FindComponent<TMP_Text>(m_RuneInfoTabManager.gameObject, "MinorRune");
            m_MajorRuneDescription = Finder.FindComponent<TMP_Text>(m_RuneInfoTabManager.gameObject, "MajorRune");
            m_PrimalRuneDescription = Finder.FindComponent<TMP_Text>(m_RuneInfoTabManager.gameObject, "PrimalRune");

            m_SpawnsSection = Finder.Find(gameObject, "SpawnsSection");
            m_SpawnsContainer = Finder.Find(m_SpawnsSection, "SpawnsContainer");

            m_RuneInfoTabManager.TabSelectedEvent += OnTabSelected;
        }

        public void Initialize(Enum enumValue, int level, ERuneActivation runeActivation = ERuneActivation.None, bool infoOnly = false)
        {
            m_RuneActivation = runeActivation;
            base.Initialize(enumValue, level, infoOnly);
        }

        public void Initialize(RuneData data, ERuneActivation runeActivation, bool infoOnly = true)
        {
            m_RuneActivation = runeActivation;
            base.Initialize(data, infoOnly);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            CheckRuneActivations();

            m_RuneData.SetActivation(m_RuneActivation);
            m_RuneInfoTabManager.Initialize(m_RuneActivation);
        }

        #endregion


        #region UIManipulators

        void CheckRuneActivations()
        {
            ERuneActivation firstAllowedActivation = ERuneActivation.None;
            foreach (ERuneActivation runeActivation in Enum.GetValues(typeof(ERuneActivation))) 
            {
                if (runeActivation == ERuneActivation.None)
                    continue;

                if (! m_RuneData.HasActivationPower(runeActivation))
                {
                    m_RuneInfoTabManager.DeactivateTab(runeActivation);

                    // if deactivation target the requested activation - reset it to None
                    if (m_RuneActivation == runeActivation)
                        m_RuneActivation = ERuneActivation.None;

                    continue;
                }

                // get the first allowed activation rune (as default rune)
                if (firstAllowedActivation == ERuneActivation.None)
                    firstAllowedActivation = runeActivation;
            }

            if (firstAllowedActivation == ERuneActivation.None)
            {
                ErrorHandler.Error("No rune activation was allowed for Rune : " + m_RuneData.Name);
                return;
            }

            // if requested rune activation was deactivated, use the first available one
            if (m_RuneActivation == ERuneActivation.None)
                m_RuneActivation = firstAllowedActivation;
        }

        protected override void SetUpDescription()
        {
            if (m_RuneData.HasActivationPower(ERuneActivation.Minor))
                m_MinorRuneDescription.text = m_RuneData.GetDescription(ERuneActivation.Minor);

            if (m_RuneData.HasActivationPower(ERuneActivation.Major))
                m_MajorRuneDescription.text = m_RuneData.GetDescription(ERuneActivation.Major);

            if (m_RuneData.HasActivationPower(ERuneActivation.Primal))
                m_PrimalRuneDescription.text = m_RuneData.GetDescription(ERuneActivation.Primal);
        }

        #endregion


        #region Listeners

        void OnTabSelected(string tabName)
        {
            if (tabName == ERuneInfosTabs.MinorRune.ToString())
                m_RuneActivation = ERuneActivation.Minor;
            else if (tabName == ERuneInfosTabs.MajorRune.ToString())
                m_RuneActivation = ERuneActivation.Major;
            else if (tabName == ERuneInfosTabs.PrimalRune.ToString())
                m_RuneActivation = ERuneActivation.Primal;
            else
            {
                ErrorHandler.Warning("Unhandled case : " + tabName);
                return;
            }

            List<CharacterData> spawnsData = m_RuneData.GetSpawnsData(m_RuneActivation);
            if (spawnsData.IsNullOrEmpty())
            {
                m_SpawnsSection.SetActive(false);
                return;
            }

            m_SpawnsSection.SetActive(true);
            UIHelper.CleanContent(m_SpawnsContainer);
            foreach (CharacterData spawnerData in spawnsData)
            {
                UIHelper.AddSpawnIconDisplayer(spawnerData, m_SpawnsContainer.transform);
            }
        }

        #endregion
    }
}