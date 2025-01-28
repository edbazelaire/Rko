using Enums;
using Game.Spells;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class StateEffectUI : MonoBehaviour
    {
        const string    c_Icon              = "Icon";
        const string    c_StacksContainer   = "StacksContainer";
        const string    c_Stacks            = "Stacks";
        const string    c_TimerFill         = "TimerFill";

        Image           m_Icon;
        GameObject      m_StacksContainer;
        TMP_Text        m_Stacks;
        Image           m_TimerFill;

        string          m_StateEffectName;
        float           m_Duration;
        float           m_Timer;
        bool            m_IsHolding;

        #region Init & End

        void Awake()
        {
            m_Icon              = Finder.FindComponent<Image>(gameObject, c_Icon);
            m_StacksContainer   = Finder.Find(gameObject, c_StacksContainer);
            m_Stacks            = Finder.FindComponent<TMP_Text>(gameObject, c_Stacks);
            m_TimerFill         = Finder.FindComponent<Image>(gameObject, c_TimerFill);
        }

        public void Initialize(string stateEffect, int stacks, float duration)
        {
            m_StateEffectName = stateEffect;

            // Setup icon (if found)
            ReloadIcon(stateEffect);

            // setup stacks and duration
            Refresh(duration, stacks);
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

        public void Refresh(float duration, int stacks)
        {
            ErrorHandler.Log("Refresh " + m_StateEffectName + " : with " + stacks + " stacks", ELogTag.Spells);

            // Setup stacks (if any)
            if (stacks <= 1)
                m_StacksContainer.SetActive(false);
            else
            {
                m_StacksContainer.SetActive(true);
                m_Stacks.text = stacks.ToString();
            }

            m_Duration = duration;
            m_Timer = duration;             // reset timer

            if (duration <= 0)
                m_TimerFill.fillAmount = 0;
        }

        public void ReloadIcon(string iconName)
        {
            Sprite icon = AssetLoader.LoadStateEffectIcon(iconName);
            if (icon != null)
                m_Icon.sprite = icon;
        }

        public void SetIsHolding(bool isHolding)
        {
            m_IsHolding = isHolding;
        }
    }
}