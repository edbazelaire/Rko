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

            Finder.Find(m_TabButtonContainer, "DebugButton").SetActive(ProfileCloudData.IsAdmin);

            // deactivate tabs button for non admins
            if (!ProfileCloudData.IsAdmin)
                m_TabButtonContainer.SetActive(false);

            m_SettingsTabManager.Initialize();
        }

        #endregion
    }
}