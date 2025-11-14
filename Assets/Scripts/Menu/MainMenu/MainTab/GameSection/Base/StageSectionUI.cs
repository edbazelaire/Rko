using System.Collections.Generic;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu.MainTab
{
    public class StageSectionUI : MObject
    {
        #region Members

        protected const string CURRENT_STAGE_ANIMATION = "CURRENT_STAGE_ANIMATION";

        // ================================================================================
        // Serialized Data
        [SerializeField] protected GameObject   m_Knob;
        [SerializeField] protected GameObject   m_Line;

        [SerializeField] protected Sprite       m_KnobCompleted;
        [SerializeField] protected Sprite       m_KnobCurrent;
        [SerializeField] protected Sprite       m_KnobNotDone;

        // ================================================================================
        // GameObejcts & Components
        protected GameObject                    m_PathDisplayContainer;
        protected TMP_Text                      m_Title;
        protected List<Image>                   m_Knobs;

        // ================================================================================
        // Data
        protected int                           m_Level;
        protected int                           m_CurrentLevel;
        protected int                           m_CurrentStage;
        protected int                           m_NStages;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PathDisplayContainer = Finder.Find(gameObject, "PathDisplayContainer");
            m_Title = Finder.FindComponent<TMP_Text>(gameObject, "CurrentStageText", false);
        }

        public virtual void Initialize(int level, int currentLevel, int currentStage, int nStages)
        {
            base.Initialize();

            m_Level         = level;
            m_CurrentLevel  = currentLevel;
            m_CurrentStage  = currentStage;
            m_NStages       = nStages;

            RefreshUI();
        }

        #endregion


        #region GUI Manipulators

        protected virtual void RefreshUI()
        {
            RefreshTitle();
            ResetPathDisplay();
        }

        protected virtual void RefreshTitle()
        {
            if (m_Title == null)
                return;
            m_Title.text = GetLevelString();
        }

        protected virtual void ResetPathDisplay()
        {
            UIHelper.CleanContent(m_PathDisplayContainer);
            m_Knobs = new List<Image>();

            for (int i = 0; i < m_NStages; i++)
            {
                m_Knobs.Add(Instantiate(m_Knob, m_PathDisplayContainer.transform).GetComponent<Image>());
                SetKnobColor(i);
            }
        }

        protected virtual void SetKnobColor(int index)
        {
            // Check Arena Level first
            if (m_Level < m_CurrentLevel)
            {
                m_Knobs[index].sprite = m_KnobCompleted;
                return;
            }

            if (m_Level > m_CurrentLevel)
            {
                m_Knobs[index].sprite = m_KnobNotDone;
                return;
            }

            // Check index of CURRENT STAGE
            if (index < m_CurrentStage)
            {
                m_Knobs[index].sprite = m_KnobCompleted;
                return;
            }

            if (index > m_CurrentStage)
            {
                m_Knobs[index].sprite = m_KnobNotDone;
                return;
            }

            // DISPLAY as current
            m_Knobs[index].sprite = m_KnobCurrent;

            var pulse = m_Knobs[index].gameObject.AddComponent<Pulse>();
            pulse.Initialize(CURRENT_STAGE_ANIMATION, -1f, 0.9f, 1.1f, pulseDuration: 1.5f, pauseDuration: 0f);
        }

        #endregion


        #region Helpers 

        protected virtual string GetLevelString()
        {
            return "Stage " + (m_Level + 1).ToString();
        }

        #endregion
    }
}