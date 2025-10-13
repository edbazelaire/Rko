using Data.GameManagement;
using Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.EndGameUI
{
    /// <summary>
    /// UI element that displays analytics for one HitType (Damage/Heal/Shield).
    /// Supports both total and split display (Direct vs Tick).
    /// </summary>
    public class AnalyticDisplayUI : MObject
    {
        float m_AnimationDuration = 1f;

        TMP_Text            m_Name;
        ExtansibleFillbar   m_Fillbar;
        TMP_Text            m_Value;
        LayoutElement       m_LayoutElement;

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Name          = Finder.FindComponent<TMP_Text>(gameObject, "Name");
            m_Fillbar       = Finder.FindComponent<ExtansibleFillbar>(gameObject, "Fillbar");
            m_Value         = Finder.FindComponent<TMP_Text>(gameObject, "Value");
            m_LayoutElement = Finder.FindComponent<LayoutElement>(gameObject);
        }

        /// <summary>
        /// Initialize with a single value (legacy mode).
        /// </summary>
        public void Initialize(EHitType hitType, int value, int maxValue, bool withAnimation = true)
        {
            base.Initialize();

            var color = PlayerSettings.GetHitTypeColor(hitType, EHitCategory.Direct);

            m_Name.text = hitType.ToString();
            m_Name.color = color;
            m_Value.color = color;

            if (!withAnimation)
            {
                m_Value.text = value.ToString();
            }
            else
            {
                StartCoroutine(PlayValueAnimation(0, value));
            }

            m_Fillbar.Initialize(new() { (value, color) }, maxValue, withAnimation);
        }

        /// <summary>
        /// Initialize with a single value (legacy mode).
        /// </summary>
        public void Initialize(ESpecialValue specialValue, int value, int maxValue, bool withAnimation = true)
        {
            base.Initialize();

            var color = PlayerSettings.GetSpecialValueColor(specialValue);

            m_Name.text = TextHandler.SplitCamelCase(specialValue.ToString());
            m_Name.color = color;
            m_Value.color = color;

            if (!withAnimation)
            {
                m_Value.text = value.ToString();
            }
            else
            {
                StartCoroutine(PlayValueAnimation(0, value));
            }

            m_Fillbar.Initialize(new() { (value, color) }, maxValue, withAnimation);
        }

        /// <summary>
        /// Initialize with split values (Direct vs Tick).
        /// </summary>
        public void InitializeSplit(EHitType hitType, List<(int, Color)> values, int maxValue, int expectedHeight = 35, bool withAnimation = true)
        {
            base.Initialize();

            m_LayoutElement.preferredHeight = expectedHeight;

            int total = values.Sum(t => t.Item1);

            m_Name.text = hitType.ToString();
            m_Name.color = PlayerSettings.GetHitTypeColor(hitType, EHitCategory.Direct);
            m_Value.color = PlayerSettings.GetHitTypeColor(hitType, EHitCategory.Direct);
            m_Value.text = total.ToString();

            // Custom fillbar mode to show split
            m_Fillbar.Initialize(values, maxValue, withAnimation);
        }

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
    }
}
