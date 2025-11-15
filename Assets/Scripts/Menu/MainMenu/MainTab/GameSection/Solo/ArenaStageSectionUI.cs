using Data.GameManagement;
using Enums;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu.MainTab
{
    public class ArenaStageSectionUI : StageSectionUI
    {
        #region Members

        [SerializeField, Tooltip("GameObject used to display extra lifes at stage")]
        GameObject m_ExtraLife;

        protected ArenaData m_ArenaData;

        #endregion


        #region Init & End

        public virtual void Initialize(int level, ArenaData arenaData)
        {
            // if sup to max level : do not display anything
            if (level > arenaData.MaxLevel)
                return;

            m_ArenaData = arenaData;

            base.Initialize(level, m_ArenaData.CurrentLevel, m_ArenaData.CurrentStage, m_ArenaData.GetArenaLevelData(level).StageData.Count);
        }

        #endregion


        #region Knobs

        protected override void ResetPathDisplay()
        {
            UIHelper.CleanContent(m_PathDisplayContainer);
            m_Knobs = new List<Image>();

            for (int i = 0; i < m_NStages; i++)
            {
                int extraLifes = CalculateExtraLifes(i);

                var knob = Instantiate(m_Knob, m_PathDisplayContainer.transform);
                var extraLifeContainer = Finder.Find(knob, "ExtraLifesContainer");
                if (extraLifes <= 0)
                {
                    extraLifeContainer.SetActive(false);
                } else
                {
                    extraLifeContainer.SetActive(true);
                    UIHelper.CleanContent(extraLifeContainer);

                    for (int iExtraLife = 0; iExtraLife < extraLifes; iExtraLife++)
                    {
                        Instantiate(m_ExtraLife, extraLifeContainer.transform);
                    }
                }

                m_Knobs.Add(knob.GetComponent<Image>());
                SetKnobColor(i);
            }
        }

        protected override void SetKnobColor(int index)
        {
            if (index < m_NStages - 1)
            {
                base.SetKnobColor(index);
                return;
            }

            m_Knobs[index].sprite = AssetLoader.LoadBossHead(m_ArenaData.GetBoss(m_Level).ToString());

            // Check Arena Level (for arena it is based on level only)
            if (m_Level != m_CurrentLevel)
                return;

            // Check index of CURRENT STAGE
            if (index != m_CurrentStage)
                return;

            var pulse = m_Knobs[index].gameObject.AddComponent<Pulse>();
            pulse.Initialize(CURRENT_STAGE_ANIMATION, -1f, 0.9f, 1.1f, pulseDuration: 1.5f, pauseDuration: 0f);
        }

        int CalculateExtraLifes(int stageIndex)
        {
            int baseExtraLifes = m_ArenaData.GetStageData(m_Level, stageIndex).ExtraLifes;
            if (baseExtraLifes <= 0)
                return 0;

            // Check Arena Level first
            if (m_Level < m_CurrentLevel)
            {
                return 0;
            }

            if (m_Level > m_CurrentLevel)
            {
                return baseExtraLifes;
            }

            // Check index of CURRENT STAGE
            if (stageIndex < m_CurrentStage)
            {
                return 0;
            }

            if (stageIndex > m_CurrentStage)
            {
                return baseExtraLifes;
            }

            return baseExtraLifes - ProgressionCloudData.CurrentArena.CurrentEnemyLifesLost;
        }

        #endregion


        #region Helpers

        protected override string GetLevelString()
        {
            if (m_ArenaData.ArenaType == EArenaType.EternalMenagerie)
                return "Phase " + TextHandler.ToRoman(m_Level + 1);
            return m_ArenaData.GetBoss(m_Level).ToString();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            //PlayerPrefsHandler.ArenaTypeChangedEvent            += OnArenaTypeChanged;
            ProgressionCloudData.CurrentArenaDataChangedEvent  += OnArenaDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            //PlayerPrefsHandler.ArenaTypeChangedEvent            -= OnArenaTypeChanged;
            ProgressionCloudData.CurrentArenaDataChangedEvent   -= OnArenaDataChanged;
        }

        void OnArenaTypeChanged(EArenaType arenaType)
        {
            ArenaData arenaData     = AssetLoader.LoadArenaData(arenaType);

            m_Level         = arenaData.CurrentLevel;
            m_CurrentLevel  = arenaData.CurrentLevel;
            m_CurrentStage  = arenaData.CurrentStage;

            RefreshUI();
        }

        void OnArenaDataChanged() 
        {
            RefreshUI();
        }

        #endregion
    }
}