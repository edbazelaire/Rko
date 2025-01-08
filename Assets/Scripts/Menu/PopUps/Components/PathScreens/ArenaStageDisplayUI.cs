using Data;
using Data.DataStructures;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons.TemplateItemButtons;
using Menu.MainMenu.MainTab;
using Save;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class ArenaStageDisplayUI : StageDisplayUI
    {
        #region Members
        
        ArenaData           m_ArenaData;
        SArenaLevelData     m_ArenaLevelData;
        int                 m_ArenaLevel;
        EArenaType          m_ArenaType;

        /// <summary> section containg all objects related to bonus effects </summary>
        GameObject          m_EffectsSection;
        /// <summary> layout container for bonus effects </summary>
        GameObject          m_EffectsContainer;
        /// <summary> display the boss of this stage </summary>
        BossPreviewDisplay  m_BossPreviewDisplay;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_EffectsSection                = Finder.Find(gameObject, "EffectsSection");
            m_EffectsContainer              = Finder.Find(gameObject, "EffectsContainer");
            m_BossPreviewDisplay            = Finder.FindComponent<BossPreviewDisplay>(gameObject, "BossPreviewDisplay");
        }

        public void Initialize(ArenaData arenaData, int arenaLevel, EArenaType arenaType)
        {
            m_ArenaData         = arenaData;
            m_ArenaLevelData    = arenaData.GetArenaLevelData(arenaLevel);
            m_ArenaLevel        = arenaLevel;
            m_ArenaType         = arenaType;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            ((ArenaStageSectionUI)m_StageSectionUI).Initialize(m_ArenaLevel, m_ArenaData);

            base.SetUpUI();

            SetUpEffects();
            SetUpBossPreview();
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI()
        {

        }

        void SetUpEffects()
        {
            // clean content (remove potential TEST displays)
            UIHelper.CleanContent(m_EffectsContainer);

            // no trigger effects : deactivate and return
            if (m_ArenaLevelData.TriggerEffects.Count == 0 && m_ArenaLevelData.PowerUps.Count == 0)
            {
                m_EffectsSection.SetActive(false);
                return;
            }

            // active section (by precaution)
            m_EffectsSection.SetActive(true);

            // load template of TriggerEffectUI
            TemplateTriggerEffectUI template = AssetLoader.LoadTemplateItem<TemplateTriggerEffectUI>();

            // add UI for each trigger effects
            foreach (STriggerEffect triggerEffect in m_ArenaLevelData.TriggerEffects)
            {
                TemplateTriggerEffectUI triggerEffectUI = Instantiate(template, m_EffectsContainer.transform);
                triggerEffectUI.Initialize(triggerEffect);
            }

            // add UI for each trigger effects
            TemplateRunePowerUI templateRune = AssetLoader.LoadTemplateItem<TemplateRunePowerUI>();
            foreach (string powerName in m_ArenaLevelData.PowerUps)
            {
                SRunePower runePower = SpellLoader.GetPowerUp(powerName);
                TemplateRunePowerUI runeItemUI = Instantiate(templateRune, m_EffectsContainer.transform);
                runeItemUI.Initialize(runePower);
            }
        }

        void SetUpBossPreview()
        {
            m_BossPreviewDisplay.Initialize(m_ArenaData.GetBoss(m_ArenaLevel), (int)m_ArenaData.ArenaDifficulty + 1);
        }

        #endregion


        #region State

        protected override void RefreshState()
        {
            if (ProgressionCloudData.IsArenaRewardCollected(m_ArenaType, m_ArenaData.SArenaDifficulty, m_ArenaLevel))
                SetState(EStageRewardState.Collected);
            else
                SetState(EStageRewardState.Unlocked);
            return;
        }

        protected override void SetUnlockedState()
        {
            m_OverlayScreen.SetActive(false);
            m_RewardDisplayerBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }

        #endregion


        #region Rewards

        protected override SRewardsData GetRewards() 
        {
            return m_ArenaLevelData.RewardsData;
        }

        #endregion
    }
}