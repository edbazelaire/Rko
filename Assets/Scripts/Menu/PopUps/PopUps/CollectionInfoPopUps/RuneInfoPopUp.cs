using Data;
using Enums;
using System;
using TMPro;
using Tools;

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
        }

        public void Initialize(Enum enumValue, int level, ERuneActivation runeActivation = ERuneActivation.None, bool infoOnly = false)
        {
            m_RuneActivation = runeActivation;
            base.Initialize(enumValue, level, infoOnly);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            SetUpDescription();
            m_RuneInfoTabManager.Initialize(m_RuneActivation);
        }

        #endregion


        #region UIManipulators

        protected override void SetupCollectable(Enum enumValue, int level)
        {
            base.SetupCollectable(enumValue, level);
            m_RuneData.SetActivation(m_RuneActivation);
        }

        void SetUpDescription()
        {
            m_MinorRuneDescription.text = m_RuneData.GetDescription(ERuneActivation.Minor);
            m_MajorRuneDescription.text = m_RuneData.GetDescription(ERuneActivation.Major);
            m_PrimalRuneDescription.text = m_RuneData.GetDescription(ERuneActivation.Primal);
        }

        #endregion
    }
}