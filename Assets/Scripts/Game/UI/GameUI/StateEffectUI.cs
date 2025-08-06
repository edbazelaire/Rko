using Enums;
using Game.Loaders;
using Game.Spells;
using Game.StateEffects.Quests;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class StateEffectUI : MonoBehaviour
    {
        Image           m_Icon;
        GameObject      m_StacksContainer;
        TMP_Text        m_StacksText;
        Image           m_TimerFill;

        string          m_StateEffectName;
        int             m_Stacks;
        int             m_MaxStacks;
        float           m_Duration;
        float           m_Timer;
        bool            m_IsHolding;

        #region Init & End

        void Awake()
        {
            m_Icon              = Finder.FindComponent<Image>(gameObject, "Icon");
            m_StacksContainer   = Finder.Find(gameObject, "StacksContainer");
            m_StacksText        = Finder.FindComponent<TMP_Text>(gameObject, "Stacks");
            m_TimerFill         = Finder.FindComponent<Image>(gameObject, "TimerFill");
        }

        public void Initialize(string stateEffect, int stacks, int maxStacks, float duration)
        {
            m_StateEffectName   = stateEffect;
            m_MaxStacks         = maxStacks;

            // Setup icon (if found)
            ReloadIcon();

            // setup stacks and duration
            Refresh(duration, stacks, maxStacks);
        }


        #endregion  

        // Update is called once per frame
        void Update()
        {
            if (m_Timer <= 0)
                return;

            if (m_IsHolding)
                return;

            m_Timer -= Time.deltaTime;
            m_TimerFill.fillAmount = Mathf.Clamp01(m_Timer / m_Duration);
        }


        #region GUI Manipulators

        public void Refresh(float duration, int stacks, int? maxStacks)
        {
            ErrorHandler.Log("Refresh " + m_StateEffectName + " : with " + stacks + " stacks", ELogTag.StateEffectGFX);

            m_Stacks = 0;
            AddStacks(stacks, maxStacks: maxStacks, duration: duration);
        }

        public void AddStacks(int stacks, int? maxStacks = null, float? duration = null)
        {
            if (maxStacks != null)
                m_MaxStacks = maxStacks.Value;

            m_Stacks = Math.Clamp(m_Stacks + stacks, 0, m_MaxStacks > 0 ? m_MaxStacks : 999);

            if (m_Stacks <= 1)
                m_StacksContainer.SetActive(false);
            else
            {
                m_StacksContainer.SetActive(true);
                m_StacksText.text = m_Stacks.ToString();
            }

            if (duration.HasValue)
                m_Duration = duration.Value;
            m_Timer = m_Duration;             // reset timer

            if (duration <= 0)
                m_TimerFill.fillAmount = 0;
        }

        public void ReloadIcon(Sprite icon = null)
        {
            if (icon == null)
                icon = AssetLoader.LoadStateEffectIcon(m_StateEffectName);

            m_Icon.sprite = icon;
        }

        public void SetIsHolding(bool isHolding)
        {
            m_IsHolding = isHolding;
        }

        #endregion
    }
}