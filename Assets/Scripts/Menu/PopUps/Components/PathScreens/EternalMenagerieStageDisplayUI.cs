using Assets.Scripts.Managers;
using Data;
using Data.DataStructures;
using Data.DataStructures.PowerEffects;
using Data.GameManagement;
using Enums;
using Game.GameManagers.ArenaModules;
using Game.Loaders;
using Tools;

namespace Menu.PopUps
{
    public class EternalMenagerieStageDisplayUI : ArenaStageDisplayUI
    {
        #region Members

        //PortalPreviewDisplay m_PortalPreviewDisplay;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            //m_PortalPreviewDisplay = Finder.FindComponent<PortalPreviewDisplay>(gameObject);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            //m_PortalPreviewDisplay.Initialize(m_ArenaData as EternalMenagerieData, m_ArenaLevel);
        }

        #endregion


        #region GUI Manipulators

        public override void RefreshUI()
        {
            base.RefreshUI();
        }

        #endregion


        #region State

        protected override void RefreshState()
        {
            base.RefreshState();
        }

        #endregion
    }
}