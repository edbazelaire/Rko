using Enums;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class AvatarButtonUI : AchievementRewardUI
    {
        #region Members

        Image       m_AvatarIcon;
        Image       m_AvatarBorder;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_AvatarIcon    = Finder.FindComponent<Image>(gameObject, "AvatarIcon");
            m_AvatarBorder  = Finder.FindComponent<Image>(gameObject, "AvatarBorder");
        }

        public override void Initialize(string name, EAchievementReward achievementReward)
        {
            base.Initialize(name, achievementReward);

            // setup Avatar Icon & Border
            SetAvatar(achievementReward == EAchievementReward.Avatar ? name : EAvatar.None.ToString());
            SetBorder(achievementReward == EAchievementReward.Border ? name : EBorder.None.ToString());
        }
        
        public void Initialize(string avatar, string border)
        {
            base.Initialize(name, EAchievementReward.Avatar);

            // setup Avatar Icon & Border
            SetAvatar(avatar);
            SetBorder(border);
        }

        #endregion


        #region GUI Manipulators

        public void SetAvatar(string name)
        {
            m_AvatarIcon.sprite = AssetLoader.Load<Sprite>(name, AssetLoader.c_AvatarsPath);
        }

        public void SetBorder(string name)
        {
            m_AvatarBorder.sprite = AssetLoader.Load<Sprite>(name, AssetLoader.c_BordersPath);
        }

        #endregion
    }
}