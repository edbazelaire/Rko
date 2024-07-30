using Enums;
using Menu.MainMenu;
using Save;
using Tools;
using UnityEngine;

namespace MainMenu.ProfileTab
{
    public class AchievementRewardsTabContent : TabContent
    {
        #region Members

        // Serialized Data
        [SerializeField] EAchievementReward m_AchivementRewardType;

        // GameObjects & Components
        GameObject m_ScrollerContent;
        AchievementRewardScrollItemUI m_Template;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            m_ScrollerContent = gameObject;
            m_Template = AssetLoader.Load<AchievementRewardScrollItemUI>(AssetLoader.c_AchievementsTemplatesPath + "AchievementRewardScrollItem");
        }

        protected override void SetUpUI()
        {
            UIHelper.CleanContent(m_ScrollerContent);

            foreach (var rewardName in ProfileCloudData.GetAchievementRewards(m_AchivementRewardType, true))
            {
                AddToScroller(rewardName);
            }
        }

        #endregion


        #region GUI Manipulators

        void AddToScroller(string rewardName)
        {
            var go = Instantiate(m_Template, m_ScrollerContent.transform);
            go.Initialize(rewardName, m_AchivementRewardType);

            // add listener of game object
            go.Button.onClick.AddListener(() => go.AchievementRewardUI.SetAsCurrent());
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            ProfileCloudData.AchievementRewardCollectedEvent += OnAchievementRewardCollected;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            ProfileCloudData.AchievementRewardCollectedEvent -= OnAchievementRewardCollected;
        }

        /// <summary>
        /// When an Achievement Rewards is collected, update scroller 
        /// </summary>
        /// <param name="arType"></param>
        /// <param name="rewardName"></param>
        void OnAchievementRewardCollected(EAchievementReward arType, string rewardName)
        {
            if (m_ScrollerContent == null)
            {
                ErrorHandler.Error("Calling destroyed game object ! Unregister OnAchievementRewardCollected()");
                return;
            }

            // not good type of achievement -> return
            if (arType != m_AchivementRewardType)
                return;

            ErrorHandler.Log("OnAchievementRewardCollected() : " + arType + " - " + rewardName, ELogTag.Achievements);

            // not a badge -> add and return
            if (arType != EAchievementReward.Badge)
            {
                AddToScroller(rewardName);
                return;
            }

            // badge : look for existing badge to upgrade
            if (!ProfileCloudData.TryGetBadgeFromString(rewardName, out EBadge newBadge, out ELeague newLeague))
            {
                ErrorHandler.Error("Unable to parse badge " + rewardName + " into badge / league");
                return;
            }

            var myCurrentTemplates = Finder.FindComponents<AchievementRewardScrollItemUI>(m_ScrollerContent);
            foreach (var template in myCurrentTemplates)
            {
                if (template.AchievementRewardUI.AchievementReward != EAchievementReward.Badge)
                    continue;

                if (!ProfileCloudData.TryGetBadgeFromString(template.AchievementRewardUI.Name, out EBadge currentBadge, out ELeague currentLeague))
                {
                    ErrorHandler.Error("Unable to parse : " + template.AchievementRewardUI.Name);
                    continue;
                }

                if (newBadge != currentBadge)
                {
                    continue;
                }

                if (newLeague <= currentLeague)
                    ErrorHandler.Error("Badge " + newBadge + " new league ("+ newLeague + ") is <= to currently unlocked league (" + currentLeague + ")" );
                else
                {
                    template.RefreshUI(rewardName);

                    // add listener of game object
                    template.Button.onClick.AddListener(() => template.AchievementRewardUI.SetAsCurrent());
                }
                return;
            }

            // badge not found : add to scroller
            AddToScroller(rewardName);
        }

        #endregion
    }
}