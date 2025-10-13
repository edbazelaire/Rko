using Assets;
using Assets.Scripts.Data.GameManagement;
using Assets.Scripts.Managers;
using Data.GameManagement;
using DevTools;
using Enums;
using Menu.Common.Displayers;
using Save;
using Save.Data.Progression.Structs;
using System;
using System.Collections.Generic;
using Tools;
using Tools.Animations;
using UnityEngine;

namespace Menu.PopUps
{
    public class ArenaPathScreen : OverlayScreen
    {
        #region Members

        protected EArenaType                m_ArenaType;
        protected ArenaData                 m_ArenaData;

        protected OrbRewardDisplayer        m_OrbRewardDisplayer;
        protected Canvas                    m_OverlayCanvas;
        protected GameObject                m_ScrollContent;
        protected GameObject                m_Viewport;
        protected ArenaStageDisplayUI       m_StageDisplayUIPrefab;
        protected ArenaInfoSidebar          m_ArenaInfoSidebar;

        // dev tools
        protected ArenaDevTool m_ArenaDevTool;

        List<ArenaStageDisplayUI> m_Stages;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            LoadStageDisplayUIPrefab();

            m_ArenaInfoSidebar = Finder.FindComponent<ArenaInfoSidebar>(gameObject, "ArenaInfoSidebar");

            m_OrbRewardDisplayer = Finder.FindComponent<OrbRewardDisplayer>(gameObject, "OrbRewardDisplayer");
            m_OverlayCanvas = Finder.FindComponent<Canvas>(gameObject, "OverlayCanvas");
            m_ScrollContent = Finder.Find(gameObject, "ScrollContent");
            m_Viewport = Finder.Find(gameObject, "Viewport");

            // devtools
            m_ArenaDevTool = Finder.FindComponent<ArenaDevTool>(gameObject, "ArenaDevTool");
        }

        public void Initialize(EArenaType arenaType, SArenaDifficulty arenaDifficulty)
        {
            m_ArenaType = arenaType;
            m_ArenaData = AssetLoader.LoadArenaData(arenaType, arenaDifficulty);

            base.Initialize();
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            // rescale canvas to be above the rest
            m_OverlayCanvas.sortingLayerName = "Overlay";
            m_OverlayCanvas.sortingOrder = ScreenManager.OrderInLayer + 100;

            // setup UI
            SetupOrbRewardDisplayer();
            SetupStagesDisplay();

            m_ArenaDevTool.Initialize();

            RefreshUI();
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

        protected virtual void RefreshUI()
        {
            SetupArenaInfoSidebar();
        }

        protected virtual void SetupOrbRewardDisplayer()
        {
            m_OrbRewardDisplayer.Initialize(ProgressionCloudData.CurrentArena.GetPowerOrb(), m_ArenaData.CalculateMaxOrbPower(), ProgressionCloudData.CurrentArena.GetBonusPowerOrb() + ArenaManagementData.BonusArenaDifficultyLevel * m_ArenaData.ArenaDifficultyLevel);
        }

        protected virtual void SetupArenaInfoSidebar()
        {
            if (ProgressionCloudData.HasArenaInProgress)
            {
                m_ArenaInfoSidebar.Initialize();
            }
            else
            {
                m_ArenaInfoSidebar.gameObject.SetActive(false);
            }
        }

        protected virtual void SetupStagesDisplay()
        {
            UIHelper.CleanContent(m_ScrollContent);
            m_Stages = new List<ArenaStageDisplayUI>();
            
            for (int i = 0; i < m_ArenaData.MaxLevel + 1; i++)
            {
                var stageDisplayUI = Instantiate(m_StageDisplayUIPrefab, m_ScrollContent.transform);
                stageDisplayUI.Initialize(m_ArenaData, i, m_ArenaType);
                m_Stages.Add(stageDisplayUI);
            }
        }

        protected virtual void CenterOnCurrentStage()
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


        #region Helpers

        protected virtual void LoadStageDisplayUIPrefab()
        {
            m_StageDisplayUIPrefab = AssetLoader.Load<ArenaStageDisplayUI>("ArenaStageDisplay", AssetLoader.c_UIPath + "OverlayScreens/Components/RewardsPath/ArenaPathContent/");
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            if (ProgressionCloudData.HasArenaInProgress)
                m_ArenaInfoSidebar.AbandonButton.onClick.AddListener(OnAbandonButtonClicked);
            ProgressionCloudData.CurrentArenaDataChangedEvent += OnCurrentArenaDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (ProgressionCloudData.HasArenaInProgress)
                m_ArenaInfoSidebar.AbandonButton.onClick.RemoveListener(OnAbandonButtonClicked);
            ProgressionCloudData.CurrentArenaDataChangedEvent -= OnCurrentArenaDataChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void OnCurrentArenaDataChanged()
        {
            // Refresh the interface with the new data
            RefreshUI();
        }

        protected virtual void OnAbandonButtonClicked()
        {
            Main.ConfirmPopUp(
                message: "Do you really want to end this game ? All progression will be lost.\nYour rewards earned so far will still be granted",
                title: "",
                onValidate: OnAbandonValidated,
                onCancel: null
            );
        }

        protected virtual void OnAbandonValidated()
        {
            ProgressionCloudData.EndCurrentArena();
            Exit();
        }

        #endregion
    }
}