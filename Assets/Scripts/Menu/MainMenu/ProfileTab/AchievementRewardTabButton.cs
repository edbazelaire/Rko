using Enums;
using UnityEngine.UI;

namespace Menu.MainMenu
{
    public class AchievementRewardTabButton : TabButton
    {
        #region Members

        public Button Button => m_Button;

        #endregion


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