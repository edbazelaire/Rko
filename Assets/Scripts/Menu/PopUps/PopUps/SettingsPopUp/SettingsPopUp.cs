using Save;
using Tools;
using Tools.Debugs.BetaTest;
using UnityEngine;

namespace Menu.PopUps
{
    public class SettingsPopUp : PopUp
    {

        #region Members

        GameObject m_TabButtonContainer;
        SettingsTabManager m_SettingsTabManager;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TabButtonContainer = Finder.Find(gameObject, "TabButtonsContainer");
            m_SettingsTabManager = Finder.FindComponent<SettingsTabManager>(gameObject);
        }


        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            // hide some tabs for non-admins
            Finder.Find(m_TabButtonContainer, "DebugButton").SetActive(ProfileCloudData.IsAdmin);
            Finder.Find(m_TabButtonContainer, "DataButton").SetActive(ProfileCloudData.IsAdmin);
            Finder.Find(m_TabButtonContainer, "SettingsButton").SetActive(ProfileCloudData.IsAdmin);

            m_SettingsTabManager.Initialize();
        }

        #endregion
    }
}