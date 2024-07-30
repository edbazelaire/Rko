using Data.GameManagement;
using Enums;
using Save;
using System.Collections.Generic;
using TMPro;
using Tools;
namespace Menu.MainMenu.MainTab
{
    public class ArenaStageSectionUI : StageSectionUI
    {
        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            PlayerPrefsHandler.ArenaTypeChangedEvent    += OnArenaTypeChanged;
            ProgressionCloudData.ArenaDataChangedEvent  += OnArenaDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            PlayerPrefsHandler.ArenaTypeChangedEvent    -= OnArenaTypeChanged;
            ProgressionCloudData.ArenaDataChangedEvent  -= OnArenaDataChanged;
        }

        void OnArenaTypeChanged(EArenaType arenaType)
        {
            ArenaData arenaData     = AssetLoader.LoadArenaData(arenaType);

            m_Level         = arenaData.CurrentLevel;
            m_CurrentLevel  = arenaData.CurrentLevel;
            m_CurrentStage  = arenaData.CurrentStage;

            RefreshUI();
        }

        void OnArenaDataChanged(EArenaType arenaType) 
        {
            RefreshUI();
        }

        #endregion
    }
}