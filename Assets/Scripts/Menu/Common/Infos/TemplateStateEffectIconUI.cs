using Assets;
using Data;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Infos
{
    public class TemplateStateEffectIconUI : MObject
    {
        #region Members

        Image               m_Icon;
        GameObject          m_StateEffectTextContainer;
        TMP_Text            m_StateEffectText;
        Button              m_Button;

        SStateEffectData    m_StateEffectData;
        int                 m_Level;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Icon                      = Finder.FindComponent<Image>(gameObject, "StateEffectIcon");
            m_StateEffectTextContainer  = Finder.Find(gameObject, "StateEffectTextContainer");
            m_StateEffectText           = Finder.FindComponent<TMP_Text>(gameObject, "StateEffectText");
            m_Button                    = Finder.FindComponent<Button>(gameObject);
        }

        public void Initialize(SStateEffectData stateEffectData, int level)
        {
            m_StateEffectData = stateEffectData;
            m_Level = level;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Icon.sprite = AssetLoader.LoadStateEffectIcon(m_StateEffectData.StateEffect.ToString());

            List<EStateEffect> statusEffect = new List<EStateEffect>() { EStateEffect.Slow, EStateEffect.Stun, EStateEffect.Silence };
            if (statusEffect.Contains(m_StateEffectData.StateEffect))
                DisplayDuration();
            else
                DisplayStacks();
        }

        #endregion


        #region GUI Manipulators

        void DisplayDuration()
        {
            float duration;
            if (m_StateEffectData.OverridingProperties.Any(value => value.StateEffectProperty.Equals(EStateEffectProperty.Duration)))
            {
                duration = m_StateEffectData.OverridingProperties.First(value => value.StateEffectProperty.Equals(EStateEffectProperty.Duration)).Value;
            }
            else
            {
                var stateEffect = SpellLoader.GetStateEffect(m_StateEffectData.StateEffect.ToString(), m_Level);
                duration = stateEffect.GetFloat(EStateEffectProperty.Duration);
                Destroy(stateEffect);
            }

            if (duration > 0)
            {
                m_StateEffectTextContainer.gameObject.SetActive(true);
                m_StateEffectText.text = duration.ToString("F2");
            }
            else
            {
                m_StateEffectTextContainer.gameObject.SetActive(false);
            }
        }

        void DisplayStacks()
        {
            if (m_StateEffectData.GetStacks() > 1)
            {
                m_StateEffectTextContainer.gameObject.SetActive(true);
                m_StateEffectText.text = m_StateEffectData.GetStacks().ToString();
            }
            else
            {
                m_StateEffectTextContainer.gameObject.SetActive(false);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Main.StateEffectPopUp(m_StateEffectData, m_Level);
        }

        #endregion

    }
}