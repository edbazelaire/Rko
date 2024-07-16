using Assets;
using Assets.Scripts.Managers.Sound;
using Data.GameManagement;
using Enums;
using Menu.Common.Displayers;
using Menu.Common.Notifications;
using Menu.MainMenu;
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

        GameObject          m_SpellsContainer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SpellsContainer               = Finder.Find(gameObject, "SpellsContainer");
        }

        public void Initialize(ArenaData arenaData, int arenaLevel, EArenaType arenaType)
        {
            m_ArenaData = arenaData;
            m_ArenaLevelData = arenaData.GetArenaLevelData(arenaLevel);
            m_ArenaLevel = arenaLevel;
            m_ArenaType = arenaType;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            m_StageSectionUI.Initialize(m_ArenaLevel, m_ArenaData.CurrentLevel, m_ArenaData.CurrentStage, m_ArenaLevelData.StageData.Count);

            base.SetUpUI();

            SetUpSpells();
        }

        #endregion


        #region GUI Manipulators

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

            Main.DisplayRewards(m_ArenaLevelData.RewardsData, ERewardContext.ArenaReward);
        }

        #endregion
    }
}