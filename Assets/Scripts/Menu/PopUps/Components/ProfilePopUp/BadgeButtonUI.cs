using System.Linq;
using Enums;
using Menu.Common.Buttons;
using Save;
using Tools;
using UnityEngine.UI;

namespace Menu.PopUps.Components.ProfilePopUp
{
    public class BadgeButtonUI : AchievementRewardUI
    {
        #region Members

        // =================================================================================
        // Data
        EBadge  m_Badge;
        ELeague m_League;

        string m_BadgeName      => ProfileCloudData.BadgeToString(m_Badge, m_League);
        public EBadge Badge     => m_Badge;
        public ELeague League   => m_League;

        // =================================================================================
        // GameObjects & Components
        Image   m_Icon;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Icon  = Finder.FindComponent<Image>(gameObject, "Icon");
        }
        public override void Initialize(string badgeName, EAchievementReward ar = EAchievementReward.Badge)
        {
            base.Initialize(badgeName, ar);
            RefreshUI(m_Name);
        }

        public void Initialize((EBadge, ELeague) badgeData)
        {
            Initialize(ProfileCloudData.BadgeToString(badgeData.Item1, badgeData.Item2));
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI(string badgeName)
        {
            if (! ProfileCloudData.TryGetBadgeFromString(badgeName, out m_Badge, out m_League))
            {
                ErrorHandler.Error("Unable to refresh UI of " + m_Name);
                return;
            }

            m_Name = badgeName;
            m_Icon.sprite = AssetLoader.LoadBadgeIcon(m_Name);
        }

        #endregion


        #region Button Methods

        public override void SetAsCurrent()
        {
            if (ProfileCloudData.CurrentBadges.Contains(m_BadgeName) && m_Badge != EBadge.None)
            {
                // TODO : error message ?
                ErrorHandler.Log(() => "Badge " + m_Badge + " already used");
                return;
            }

            var index = ProfileCloudData.LastSelectedBadgeIndex;
            if (m_Badge != EBadge.None)
                index = ProfileCloudData.GetFirstBadgeNoneIndex();

            ProfileCloudData.SetCurrentBadge(m_Badge, index >= 0 ? index : ProfileCloudData.LastSelectedBadgeIndex);
        }

        #endregion
    }
}