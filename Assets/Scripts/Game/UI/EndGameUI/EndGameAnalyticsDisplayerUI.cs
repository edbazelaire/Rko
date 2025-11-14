using Assets.Scripts.Game;
using Enums;
using MyBox;
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

        List<EHitType> m_OrderSpellsBy;
        List<EHitType> m_DefaultOrderSpellsBy => new() { EHitType.PhysicalDamage, EHitType.MagicalDamage };
        EHitType[] m_HitTypeDisplayOrder => new EHitType[4] { EHitType.PhysicalDamage, EHitType.MagicalDamage, EHitType.Heal, EHitType.Shield };

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

            m_OrderSpellsBy = m_DefaultOrderSpellsBy;
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
                var values = m_SpellHitSummary.GetCategoryValuesSplitted(hitType);

                if (values.IsNullOrEmpty()) continue;

                var analyticDisplay = Instantiate(m_AnalyticsDisplayUIPrefab, m_SummaryContainer.transform);
                analyticDisplay.InitializeSplit(hitType, values, m_MaxValue, expectedHeight: 65);
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

        void OrderSpellAnalyticsBy(List<EHitType> hitTypes, bool desc = true)
        {
            if (m_SpellHitTypeDatas == null || m_SpellHitTypeDatas.Count == 0)
                return;

            if (hitTypes == null || hitTypes.Count == 0)
                hitTypes = m_DefaultOrderSpellsBy;

            m_SpellHitTypeDatas.Sort((a, b) =>
            {
                // Compute total sum for all given hit types
                int aSum = 0;
                int bSum = 0;

                foreach (var type in hitTypes)
                {
                    aSum += a.GetTotal(type);
                    bSum += b.GetTotal(type);
                }

                // Compare according to order (desc/asc)
                return desc ? bSum.CompareTo(aSum) : aSum.CompareTo(bSum);
            });
        }

        #endregion
    }
}
