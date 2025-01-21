using Data.GameManagement;
using Enums;
using System;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;


namespace Game.UI.EndGameUI
{
    public class AnalyticDisplayUI : MObject
    {
        #region Members

        float m_AnimationDuration = 1f;

        // =====================================================================================
        // GameObjects & Components
        TMP_Text            m_Name;
        ExtansibleFillbar   m_Fillbar;
        TMP_Text            m_Value;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Name      = Finder.FindComponent<TMP_Text>(gameObject, "Name");
            m_Fillbar   = Finder.FindComponent<ExtansibleFillbar>(gameObject, "Fillbar");
            m_Value     = Finder.FindComponent<TMP_Text>(gameObject, "Value");
        }

        public void Initialize(EHitType hitType, int value, int maxValue, bool withAnimation = true)
        {
            base.Initialize();

            var color = Settings.GetHitTypeColor(hitType);

            m_Name.text     = hitType.ToString();
            m_Name.color    = color;
            m_Value.color   = color;

            if (! withAnimation)
            {
                m_Value.text = value.ToString();
            }
            else
            {
                StartCoroutine(PlayValueAnimation(0, value));
            }

            m_Fillbar.Initialize(value, maxValue, color, withAnimation);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region Animation

        public IEnumerator PlayValueAnimation(int fromValue, int toValue)
        {
            float timer = 0;
            while (timer <= m_AnimationDuration)
            {
                m_Value.text = Math.Round(fromValue + (toValue - fromValue) * timer / m_AnimationDuration).ToString();
                timer += Time.deltaTime;
                yield return null;
            }

            m_Value.text = toValue.ToString();
        }

        #endregion
    }
}