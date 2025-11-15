using System;

namespace Menu
{
    public enum EAchievementTab
    {
        Achievements,
        Avatars,
        Borders,
        Titles,
        Badges,
        Emots,
    }

    public class AchievementsTabManager : TabsManager
    {
        #region Members

        protected override Type m_TabEnumType { get; set; } = typeof(EAchievementTab);
        protected override Enum m_DefaultTab { get; set; } = EAchievementTab.Achievements;

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
