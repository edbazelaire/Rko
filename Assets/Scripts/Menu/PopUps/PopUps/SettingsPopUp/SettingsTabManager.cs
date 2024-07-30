using Menu;
using System;
namespace Tools.Debugs.BetaTest
{
    public enum ESettingsItemTab
    {
        Sound,
        Data,
        Settings,
        Debug,
        Account,
    }

    public class SettingsTabManager : TabsManager
    {
        #region Members

        protected override Type m_TabEnumType { get; set; } = typeof(ESettingsItemTab);
        protected override Enum m_DefaultTab { get; set; } = ESettingsItemTab.Sound;

        #endregion


        #region Init & End

        protected override void InitTabsContent()
        {
            base.InitTabsContent();

            m_TabsContainerContent = m_TabsContainer;
        }

        #endregion
    }
}