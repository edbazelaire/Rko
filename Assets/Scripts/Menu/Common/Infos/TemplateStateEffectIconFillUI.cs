using Assets;
using Assets.Scripts.Managers;
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
    public class TemplateStateEffectIconFillUI : TemplateStateEffectIconUI
    {
        #region Members

        Image m_FillImage;
        int m_MaxStacks;
        bool m_DisplayNextLevel = true;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_FillImage = Finder.FindComponent<Image>(m_StateEffectTextContainer);
        }

        public void Initialize(EStateEffect stateEffect, int level, int nStacks, bool displayNextLevel = true)
        {
            m_MaxStacks = SpellLoader.GetStateEffect(stateEffect, level).MaxStacks;
            m_DisplayNextLevel = displayNextLevel;
            base.Initialize(new SStateEffectData(stateEffect, nStacks, level: level), level);
        }

        #endregion


        #region GUI Manipulators

        protected override void DisplayStacks()
        {
            base.DisplayStacks();

            if (m_MaxStacks <= 0)
            {
                m_FillImage.fillAmount = 0;
                return;
            }

            m_FillImage.fillAmount = m_StateEffectData.GetStacks(m_Level) / m_MaxStacks;
        }

        #endregion


        #region Listeners

        protected override void OnClick()
        {
            ScreenManager.StateEffectPopUp(m_StateEffectData, m_Level, m_StateEffectData.GetStacks(m_Level), m_DisplayNextLevel);
        }

        #endregion
    }
}