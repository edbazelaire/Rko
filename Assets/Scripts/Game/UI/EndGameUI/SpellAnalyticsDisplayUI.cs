using Assets.Scripts.Game;
using Enums;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.EndGameUI
{
    /// <summary>
    /// Displays analytics for a single spell.
    /// Shows the icon and per-hitType analytics bars (Damage/Heal/Shield).
    /// Damage is split between Direct and Tick when applicable.
    /// </summary>
    public class SpellAnalyticsDisplayUI : MObject
    {
        #region Members
        EHitType[] m_HitTypeDisplayOrder => new EHitType[3] { EHitType.Damage, EHitType.Heal, EHitType.Shield };

        // ====================================================================================
        // Data
        SSpellHitTypeData m_SpellHitTypeData;
        int m_MaxValue;

        // ====================================================================================
        // GameObjects & Components
        Image m_Icon;
        GameObject m_AnalyticsContainer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
            m_AnalyticsContainer = Finder.Find(gameObject, "AnalyticsContainer");
        }

        /// <summary>
        /// Initialize spell analytics UI with data and max reference value.
        /// </summary>
        public void Initialize(SSpellHitTypeData spellHitTypeData, int maxValue)
        {
            m_SpellHitTypeData = spellHitTypeData;
            m_MaxValue = maxValue;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetupIcon();
            DisplayAnalytics();
        }

        #endregion


        #region GUI Manipulators

        void SetupIcon()
        {
            m_Icon.sprite = AssetLoader.LoadIcon(m_SpellHitTypeData.SpellName);
        }

        /// <summary>
        /// Displays analytics bars for each HitType (Damage/Heal/Shield).
        /// Damage bars are split between Direct and Tick.
        /// </summary>
        void DisplayAnalytics()
        {
            // Clean content and load template
            UIHelper.CleanContent(m_AnalyticsContainer);
            AnalyticDisplayUI template = AssetLoader.Load<AnalyticDisplayUI>(AssetLoader.c_MainUIComponentsPath);

            foreach (EHitType hitType in m_HitTypeDisplayOrder)
            {
                // Compute values
                int directValue = m_SpellHitTypeData.GetCategoryValue(hitType, EHitCategory.Direct);
                int tickValue = m_SpellHitTypeData.GetCategoryValue(hitType, EHitCategory.Tick);
                int total = directValue + tickValue;

                if (total == 0)
                    continue;

                // Instantiate UI element
                AnalyticDisplayUI analyticDisplayUI = Instantiate(template, m_AnalyticsContainer.transform);
                analyticDisplayUI.InitializeSplit(hitType, directValue, tickValue, m_MaxValue);
            }
        }

        #endregion
    }
}
