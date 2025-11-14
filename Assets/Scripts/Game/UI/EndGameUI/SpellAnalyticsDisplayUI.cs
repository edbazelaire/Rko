using Assets.Scripts.Game;
using Enums;
using MyBox;
using TMPro;
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
        EHitType[] m_HitTypeDisplayOrder => new EHitType[4] { EHitType.PhysicalDamage, EHitType.MagicalDamage, EHitType.Heal, EHitType.Shield };

        // ====================================================================================
        // Data
        SSpellHitTypeData m_SpellHitTypeData;
        int m_MaxValue;

        // ====================================================================================
        // GameObjects & Components
        Image m_Icon;
        TMP_Text m_EffectName;
        TMP_Text m_EffectCounter;
        GameObject m_AnalyticsContainer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_EffectName            = Finder.FindComponent<TMP_Text>(gameObject, "EffectName");
            m_Icon                  = Finder.FindComponent<Image>(gameObject, "Icon");
            m_EffectCounter         = Finder.FindComponent<TMP_Text>(gameObject, "EffectCounter");
            m_AnalyticsContainer    = Finder.Find(gameObject, "AnalyticsContainer");
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
            SetupName();
            SetupCounter();
            DisplayAnalytics();
        }

        #endregion


        #region GUI Manipulators

        void SetupIcon()
        {
            m_Icon.sprite = AssetLoader.LoadIcon(m_SpellHitTypeData.SpellName);
        }

        void SetupName()
        {
            m_EffectName.text = TextHandler.Split(m_SpellHitTypeData.SpellName);
        }

        void SetupCounter()
        {
            m_EffectCounter.text = m_SpellHitTypeData.Counter.ToString();
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
                var values = m_SpellHitTypeData.GetCategoryValuesSplitted(hitType);

                if (values.IsNullOrEmpty())
                    continue;

                // Instantiate UI element
                AnalyticDisplayUI analyticDisplayUI = Instantiate(template, m_AnalyticsContainer.transform);
                analyticDisplayUI.InitializeSplit(hitType, values, m_MaxValue);
            }
        }

        #endregion
    }
}
