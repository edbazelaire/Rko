using Assets.Scripts.Game.Loaders.Filters;
using Data;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Tools;
using UnityEngine;

namespace Menu.MainMenu.ProfileTab
{
    public class AchievementsTabContent : TabContent
    {
        #region Members

        // GameObjects & Components
        GameObject m_ScrollerContent;
        TemplateAchievementButton m_TemplateAchievementButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            m_ScrollerContent = gameObject;
            m_TemplateAchievementButton = AssetLoader.LoadTemplateItem<TemplateAchievementButton>("Achievement");
        }

        protected override void SetUpUI()
        {
            // reset UI
            UIHelper.CleanContent(m_ScrollerContent);

            foreach (IAchievement achievement in AchievementLoader.Achievements.FilterByCharacter(ECharacter.None, strict: true))
            {
                // skip completed achievements 
                if (achievement.Current == null)
                    continue;

                var go = Instantiate(m_TemplateAchievementButton, m_ScrollerContent.transform);
                go.Initialize(achievement);
            }
        }

        #endregion
    }
}