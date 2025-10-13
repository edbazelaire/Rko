using Assets;
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
    public class LeaguesPathScreen : OverlayScreen
    {
        #region Members

        LeagueDisplayUI m_TemplateLeagueDisplayUI;

        GameObject              m_ScrollContent;
        GameObject              m_Viewport;

        Dictionary<ELeague, LeagueDisplayUI> m_Stages;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TemplateLeagueDisplayUI = AssetLoader.Load<LeagueDisplayUI>("LeagueDisplay", AssetLoader.c_UIPath + "OverlayScreens/Components/RewardsPath/LeaguePathContent/");

            m_ScrollContent = Finder.Find(gameObject, "ScrollContent");
            m_Viewport = Finder.Find(gameObject, "Viewport");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

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
            fadeIn.Initialize("", duration: 0.4f, startOpacity: 0);
        }

        #endregion


        #region GUI Manipulators

        void SetupStagesDisplay()
        {
            UIHelper.CleanContent(m_ScrollContent);
            m_Stages = new Dictionary<ELeague, LeagueDisplayUI>();
            
            foreach (SLeagueData leagueData in Main.LeagueDataConfig.LeagueDataList)
            {
                var leagueDisplayUI = Instantiate(m_TemplateLeagueDisplayUI, m_ScrollContent.transform);
                leagueDisplayUI.Initialize(leagueData.League);
                m_Stages.Add(leagueData.League, leagueDisplayUI);
            }

            // add last league as final display
            var leagueBanner = Instantiate(AssetLoader.Load<GameObject>("LeagueBanner", AssetLoader.c_UIPath + "OverlayScreens/Components/RewardsPath/LeaguePathContent/"), m_ScrollContent.transform);
            Finder.FindComponent<Image>(leagueBanner, "LeagueBannerIcon").sprite = AssetLoader.LoadLeagueBanner(Main.LeagueDataConfig.LeagueDataList[^1].League + 1);
        }

        void CenterOnCurrentStage()
        {
            // init position with current position
            float poseX = Finder.FindComponent<RectTransform>(m_Viewport).rect.width / 2;

            // move stages until current stage level is left of viewport
            foreach (var kvp in m_Stages)
            {
                // skip until finding current league
                if (kvp.Key < ProgressionCloudData.CurrentLeague)
                {
                    poseX -= Finder.FindComponent<RectTransform>(kvp.Value.gameObject).rect.width;
                    continue;
                }

                // for current league : get PosX from sub LeagueDisplayUI
                poseX += kvp.Value.GetOffsetX();
                break;
            }

            // poseX sup 0 means that not enought stages are behind to be able to center current stage
            if (poseX > 0)
            {
                poseX = 0;
            }

            // setup position
            m_ScrollContent.transform.localPosition = new Vector3(poseX, m_ScrollContent.transform.localPosition.y, 0f);
        }

        #endregion
    }
}