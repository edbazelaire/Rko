using Assets;
using Assets.Scripts.Managers.Sound;
using Data.DataStructures;
using Data.GameManagement;
using Enums;
using Menu.Common.Buttons.TemplateItemButtons;
using Menu.MainMenu;
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
        /// <summary> layout container for spells </summary>
        GameObject          m_SpellsContainer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_EffectsSection                = Finder.Find(gameObject, "EffectsSection");
            m_EffectsContainer              = Finder.Find(gameObject, "EffectsContainer");
            m_SpellsContainer               = Finder.Find(gameObject, "SpellsContainer");
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
            m_StageSectionUI.Initialize(m_ArenaLevel, m_ArenaData.CurrentLevel, m_ArenaData.CurrentStage, m_ArenaLevelData.StageData.Count);

            base.SetUpUI();

            SetUpEffects();
            SetUpSpells();
        }

        #endregion


        #region GUI Manipulators

        void SetUpEffects()
        {
            // clean content (remove potential TEST displays)
            UIHelper.CleanContent(m_EffectsContainer);

            // no trigger effects : deactivate and return
            if (m_ArenaLevelData.TriggerEffects.Count == 0)
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
        }

        void SetUpSpells()
        {
            UIHelper.CleanContent(m_SpellsContainer);
            foreach (ESpell spell in m_ArenaLevelData.Spells)
            {
                TemplateSpellItemUI spellItemUI = Instantiate(AssetLoader.LoadTemplateItem(spell), m_SpellsContainer.transform).GetComponent<TemplateSpellItemUI>();
                spellItemUI.Initialize(spell, asIconOnly: true);

                // TODO : later
                int spellLevel = m_ArenaLevelData.StageData[0].CharacterLevel;
                spellItemUI.SetBottomOverlay("Level " + spellLevel);

                // display informations of the spell on click
                spellItemUI.Button.interactable = true;
                spellItemUI.Button.onClick.RemoveAllListeners();
                spellItemUI.Button.onClick.AddListener(() => { Main.SetPopUp(EPopUpState.SpellInfoPopUp, spell, spellLevel, true); });
            }
        }

        #endregion


        #region State

        protected override void RefreshState()
        {
            if (NotificationCloudData.HasRewardsForArenaTypeAtLevel(m_ArenaType, m_ArenaLevel))
            {
                SetState(EStageRewardState.Unlocked);
                return;
            }

            if (m_ArenaLevel < ProgressionCloudData.SoloArenas[m_ArenaType].CurrentLevel)
            {
                SetState(EStageRewardState.Collected);
                return;
            }

            SetState(EStageRewardState.Locked);
        }

        #endregion


        #region Rewards

        protected override SRewardsData GetRewards() 
        {
            return m_ArenaLevelData.RewardsData;
        }
        
        protected override void CollectReward()
        {
            base.CollectReward();

            if (!NotificationCloudData.CollectArenaReward(m_ArenaType, m_ArenaLevel))
                return;

            bool isLastReward = 
                ProgressionCloudData.IsArenaDifficultyCompleted(m_ArenaType)        // the current difficulty is finished
                && ! ProgressionCloudData.IsArenaCompleted(m_ArenaType)             // this is not the last difficulty level
                && ! NotificationCloudData.HasRewardsForArenaType(m_ArenaType);     // this was the last reward to collect for this arena type

            Main.DisplayRewards(m_ArenaLevelData.RewardsData, ERewardContext.ArenaReward.ToString(), isLastReward ? OnCollectingLastReward : null);
        }

        protected void OnCollectingLastReward() 
        {
            ProgressionCloudData.UpgradeArenaDifficulty(m_ArenaType);
        }

        #endregion
    }
}