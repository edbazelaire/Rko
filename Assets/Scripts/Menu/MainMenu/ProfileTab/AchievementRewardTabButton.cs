using Enums;

namespace Menu.MainMenu
{
    public class AchievementRewardTabButton : TabButton
    {
        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public void Initialize(EAchievementReward achievementReward)
        {
            base.Initialize();

            m_Text.text = achievementReward.ToString();
        }

        #endregion


        #region GUI Manipulators

        

        #endregion
    }
}