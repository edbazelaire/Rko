using Data.GameManagement;
using Enums;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class ArenaPathScreen : OverlayScreen
    {
        #region Members

        EArenaType m_ArenaType;
        ArenaData m_ArenaData;

        Image                   m_Background;
        GameObject              m_ScrollContent;
        GameObject              m_Viewport;
        ArenaStageDisplayUI     m_StageDisplayUIPrefab;

        List<ArenaStageDisplayUI> m_Stages;

        #endregion


        #region Init & End

        public void Initialize(EArenaType arenaType)
        {
            m_ArenaType = arenaType;
            m_ArenaData = AssetLoader.LoadArenaData(arenaType);

            base.Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_StageDisplayUIPrefab = AssetLoader.Load<ArenaStageDisplayUI>("ArenaStageDisplay", AssetLoader.c_UIPath + "OverlayScreens/Components/RewardsPath/ArenaPathContent/");

            m_Background = Finder.FindComponent<Image>(gameObject, "Background");
            m_ScrollContent = Finder.Find(gameObject, "ScrollContent");
            m_Viewport = Finder.Find(gameObject, "Viewport");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_Background.sprite = AssetLoader.Load<Sprite>(m_ArenaData.ArenaType.ToString(), AssetLoader.c_ArenaBackgroundsImagePath);
            SetupStagesDisplay();
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();
            CoroutineManager.DelayMethod(CenterOnCurrentStage, 2);
        }

        protected override void EnterAnimation()
        {
            var fadeIn = gameObject.AddComponent<Fade>();
            fadeIn.Initialize("", duration: 0.6f, startOpacity: 0);
        }

        #endregion


        #region GUI Manipulators

        void SetupStagesDisplay()
        {
            UIHelper.CleanContent(m_ScrollContent);
            m_Stages = new List<ArenaStageDisplayUI>();
            
            for (int i = 0; i < m_ArenaData.MaxLevel; i++)
            {
                var stageDisplayUI = Instantiate(m_StageDisplayUIPrefab, m_ScrollContent.transform);
                stageDisplayUI.Initialize(m_ArenaData, i, m_ArenaType);
                m_Stages.Add(stageDisplayUI);
            }
        }

        void CenterOnCurrentStage()
        {
            if (m_ArenaData.CurrentLevel == 0)
                return;

            int level = Math.Clamp(m_ArenaData.CurrentLevel, 0, m_Stages.Count - 1) ;
            
            // init position with current position
            float poseX = 0;

            // move stages until current stage level is left of viewport
            for (int i = 0; i < level; i++)
            {
                poseX -= Finder.FindComponent<RectTransform>(m_Stages[i].gameObject).rect.width;
            }

            // add half of viewport size
            poseX += (Finder.FindComponent<RectTransform>(m_Viewport).rect.width / 2);

            // remove half of current stage size
            poseX -= Finder.FindComponent<RectTransform>(m_Stages[level].gameObject).rect.width / 3;

            // poseX sup 0 means that not enought stages are behind to be able to center current stage
            if (poseX > 0)
            {
                return;
            }

            // setup position
            m_ScrollContent.transform.localPosition = new Vector3(poseX, m_ScrollContent.transform.localPosition.y, 0f);
        }

        #endregion
    }
}