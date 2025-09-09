using Enums;
using MyBox;
using Save;
using System;
using Tools;


namespace Data
{
    [Serializable]
    public struct SAchievementReward
    {
        public EAchievementReward AchievementReward;

        [ConditionalField("AchievementReward", false, EAchievementReward.Title)]
        public ETitle   Title;
        [ConditionalField("AchievementReward", false, EAchievementReward.Avatar)]
        public EAvatar  Avatar;
        [ConditionalField("AchievementReward", false, EAchievementReward.Border)]
        public EBorder  Border;
        [ConditionalField("AchievementReward", false, EAchievementReward.Emot)]
        public EEmot    Emot;
        [ConditionalField("AchievementReward", false, EAchievementReward.Badge)]
        public EBadge   Badge;
        [ConditionalField("AchievementReward", false, EAchievementReward.Badge)]
        public ELeague  League;

        public void Set(Enum value)
        {
            // try to extract type from provided value
            if (!ProfileCloudData.TryGetType(value, out AchievementReward))
                return;

            // try to set value from string
            Value = value.ToString();
        }

        public Enum EnumValue
        {
            get
            {
                switch (AchievementReward)
                {
                    case EAchievementReward.Title:
                        return Title;
                    case EAchievementReward.Avatar:
                        return Avatar;
                    case EAchievementReward.Border:
                        return Border;
                    case EAchievementReward.Badge:
                        return Badge;
                    case EAchievementReward.Emot:
                        return Emot;

                    default:
                        ErrorHandler.Error("Unahandled case : " + AchievementReward);
                        return default;
                }
            }
        }

        public string Value
        {
            get
            {
                if (AchievementReward == EAchievementReward.Badge)
                    return ProfileCloudData.BadgeToString(Badge, League);

                if (EnumValue == null)
                {
                    ErrorHandler.Error("EnumValue is null - return");
                    return "";
                }

                return EnumValue.ToString();
            }

            set
            {
                switch (AchievementReward)
                {
                    case EAchievementReward.Title:
                        if (!Enum.TryParse(value, out Title))
                            break;
                        return;

                    case EAchievementReward.Avatar:
                        if (!Enum.TryParse(value, out Avatar))
                            break;
                        return;

                    case EAchievementReward.Border:
                        if (!Enum.TryParse(value, out Border))
                            break;
                        return;

                    case EAchievementReward.Badge:
                        if (!ProfileCloudData.TryGetBadgeFromString(value, out Badge, out League))
                            break;
                        return;

                    case EAchievementReward.Emot:
                        if (!Enum.TryParse(value, out Emot))
                            break;
                        return;

                    default:
                        ErrorHandler.Error("Unahandled case : " + AchievementReward);
                        return;
                }

                ErrorHandler.Error("Unable to set value " + value + " for achievement of type " + AchievementReward);
            }
        }
    }
}