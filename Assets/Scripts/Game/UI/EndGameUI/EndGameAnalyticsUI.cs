using Assets.Scripts.Game;
using Enums;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

namespace Game.UI.EndGameUI
{
    public class EndGameAnalyticsUI : MObject
    {
        #region Members

        EHitType m_OrderSpellsBy => EHitType.Damage;
        EHitType[] m_HitTypeDisplayOrder => new EHitType[3] { EHitType.Damage, EHitType.Heal, EHitType.Shield };

        // Data
        SSpellHitTypeData       m_SpellHitSummary;
        List<SSpellHitTypeData> m_SpellHitTypeDatas;
        int                     m_MaxValue;
        bool                    m_IsDisplayed = false;

        // GameObjects & Components
        AnalyticDisplayUI   m_AnalyticsDisplayUIPrefab;
        GameObject          m_SummaryContainer;
        GameObject          m_DetailsContainer;
        Button              m_CancelButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_AnalyticsDisplayUIPrefab = AssetLoader.Load<AnalyticDisplayUI>(AssetLoader.c_MainUIComponentsPath);
            m_SummaryContainer = Finder.Find(gameObject, "SummaryContainer");
            m_DetailsContainer = Finder.Find(gameObject, "DetailsContainer");
            m_CancelButton = Finder.FindComponent<Button>(gameObject, "CancelButton");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // clean content during setup
            UIHelper.CleanContent(m_SummaryContainer);
            UIHelper.CleanContent(m_DetailsContainer);
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI()
        {
            if (m_SpellHitTypeDatas == null || m_SpellHitTypeDatas.Count == 0)
                return;

            // calculate data before display
            CalulateSummary();      
            CalculateMaxValue();
            OrderSpellAnalyticsBy(m_OrderSpellsBy);

            // display data
            DisplaySummary();
            DisplayDetails();

            m_IsDisplayed = true;
        }

        public void ToggleDisplay()
        {
            gameObject.SetActive(! gameObject.activeInHierarchy);

            if (m_IsDisplayed || ! gameObject.activeInHierarchy)
                return;

            // first display : refresh UI with animation
            RefreshUI();
        }

        public void DisplaySummary()
        {
            UIHelper.CleanContent(m_SummaryContainer);

            // Display summary for each HitType
            foreach (EHitType hitType in m_HitTypeDisplayOrder)
            {
                var hitTypeValue = m_SpellHitSummary.HitTypeValues.ToList().FirstOrDefault(temp => temp.HitType == hitType);

                if (hitTypeValue.Value == 0) continue;

                var analyticDisplay = Instantiate(m_AnalyticsDisplayUIPrefab, m_SummaryContainer.transform);
                analyticDisplay.Initialize(hitTypeValue.HitType, hitTypeValue.Value, m_MaxValue);
            }
        }

        public void DisplayDetails()
        {
            UIHelper.CleanContent(m_DetailsContainer);
            SpellAnalyticsDisplayUI template = AssetLoader.Load<SpellAnalyticsDisplayUI>(AssetLoader.c_MainUIComponentsPath);

            foreach (var spellHitTypeData in m_SpellHitTypeDatas)
            {
                // filter data without any values
                if (!spellHitTypeData.HitTypeValues.Any(temp => temp.Value > 0))
                    continue;

                SpellAnalyticsDisplayUI spellAnalyticDisplayUI = Instantiate(template, m_DetailsContainer.transform);
                spellAnalyticDisplayUI.Initialize(spellHitTypeData, m_MaxValue);
            }
        }

        #endregion


        #region Analytics Management

        public void UpdateAnalytics(List<SSpellHitTypeData> spellHitTypeDatas)
        {
            if (!m_Initialized)
                return;

            m_SpellHitTypeDatas = spellHitTypeDatas;
        }


        #endregion


        #region Helpers

        void CalculateMaxValue()
        {
            // Calculate max value for HitType across all spells
            m_MaxValue = 0;
            foreach (var hitTypeValue in m_SpellHitSummary.HitTypeValues)
            {
                m_MaxValue = Mathf.Max(m_MaxValue, hitTypeValue.Value);
            }
        }

        void CalulateSummary()
        {
            // Calculate max value for HitType across all spells
            m_SpellHitSummary = new SSpellHitTypeData("Summary");
            foreach (var spellData in m_SpellHitTypeDatas)
            {
                foreach (var hitTypeValue in spellData.HitTypeValues)
                {
                    m_SpellHitSummary.AddHit(hitTypeValue.Value, hitTypeValue.HitType);
                }
            }
        }

        void OrderSpellAnalyticsBy(EHitType hitType, bool desc = true)
        {
            // Order the spell data based on the value of the specified hit type
            m_SpellHitTypeDatas.Sort((a, b) =>
            {
                // Find the value of the specified hit type in the first spell data
                int aValue = a.HitTypeValues.ToList()
                    .Find(hitTypeValue => hitTypeValue.HitType == hitType).Value;

                // Find the value of the specified hit type in the second spell data
                int bValue = b.HitTypeValues.ToList()
                    .Find(hitTypeValue => hitTypeValue.HitType == hitType).Value;

                // Compare values based on the descending flag
                return desc ? bValue.CompareTo(aValue) : aValue.CompareTo(bValue);
            });
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_CancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_CancelButton.onClick.RemoveAllListeners();
        }

        void OnCancelButtonClicked()
        {
            ToggleDisplay();
        }

        #endregion
    }
}
