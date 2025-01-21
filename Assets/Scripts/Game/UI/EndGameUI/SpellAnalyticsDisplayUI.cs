using Assets.Scripts.Game;
using Enums;
using MyBox;
using System.Collections;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.EndGameUI
{
    public class SpellAnalyticsDisplayUI : MObject
    {
        #region Members

        [SerializeField] float m_AnalyticsUIHeight = 35;
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

        void DisplayAnalytics()
        {
            // PREPARE : clean content and load template
            UIHelper.CleanContent(m_AnalyticsContainer);
            AnalyticDisplayUI template = AssetLoader.Load<AnalyticDisplayUI>(AssetLoader.c_MainUIComponentsPath);

            foreach (EHitType hitType in m_HitTypeDisplayOrder)
            {
                var data = m_SpellHitTypeData.HitTypeValues.ToList().FirstOrDefault(temp => temp.HitType == hitType);

                if (data.Value == 0)
                    continue;

                // instantiate object from template
                AnalyticDisplayUI analyticDisplayUI = Instantiate(template, m_AnalyticsContainer.transform);
                analyticDisplayUI.GetComponent<RectTransform>().SetHeight(m_AnalyticsUIHeight);

                // init analytics from provided data
                analyticDisplayUI.Initialize(data.HitType, data.Value, m_MaxValue);
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