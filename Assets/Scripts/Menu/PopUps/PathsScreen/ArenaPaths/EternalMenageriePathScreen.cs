using Enums;
using Game.GameManagers.ArenaModules;
using Game.Loaders;
using Menu.Common;
using Menu.Common.Infos;
using Save;
using System;
using System.Linq;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class EternalMenageriePathScreen : ArenaPathScreen
    {
        #region Members

        GameObject m_InfoSectionEM;
        CollectionFillBar m_HpBar;
        TemplateStateEffectIconFillUI m_CorruptedStacksUI;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_InfoSectionEM = Finder.Find(gameObject, "InfoSection_EM");
            m_HpBar = Finder.FindComponent<CollectionFillBar>(m_InfoSectionEM, "HpBar");
            m_CorruptedStacksUI = Finder.FindComponent<TemplateStateEffectIconFillUI>(m_InfoSectionEM, "StateEffectIcon");
        }

        #endregion


        #region GUI Manipulators

        protected override void RefreshUI()
        {
            base.RefreshUI();
            RefreshInfo_EM();
        }

        void RefreshInfo_EM()
        {
            if (! ProgressionCloudData.HasArenaInProgress || ProgressionCloudData.CurrentArena.IsOver())
            {
                m_InfoSectionEM.gameObject.SetActive(false);
                return;
            }

            m_InfoSectionEM.gameObject.SetActive(true);
            InitBossHp();
            InitCorruptionStacks();
        }

        protected override void SetupStagesDisplay()
        {
            base.SetupStagesDisplay();
        }

        void InitBossHp()
        {
            float bossHp = ProgressionCloudData.CurrentArena.GetMetaData<float>(EArenaMetadataKeys.BossHp.ToString());
            if (bossHp <= 0)
            {
                m_HpBar.InitializePercentage(1);
                return;
            }

            var stageData = m_ArenaData.GetBossStageData(m_ArenaData.CurrentLevel);
            int characterLevel = m_ArenaData.CurrentBaseCharacterLevel;
            var characterData = CharacterLoader.GetCharacterData(stageData.Boss.ToString(), characterLevel);
            float bossMaxHp = characterData.MaxHealth + (int)Math.Round(stageData.BonusStats.FirstOrDefault(t => t.StateEffectProperty == EStateEffectProperty.Hp).GetValue(characterLevel));
            m_HpBar.InitializePercentage(bossHp / bossMaxHp);
        }

        void InitCorruptionStacks()
        {
            m_CorruptedStacksUI.Initialize(EStateEffect.CorruptedPower, m_ArenaData.CurrentBaseCharacterLevel, ProgressionCloudData.CurrentArena.GetMetaData<int>(EArenaMetadataKeys.CorruptionStacks.ToString()), displayNextLevel: false);
        }

        #endregion


        #region Listeners

        #endregion


        #region Helpers

        protected override void LoadStageDisplayUIPrefab()
        {
            m_StageDisplayUIPrefab = AssetLoader.Load<EternalMenagerieStageDisplayUI>(AssetLoader.c_UIPath + "OverlayScreens/Components/RewardsPath/ArenaPathContent/");
        }

        #endregion
    }
}