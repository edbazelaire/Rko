using Assets.Scripts.Managers.Sound;
using Data.GameManagement;
using Enums;
using Menu.Common.Displayers;
using System;
using System.Collections;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class QuickRewardMessagePopUp : QuickMessagePopUp
    {
        #region Members

        // ==========================================================================================
        // GameObjects & Components
        protected GameObject        m_RewardsSection;
        protected RewardsDisplayer  m_RewardsDisplayer;
        protected TMP_Text          m_RewardTitle;

        // Data
        SRewardsData m_RewardsData;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_RewardsSection    = Finder.Find(gameObject, "RewardsSection");
            m_RewardsDisplayer  = Finder.FindComponent<RewardsDisplayer>(m_RewardsSection);
            m_RewardTitle       = Finder.FindComponent<TMP_Text>(gameObject, "RewardTitle");
        }

        public virtual void Initialize(string message, SRewardsData rewardsData, float duration = 3f)
        {
            m_RewardsData = rewardsData;

            base.Initialize(message, duration);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_RewardsDisplayer.Initialize(m_RewardsData);
            m_RewardTitle.text = m_RewardsData.Rewards[0].GetPrettyName();
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();
        }

        protected override void OnExit()
        {
            base.OnExit();
        }

        #endregion


        #region Listeners


        #endregion
    }
}