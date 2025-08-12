using Assets.Scripts.Managers;
using Data;
using Data.DataStructures;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons.TemplateItemButtons;
using Menu.MainMenu.MainTab;
using Save;
using System.Linq;
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
        /// <summary> layout container for runes effects </summary>
        GameObject          m_RunesContainer;
        /// <summary> display the boss of this stage </summary>
        BossPreviewDisplay  m_BossPreviewDisplay;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_EffectsSection                = Finder.Find(gameObject, "EffectsSection");
            m_EffectsContainer              = Finder.Find(gameObject, "EffectsContainer");
            m_RunesContainer                = Finder.Find(gameObject, "RunesContainer");
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
            SetUpTriggerEffects();
            SetUpRunePowers();
        }

        void SetUpTriggerEffects()
        {
            // clean content (remove potential TEST displays)
            UIHelper.CleanContent(m_EffectsContainer);

            // no trigger effects : deactivate and return
            if (m_ArenaLevelData.TriggerEffects.Count == 0)
            {
                m_EffectsContainer.SetActive(false);
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
        }

        void SetUpRunePowers()
        {
            UIHelper.CleanContent(m_RunesContainer);

            // no trigger effects : deactivate and return
            if (m_ArenaLevelData.PowerUps.Count == 0)
            {
                m_RunesContainer.SetActive(false);
                return;
            }

            m_RunesContainer.SetActive(true);

            // -- level of the spell
            int level = m_ArenaLevelData.StageData.Last().Level;

            // -- add UI for each PowerUp effects
            TemplateRunePowerUI templateRune = AssetLoader.LoadTemplateItem<TemplateRunePowerUI>();     // load template
            foreach (string powerName in m_ArenaLevelData.PowerUps)
            {
                SRunePower runePower = SpellLoader.GetPowerUp(powerName, level);
                TemplateRunePowerUI runeItemUI = Instantiate(templateRune, m_RunesContainer.transform);
                runeItemUI.Initialize(runePower);
            }
        }

        void SetUpBossPreview()
        {
            SStageData stageData = m_ArenaData.GetBossStageData(m_ArenaLevel);
            int level = (int)m_ArenaData.ArenaDifficulty * 2 + 1 + m_ArenaData.ArenaDifficultyLevel;
            m_BossPreviewDisplay.Initialize(stageData.Boss, level);
            m_BossPreviewDisplay.Button.onClick.AddListener(() => ScreenManager.SetPopUp(EPopUpState.BossInfoPopUp, stageData.Boss.ToString(), level, stageData.Spells.ConvertAll(t => t.ToString())));
        }

        #endregion


        #region State

        protected override void RefreshState()
        {
            if (ProgressionCloudData.IsArenaRewardCollected(m_ArenaType, m_ArenaData.ArenaDifficulty, m_ArenaLevel))
                SetState(EStageRewardState.Collected);
            else
                SetState(EStageRewardState.Unlocked);
            return;
        }

        protected override void SetUnlockedState()
        {
            m_CollectButton.gameObject.SetActive(false);
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