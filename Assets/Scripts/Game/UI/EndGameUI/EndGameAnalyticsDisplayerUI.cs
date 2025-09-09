using Assets.Scripts.Game;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Game.UI.EndGameUI
{
    /// <summary>
    /// Main EndGame UI that shows analytics for the match.
    /// Displays summary (by HitType) and details (by spell).
    /// </summary>
    public class EndGameAnalyticsDisplayerUI : MObject
    {
        #region Members

        EHitType m_OrderSpellsBy => EHitType.Damage;
        EHitType[] m_HitTypeDisplayOrder => new EHitType[3] { EHitType.Damage, EHitType.Heal, EHitType.Shield };

        SSpellHitTypeData                   m_SpellHitSummary;
        Dictionary<ESpecialValue, float>    m_SpecialValues         = new();
        List<SSpellHitTypeData>             m_SpellHitTypeDatas     = new();
        int m_MaxValue;
        bool m_IsActive = false;

        AnalyticDisplayUI m_AnalyticsDisplayUIPrefab;
        GameObject m_SummaryContainer;
        GameObject m_DetailsContainer;

        public SSpellHitTypeData                    SpellHitSummary     => m_SpellHitSummary;
        public Dictionary<ESpecialValue, float>     SpecialValues       => m_SpecialValues;
        public List<SSpellHitTypeData>              SpellHitTypeDatas   => m_SpellHitTypeDatas;

        #endregion


        #region Init & End

        public void Initialize(List<SSpellHitTypeData> spellHitTypeDatas)
        {
            base.Initialize();

            m_SpellHitTypeDatas = spellHitTypeDatas;

            // calculate contextual values
            CalculateSummary();
            CalculateMaxValue();

            // make sure the display is deactivated
            Activate(false);
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_AnalyticsDisplayUIPrefab = AssetLoader.Load<AnalyticDisplayUI>(AssetLoader.c_MainUIComponentsPath);
            m_SummaryContainer = Finder.Find(gameObject, "SummaryContainer");
            m_DetailsContainer = Finder.Find(gameObject, "DetailsContainer");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            UIHelper.CleanContent(m_SummaryContainer);
            UIHelper.CleanContent(m_DetailsContainer);
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI()
        {
            if (m_SpellHitTypeDatas == null || m_SpellHitTypeDatas.Count == 0)
                return;

            // clean content
            UIHelper.CleanContent(m_SummaryContainer);
            UIHelper.CleanContent(m_DetailsContainer);

            // re-order by slected order
            OrderSpellAnalyticsBy(m_OrderSpellsBy);

            // delay display summary
            CoroutineManager.DelayMethod(RefreshDisplay);
        }

        public void RefreshDisplay()
        {
            DisplaySummary();
            DisplayDetails();
        }

        public void Activate(bool activate)
        {
            if (m_IsActive == activate)
                return;

            m_IsActive = activate;
            gameObject.SetActive(activate);

            if (!m_IsActive)
                return;

            RefreshUI();
        }

        /// <summary>
        /// Displays the summary bars (Damage/Heal/Shield).
        /// Splits Damage into Direct vs Tick.
        /// </summary>
        public void DisplaySummary()
        {
            UIHelper.CleanContent(m_SummaryContainer);

            foreach (EHitType hitType in m_HitTypeDisplayOrder)
            {
                int directValue = m_SpellHitSummary.GetCategoryValue(hitType, EHitCategory.Direct);
                int tickValue = m_SpellHitSummary.GetCategoryValue(hitType, EHitCategory.Tick);
                int total = directValue + tickValue;

                if (total == 0) continue;

                var analyticDisplay = Instantiate(m_AnalyticsDisplayUIPrefab, m_SummaryContainer.transform);
                analyticDisplay.InitializeSplit(hitType, directValue, tickValue, m_MaxValue, expectedHeight: 65);
            }

            foreach (var item in m_SpecialValues)
            {
                var analyticDisplay = Instantiate(m_AnalyticsDisplayUIPrefab, m_SummaryContainer.transform);
                analyticDisplay.Initialize(item.Key, (int)Math.Round(item.Value), m_MaxValue);
            }
        }

        /// <summary>
        /// Displays details for each spell separately.
        /// </summary>
        public void DisplayDetails()
        {
            UIHelper.CleanContent(m_DetailsContainer);
            SpellAnalyticsDisplayUI template = AssetLoader.Load<SpellAnalyticsDisplayUI>(AssetLoader.c_MainUIComponentsPath);

            foreach (var spellHitTypeData in m_SpellHitTypeDatas)
            {
                if (!spellHitTypeData.HitDetails.Any(temp => temp.Value > 0))
                    continue;

                SpellAnalyticsDisplayUI spellAnalyticDisplayUI = Instantiate(template, m_DetailsContainer.transform);
                spellAnalyticDisplayUI.Initialize(spellHitTypeData, m_MaxValue);
            }
        }

        #endregion


        #region Analytics

        public void UpdateAnalytics(List<SSpellHitTypeData> spellHitTypeDatas)
        {
            if (!m_Initialized)
                return;

            m_SpellHitTypeDatas = spellHitTypeDatas;
        }

        public void AddSpecialValue(ESpecialValue specialValue, float value)
        {
            if (m_SpecialValues == null)
                m_SpecialValues = new();

            m_SpecialValues[specialValue] = value;
        }

        void CalculateMaxValue()
        {
            m_MaxValue = 0;
            foreach (var hitType in m_HitTypeDisplayOrder)
            {
                m_MaxValue = Mathf.Max(m_MaxValue, m_SpellHitSummary.GetTotal(hitType));
            }
        }

        void CalculateSummary()
        {
            m_SpellHitSummary = new SSpellHitTypeData("Summary");
            foreach (var spellData in m_SpellHitTypeDatas)
            {
                foreach (var detail in spellData.HitDetails)
                {
                    m_SpellHitSummary.AddHit(detail.Value, detail.HitType, detail.Category);
                }
            }
        }

        void OrderSpellAnalyticsBy(EHitType hitType, bool desc = true)
        {
            m_SpellHitTypeDatas.Sort((a, b) =>
            {
                int aValue = a.GetTotal(hitType);
                int bValue = b.GetTotal(hitType);
                return desc ? bValue.CompareTo(aValue) : aValue.CompareTo(bValue);
            });
        }

        #endregion
    }
}
