using Analytics.Events;
using Assets;
using Data.GameManagement;
using Enums;
using Inventory;
using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.VisualScripting;

namespace Save
{
    public struct SProfileDataNetwork : INetworkSerializable
    {
        public int                      AccountLevel;
        public FixedString32Bytes       GamerTag;
        public FixedString32Bytes       Avatar;
        public FixedString32Bytes       Border;
        public FixedString32Bytes       Title;
        public FixedString32Bytes[]     Badges;

        public SProfileDataNetwork(int accountLevel = 0, string gamerTag = default, string avatar = default, string border = default, string title = default, string[] badges = null)
        {
            if (gamerTag == default)
                gamerTag = SProfileCurrentData.DEFAULT_PSEUDO;

            if (avatar == default)
                avatar = SProfileCurrentData.DEFAULT_AR[EAchievementReward.Avatar];

            if (border == default)
                border = SProfileCurrentData.DEFAULT_AR[EAchievementReward.Border];

            if (title == default)
                title = SProfileCurrentData.DEFAULT_AR[EAchievementReward.Title];

            if (badges == null)
                badges = SProfileCurrentData.DEFAULT_BADGES;

            AccountLevel    = accountLevel;
            GamerTag        = gamerTag;
            Avatar          = avatar;
            Border          = border;
            Title           = title;
            Badges          = badges.Select(badge => (FixedString32Bytes)badge).ToArray();
        }

        #region Network Serialization

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AccountLevel);
            serializer.SerializeValue(ref GamerTag);
            serializer.SerializeValue(ref Avatar);
            serializer.SerializeValue(ref Border);
            serializer.SerializeValue(ref Title);

            // Serialize the length of the badges array followed by each badge string
            int numBadges = Badges != null ? Badges.Length : 0;
            serializer.SerializeValue(ref numBadges);
            if (serializer.IsReader)
            {
                Badges = new FixedString32Bytes[numBadges];
            }

            for (int i = 0; i < numBadges; i++)
            {
                serializer.SerializeValue(ref Badges[i]);
            }
        }

        #endregion
    }

    [Serializable]
    public struct SProfileCurrentData
    {
        #region Members

        // ========================================================================================================================
        // CONSTANTS
        /// <summary> default gamer tag </summary>
        public const string DEFAULT_PSEUDO = "DEFAULT_PSEUDO";
        public static Dictionary<EAchievementReward, string> DEFAULT_AR => new Dictionary<EAchievementReward, string>(){
            { EAchievementReward.Avatar,    EAvatar.None.ToString() },
            { EAchievementReward.Border,    EBorder.None.ToString() },
            { EAchievementReward.Title,     ETitle.None.ToString()  },
        };
        public static string DEFAULT_BADGE => EBadge.None.ToString();
        public static string[] DEFAULT_BADGES => new string[] { DEFAULT_BADGE, DEFAULT_BADGE, DEFAULT_BADGE };

        public int      AccountLevel; 
        public string   GamerTag; 
        public string   Avatar; 
        public string   Border; 
        public string   Title;
        public string[] Badges;

        #endregion

        public SProfileCurrentData(int accountLevel = 1, string gamerTag = default, string avatar = default, string border = default, string title = default, string[] badges = null)
        {
            if (accountLevel == 0)
                accountLevel = 1;

            if (gamerTag == default)
                gamerTag = ProfileCloudData.GenerateDefaultPseudo();

            if (avatar == default)
                avatar = DEFAULT_AR[EAchievementReward.Avatar];

            if (border == default)
                border = DEFAULT_AR[EAchievementReward.Border];

            if (title == default)
                title = DEFAULT_AR[EAchievementReward.Title];

            if (badges == null)
                badges = DEFAULT_BADGES;

            AccountLevel    = accountLevel;
            GamerTag        = gamerTag;
            Avatar          = avatar;
            Border          = border;
            Title           = title;
            Badges          = badges;
        }

        public SProfileDataNetwork AsNetworkSerializable()
        {
            return new SProfileDataNetwork(AccountLevel, GamerTag, Avatar, Border, Title, Badges);
        }

         
        #region Getter & Setter

        public string Get(EAchievementReward achievementReward)
        {
            switch (achievementReward)
            {
                case EAchievementReward.Avatar:
                    return Avatar;

                case EAchievementReward.Border:
                    return Border;

                case EAchievementReward.Title:
                    return Title;

                default:
                    ErrorHandler.Error("Unhandled case : " + achievementReward);
                    return "";
            }
        }

        public void Set(EAchievementReward achievementReward, string value)
        {
            switch (achievementReward)
            {
                case EAchievementReward.Avatar:
                    Avatar = value;
                    break;

                case EAchievementReward.Border:
                    Border = value;
                    break;

                case EAchievementReward.Title:
                    Title = value;
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + achievementReward);
                    break;
            }
        }


        #endregion


        #region Checkers

        public void Check()
        {
            CheckLevelAccount();
            CheckGamerTag();
            CheckAchievementRewards();
        }

        void CheckLevelAccount()
        {
            if (AccountLevel <= 0)
            {
                ErrorHandler.Error("Bad AccountLevel detected : " + AccountLevel + " - reseting to 1");
                AccountLevel = 1;
            }

            if (AccountLevel > CollectablesManagementData.Instance.AccountLevelData.Count)
            {
                ErrorHandler.Error("Bad AccountLevel detected : " + AccountLevel + " - reseting to " + CollectablesManagementData.Instance.AccountLevelData.Count);
                AccountLevel = CollectablesManagementData.Instance.AccountLevelData.Count;
            }
        }

        void CheckGamerTag()
        {
            if (GamerTag == null || GamerTag == "")
            {
                ErrorHandler.Error("GamerTag not set : reseting with default");
                GamerTag = DEFAULT_PSEUDO;
                return;
            }
        }

        void CheckAchievementRewards()
        {
            foreach (EAchievementReward ar in Enum.GetValues(typeof(EAchievementReward)))
            {
                if (ar == EAchievementReward.None)
                    continue;

                CheckAchievementReward(ar);
            }
        }

        /// <summary>
        /// Check that current AchievementReward are set and unlocked
        /// </summary>
        /// <param name="achievementReward"></param>
        void CheckAchievementReward(EAchievementReward achievementReward)
        {
            // special BADGE checks
            if (achievementReward == EAchievementReward.Badge)
            {
                CheckCurrentBadges();
                return;
            }

            string value = Get(achievementReward);
            if (value == null || value == "")
            {
                ErrorHandler.Warning("Current "+ achievementReward.ToString() +" is empty : use default one");
                Set(achievementReward, DEFAULT_AR[achievementReward]);
                return;
            }

            // CHECK : exists in the unlocked data
            if (! ProfileCloudData.AchievementRewards[achievementReward].Contains(value))
            {
                ErrorHandler.Warning("Avatar is set with value " + achievementReward.ToString() + " but was not found in unlocked data - reseting");
                Set(achievementReward, DEFAULT_AR[achievementReward]);
            }
        }

        /// <summary>
        /// Check that current badges are set and that provided values are correct (exist, unlocked, ...)
        /// </summary>
        void CheckCurrentBadges()
        {
            // CHECK : setup correctly 
            if (Badges == null || Badges.Length == 0)
            {
                ErrorHandler.Warning("Current Badges are empty : use default ones");
                Badges = DEFAULT_BADGES;
                return;
            }

            // CHECK : badge exists
            for (int i = 0; i < Badges.Length; i++)
            {
                // CHECK : can be parsed
                if (! ProfileCloudData.TryGetBadgeFromString(Badges[i], out EBadge badge, out ELeague league))
                {
                    ErrorHandler.Warning("Unable to parse badge " + Badges[i] + " - reseting to default");
                    Badges[i] = DEFAULT_BADGE;
                    continue;
                }

                // CHECK : is in unlocked data
                if (ProfileCloudData.Badges == null || ! ProfileCloudData.Badges.ContainsKey(badge))
                {
                    ErrorHandler.Warning("Badge " + badge + " not found in unlocked data - reseting to default");
                    Badges[i] = DEFAULT_BADGE;
                    continue;
                }

                // CHECK : league was unlocked
                if (ProfileCloudData.Badges[badge] != league)
                {
                    ErrorHandler.Warning("Badge " + badge + " was set with league " + league + " but current league found was : " + ProfileCloudData.Badges[badge]);
                    Badges[i] = ProfileCloudData.BadgeToString(badge, league);
                    continue;
                }
            }
        }

        #endregion
    }

    [Serializable]
    public struct SPublicProfileData
    {
        public string       Pseudo;
        public string       Tag;
        public string       Token;
        public string       Region;
        //public Presence     Availability;
        public string PlayerName => Pseudo + Tag;   
        public bool IsValid => ! Pseudo.IsNullOrEmpty() 
            && ! Tag.IsNullOrEmpty() 
            && ! Token.IsNullOrEmpty() 
            && ! Region.IsNullOrEmpty();

        public SPublicProfileData(EntityData entityData)
        {
            Pseudo      = "";
            Tag         = "";
            Token       = "";
            Region      = "";

            foreach (Item data in entityData.Data)
            {
                switch (data.Key)
                {
                    case ProfileCloudData.KEY_GAMER_TAG:
                        Pseudo = data.Value.GetAsString();
                        break;

                    case ProfileCloudData.KEY_TAG:
                        Tag = data.Value.GetAsString();
                        break;

                    case ProfileCloudData.KEY_TOKEN:
                        Token = data.Value.GetAsString();
                        break;

                    case ProfileCloudData.KEY_REGION:
                        Region = data.Value.GetAsString();
                        break;

                    default:
                        ErrorHandler.Error("Unhandled case " + data.Key);
                        break;
                }
            }
        }
    }

    public class ProfileCloudData : CloudData
    {
        #region Members

        public new static ProfileCloudData Instance => CloudSaveManager.Instance.GetCloudData(typeof(ProfileCloudData)) as ProfileCloudData;

        // ===============================================================================================
        // CONSTANTS
        /// <summary> number of spells in one build </summary>
        public const int N_BADGES_DISPLAYED = 3;
        public const int MIN_CHAR_GAMER_TAG = 4;
        public const int MAX_CHAR_GAMER_TAG = 25;
        public static List<char> FORBIDDEN_CHARACTERS => new (){ '#', ' ', '\\' };

        // KEYS ------------------------------------
        public const string KEY_TUTO_DONE               = "TutoDone";
        public const string KEY_PSEUDO_CHANGED          = "PseudoChanged";
        public const string KEY_GAMER_TAG               = "GamerTag";
        public const string KEY_IS_ADMIN                = "IsAdmin";
        public const string KEY_TAG                     = "Tag";
        public const string KEY_TOKEN                   = "Token";
        public const string KEY_REGION                  = "Region";
        public const string KEY_CURRENT_PROFILE_DATA    = "CurrentProfileData";
        public const string KEY_ACHIEVEMENTS            = "Achievements";
        public const string KEY_ACHIEVEMENT_REWARDS     = "AchievementRewards";
        public const string KEY_GIFT_CODES              = "GiftCodes";

        // ===============================================================================================
        // EVENTS
        /// <summary> action fired when the amount of gold changed </summary>
        public static Action                                AccountLevelUpEvent;
        public static Action<EAchievementReward, string>    AchievementRewardCollectedEvent;
        public static Action<string>                        AchievementCompletedEvent;
        public static Action                                PseudoChangedEvent;
        public static Action<EAchievementReward>            CurrentDataChanged;
        public static Action<int>                           CurrentBadgeChangedEvent;
        public static Action<EBadge, ELeague>               BadgeUnlockedEvent;

        // ===============================================================================================
        // DATA
        protected override List<string> m_PublicKeys => new() { KEY_GAMER_TAG, KEY_TAG, KEY_REGION, KEY_TOKEN };

        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_TUTO_DONE,                true                                                },
            { KEY_PSEUDO_CHANGED,           false                                               },
            { KEY_GAMER_TAG,                ""                                                  },
            { KEY_IS_ADMIN,                 false                                               },
            { KEY_TAG,                      ""                                                  },
            { KEY_TOKEN,                    ""                                                  },
            { KEY_REGION,                   ""                                                  },
            { KEY_CURRENT_PROFILE_DATA,     new SProfileCurrentData()                           },
            { KEY_ACHIEVEMENTS,             new Dictionary<string, int>()                       },
            { KEY_ACHIEVEMENT_REWARDS,      new Dictionary<EAchievementReward, List<string>>()  },
            { KEY_GIFT_CODES,               new List<string>()                                  },
        };

        /// <summary> dictionary of currently unlocked badges linked to max league </summary>
        private Dictionary<EBadge, ELeague> m_Badges;

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        public static int                   LastSelectedBadgeIndex = 0;
        public static bool                  IsAdmin             => (bool)Instance.m_Data[KEY_IS_ADMIN];
        public static bool                  IsAccountMaxed      => AccountLevel >= CollectablesManagementData.Instance.AccountLevelData.Count;
        public static bool                  IsAccountUpgradable => ! IsAccountMaxed && CollectablesManagementData.GetCurrentAccountLevelData().RequiredXp <= InventoryCloudData.Instance.GetCurrency(ECurrency.TotalXp);
        public static string                PlayerName          => GamerTag + Tag;
        public static string                GamerTag            => (string)Instance.m_Data[KEY_GAMER_TAG];
        public static string                Tag                 => (string)Instance.m_Data[KEY_TAG];
        public static bool                  TutoDone            => (bool)Instance.m_Data[KEY_TUTO_DONE];
        public static bool                  PseudoChanged       => (bool)Instance.m_Data[KEY_PSEUDO_CHANGED];
        public static string                Token               => (string)Instance.m_Data[KEY_TOKEN];
        public static string                Region              => (string)Instance.m_Data[KEY_REGION];
        public static SProfileCurrentData   CurrentProfileData  => (SProfileCurrentData)Instance.m_Data[KEY_CURRENT_PROFILE_DATA];
        public static string[]              CurrentBadges       => CurrentProfileData.Badges;

        public static int                   AccountLevel    => CurrentProfileData.AccountLevel;
        public static Dictionary<string, int>   Achievements    => (Instance.m_Data[KEY_ACHIEVEMENTS] as Dictionary<string, int>);
        public static Dictionary<EAchievementReward, List<string>> AchievementRewards => (Instance.m_Data[KEY_ACHIEVEMENT_REWARDS] as Dictionary<EAchievementReward, List<string>>);
        public static Dictionary<EBadge, ELeague> Badges        => Instance.m_Badges;
        public static List<string>          GiftCodes           => Instance.m_Data[KEY_GIFT_CODES] as List<string>;
        public static bool                  HasDefaultPseudo    => Regex.IsMatch(GamerTag, @"^User_\d{6}$") || GamerTag == "DEFAULT_PSEUDO";

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert SCharacterBuildsCloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <param name="charsBuildsList"></param>
        /// <returns></returns>
        protected override object Convert(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(SProfileCurrentData))
                return item.Value.GetAs<SProfileCurrentData>();

            if (m_Data[item.Key].GetType() == typeof(Dictionary<EBadge, ELeague>))
                return item.Value.GetAs<Dictionary<EBadge, ELeague>>();

            if (m_Data[item.Key].GetType() == typeof(Dictionary<EAchievementReward, List<string>>))
                return item.Value.GetAs<Dictionary<EAchievementReward, List<string>>>();

            if (m_Data[item.Key].GetType() == typeof(Dictionary<EAchievementReward, string>))
                return item.Value.GetAs<Dictionary<EAchievementReward, string>>();

            if (m_Data[item.Key].GetType() == typeof(Dictionary<string, int>))
                return item.Value.GetAs<Dictionary<string, int>>();

            return base.Convert(item);
        }

        #endregion


        #region Current Selected Data Manipulator

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetGamerTag(string gamerTag)
        {
            var data = CurrentProfileData;
            data.GamerTag = gamerTag;

            Instance.SetData(KEY_CURRENT_PROFILE_DATA, data);
            Instance.SetData(KEY_GAMER_TAG, gamerTag);

            if (gamerTag != SProfileCurrentData.DEFAULT_PSEUDO)
                Instance.SetData(KEY_PSEUDO_CHANGED, true);

            // update value in AuthService
            AuthenticationService.Instance.UpdatePlayerNameAsync(gamerTag);

            MAnalytics.SendEvent(new PlayerDataEvent(gamerTag, Token, Region));
            PseudoChangedEvent?.Invoke();
        }

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetTag(string tag)
        {
            Instance.SetData(KEY_TAG, tag);
        }

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetToken(string token)
        {
            Instance.SetData(KEY_TOKEN, token);
        }

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetRegion(string region)
        {
            Instance.SetData(KEY_REGION, region);
        }

        public static void ResetGamerTag()
        {
            Instance.SetData(KEY_GAMER_TAG, SProfileCurrentData.DEFAULT_PSEUDO);
        }

        /// <summary>
        /// Get value currently selected as profile
        /// </summary>
        /// <param name="achR"></param>
        /// <returns></returns>
        public static string GetCurrentData(EAchievementReward achR)
        {
            if (achR == EAchievementReward.Badge)
                return CurrentProfileData.Badges[LastSelectedBadgeIndex];

            return CurrentProfileData.Get(achR);
        }

        public static void SetCurrentData(EAchievementReward achR, string value)
        {
            var data = CurrentProfileData;
            data.Set(achR, value);
            Instance.SetData(KEY_CURRENT_PROFILE_DATA, data);

            CurrentDataChanged?.Invoke(achR);
        }

        /// <summary>
        /// Get index of first "unset" badge in Current Badges. Returns -1 if all badges are set
        /// </summary>
        /// <returns></returns>
        public static int GetFirstBadgeNoneIndex()
        {
            for (int index = 0; index < CurrentBadges.Length; index++)
            {
                if (CurrentBadges[index] == null || CurrentBadges[index] == EBadge.None.ToString() || CurrentBadges[index] == "")
                {
                    return index;
                }
            }

            return -1;
        }

        public static void SetCurrentBadge(EBadge badge, int index)
        {
            CurrentBadges[index] = BadgeToString(badge, Badges[badge]);

            // Save & Fire event of the change
            Instance.SetData(KEY_CURRENT_PROFILE_DATA, CurrentProfileData);
            CurrentDataChanged?.Invoke(EAchievementReward.Badge);
        }

        #endregion


        #region Check Public Data

        /// <summary>
        /// 
        /// </summary>
        public static async Task<(bool Success, string Reason)> IsGamerTagValid(string gamerTag, bool checkAvailable = true)
        {
            if (gamerTag.Length < MIN_CHAR_GAMER_TAG || gamerTag.Length > MAX_CHAR_GAMER_TAG)
            {
                return (false, TextLocalizer.LocalizeText("gamer tag must have between " + MIN_CHAR_GAMER_TAG + " and " + MAX_CHAR_GAMER_TAG + " characters"));
            }

            if (gamerTag.Contains(" "))
            {
                return (false, "Spaces are not allowe");
            }

            foreach (char c in gamerTag)
            {
                if (FORBIDDEN_CHARACTERS.Contains(c))
                {
                    return (false, "'" + c + "' is not allowed (" + string.Join(", ", FORBIDDEN_CHARACTERS) + ")"); 
                }
            }

            if (checkAvailable && await Instance.FindPlayerWithValue(KEY_GAMER_TAG, gamerTag) != null)
            {
                return (false, "Pseudo already used");
            }

            return (true, "");
        }

        #endregion


        #region Account Level

        public static void UpgradeAccountLevel(bool save = true)
        {
            if (IsAccountMaxed)
            {
                ErrorHandler.Error("Trying to level up an account already maxed");
                return;
            }

            int requiredXp = CollectablesManagementData.GetCurrentAccountLevelData().RequiredXp;
            int totalXp = InventoryCloudData.Instance.GetCurrency(ECurrency.TotalXp);
            if (totalXp < requiredXp)
            {
                ErrorHandler.Error("Trying to level up an account but not enought TOTAL XP (" + totalXp + ") - required " + requiredXp);
                return;
            }

            // level up account
            var currentProfileData = CurrentProfileData;
            currentProfileData.AccountLevel++;
            Instance.m_Data[KEY_CURRENT_PROFILE_DATA] = currentProfileData;

            if (save)
                Instance.SaveValue(KEY_CURRENT_PROFILE_DATA);

            // update collectable
            InventoryManager.Spend(requiredXp, ECurrency.TotalXp, "AccountLevelUp");

            // fire event that the account has been leveled up
            AccountLevelUpEvent?.Invoke();
        }

        #endregion


        #region Achievements

        public static int GetAchievementIndex(string achievement)
        {
            if (Achievements.ContainsKey(achievement))
                return Achievements[achievement];

            return 0;
        }

        public static void CompleteAchievement(string achievement)
        {
            ErrorHandler.Log("CompleteAchievement : " + achievement, ELogTag.Achievements);

            if (Achievements.ContainsKey(achievement))
                Achievements[achievement]++;
            else
                Achievements[achievement] = 1;

            ErrorHandler.Log("      + Saving", ELogTag.Achievements);
            Instance.SaveValue(KEY_ACHIEVEMENTS);

            AchievementCompletedEvent?.Invoke(achievement);
        }

        #endregion


        #region Achievement Rewards Data

        /// <summary>
        /// Get all values of a specific achievement reward type
        /// </summary>
        /// <param name="achievementReward"></param>
        /// <returns></returns>
        public static List<string> GetAchievementRewards(EAchievementReward achievementReward, bool highestOnly = false)
        {
            if (! highestOnly || achievementReward != EAchievementReward.Badge)
                return AchievementRewards[achievementReward];

            var values = new List<string>();
            foreach (var item in Badges)
            {
                values.Add(BadgeToString(item.Key, item.Value));
            }

            return values;
        }

        /// <summary>
        /// Add a title, an avatar or a border to the list of unlocked achievements rewards
        /// </summary>
        /// <param name="achievementReward"></param>
        /// <param name="value"></param>
        public static void AddAchievementReward(EAchievementReward achievementReward, string value, bool save = true)
        {
            ErrorHandler.Log("AddAchievementReward() : " + value, ELogTag.Achievements);
            if (HasAchievementReward(achievementReward, value))
            {
                ErrorHandler.Error("Trying to unlock achievement REWARD already unlocked : " + achievementReward.ToString() + " - " + value);
                return;
            }

            // add & save data
            var data = AchievementRewards;
            data[achievementReward].Add(value);
            if (save)
                Instance.SetData(KEY_ACHIEVEMENT_REWARDS, data, true);

            // fire event that a new reward has been collected
            AchievementRewardCollectedEvent?.Invoke(achievementReward, value);

            // if is badge, update league of badge
            if (achievementReward == EAchievementReward.Badge)
                UpdateBadgeLeague(value);
        }

        void RemoveAchievementReward(EAchievementReward achievementReward, string value, bool save = true)
        {
            if (! HasAchievementReward(achievementReward, value))
            {
                ErrorHandler.Warning("Trying to remove value " + value + " in " + achievementReward.ToString() + " but the value was not found");
                return;
            }

            AchievementRewards[achievementReward].Remove(value);

            if (save)
                Instance.SaveValue(KEY_ACHIEVEMENT_REWARDS);
        } 

        public static bool HasAchievementReward(EAchievementReward achievementReward, string value)
        {
            return AchievementRewards[achievementReward].Contains(value);
        }

        #endregion


        #region Badges

        /// <summary>
        /// Filter values of the same reward to get the highest league unlocked
        /// </summary>
        /// <param name="achievementReward"></param>
        /// <returns></returns>
        Dictionary<EBadge, ELeague> FilterHighestLeague(EAchievementReward achievementReward)
        {
            var filteredDict = new Dictionary<EBadge, ELeague>();
            if (achievementReward != EAchievementReward.Badge)
            {
                ErrorHandler.Warning("FilterHighestLigue() currently not handled for other types than Badge");
                return filteredDict;
            }

            foreach (string valueName in GetAchievementRewards(achievementReward))
            {
                if (!TryGetBadgeFromString(valueName, out EBadge badge, out ELeague league))
                    continue;

                // check that current set value is already sup to provided league
                if (filteredDict.ContainsKey(badge) && filteredDict[badge] > league)
                    continue;

                filteredDict[badge] = league;
            }

            return filteredDict;
        }

        /// <summary>
        /// Add badge to dict of unlocked badge
        /// </summary>
        /// <param name="badge"></param>
        /// <param name="rarety"></param>
        public static void UpdateBadgeLeague(string badgName)
        {
            if (!TryGetBadgeFromString(badgName, out EBadge badge, out ELeague league))
                return;

            // update the value
            Badges[badge] = league;

            // fire event of the change
            BadgeUnlockedEvent?.Invoke(badge, league);
        }

        public static string BadgeToString(EBadge badge, ELeague league)
        {
            return badge.ToString() + (league != ELeague.None ? league.ToString() : "");
        }

        public static (EBadge EBadge, ELeague league) BadgeFromString(string badgeName)
        {
            ELeague league = ELeague.None;
            foreach (ELeague leagueValue in Enum.GetValues(typeof(ELeague)))
            {
                if (badgeName.EndsWith(leagueValue.ToString()))
                {
                    league = leagueValue;
                    badgeName = badgeName[..^league.ToString().Length];
                    break;
                }
            }

            // parse the badge name into the expected enum
            if (!Enum.TryParse(badgeName, out EBadge badge))
            {
                ErrorHandler.Error("Unable to parse badge name (" + badgeName + ") into badge / leage");
                return (EBadge.None, ELeague.None);
            }

            return (badge, league);
        }

        public static bool TryGetBadgeFromString(string badgeName, out EBadge badge, out ELeague league)
        {
            badge = EBadge.None;
            league = ELeague.None;

            if (badgeName == "None")
                return true;

            foreach (ELeague leagueValue in Enum.GetValues(typeof(ELeague)))
            {
                if (leagueValue == ELeague.None)
                    continue;

                if (badgeName.EndsWith(leagueValue.ToString()))
                {
                    league = leagueValue;
                    badgeName = badgeName[..^league.ToString().Length];
                    break;
                }
            }

            // parse the badge name into the expected enum
            if (!Enum.TryParse(badgeName, out badge))
            {
                ErrorHandler.Error("Unable to parse badge name (" + badgeName + ") into badge / leage");
                badge = EBadge.None;
                return false;
            }

            return true;

        }

        #endregion


        #region Gift Codes

        public async static Task<bool> AddGiftCode(string code, bool save = true)
        {
            if (HasGiftCode(code))
                return false;

            GiftCodes.Add(code);

            if (save)
            {
                return await Instance.SaveValueAsync(KEY_GIFT_CODES);
            }

            return true;
        }

        public static bool HasGiftCode(string code)
        {
            return GiftCodes.Contains(code);
        }

        #endregion


        #region Helpers

        public static bool TryGetType(Enum ar, out EAchievementReward arType, bool throwError = true)
        {
            return TryGetType(ar.GetType(), out arType, throwError);
        }

        public static bool TryGetType(Type type, out EAchievementReward arType, bool throwError = true)
        {
            arType = EAchievementReward.None;
            foreach (EAchievementReward ar in Enum.GetValues(typeof(EAchievementReward)))
            {
                if (type == GetTypeOf(ar))
                {
                    arType = ar;
                    return true;
                }
            }

            if (throwError)
                ErrorHandler.Error("Unable to parse type " + type + " as EAchievementReward");
            return false;
        }

        public static bool IsAchievementRewardType(Type type)
        {
            foreach (EAchievementReward ar in Enum.GetValues(typeof(EAchievementReward)))
            {
                if (ar == EAchievementReward.None)
                    continue;

                if (type == GetTypeOf(ar))
                    return true;
            }

            return false;
        }

        public static Type GetTypeOf(EAchievementReward achievementReward)
        {
            switch(achievementReward)
            {
                case EAchievementReward.None:
                    return null;

                case EAchievementReward.Title:
                    return typeof(ETitle);

                case EAchievementReward.Avatar:
                    return typeof(EAvatar);

                case EAchievementReward.Border:
                    return typeof(EBorder);

                case EAchievementReward.Badge:
                    return typeof(EBadge);

                default:
                    ErrorHandler.Error("Unhandled case : " + achievementReward);
                    return null;
            }
        }

        public static string GenerateDefaultPseudo()
        {
            int randomNumber = UnityEngine.Random.Range(100000, 999999); // Generates a 6-digit random number
            return $"User_{randomNumber}";
        }

        #endregion


        #region Reset & Unlock

        public override void Reset(string key, bool save = true) 
        {
            base.Reset(key, save);

            switch (key)
            {
                // List of un-resetable data
                case KEY_PSEUDO_CHANGED:
                case KEY_TUTO_DONE:
                    break;

                case KEY_CURRENT_PROFILE_DATA:
                    var data = new SProfileCurrentData();
                    data.Check();
                    Instance.m_Data[key] = data;
                    break;

                case KEY_GAMER_TAG:
                    SetGamerTag(SProfileCurrentData.DEFAULT_PSEUDO);
                    return;
                    
                case KEY_TOKEN:
                    SetToken("");
                    return;
                    
                case KEY_REGION:
                    SetRegion("");
                    return;

                case KEY_ACHIEVEMENTS:
                    Instance.m_Data[key] = new Dictionary<string, int>() { };
                    break;

                case KEY_ACHIEVEMENT_REWARDS:
                    Instance.m_Data[key] = new Dictionary<EAchievementReward, List<string>>()
                    {
                        { EAchievementReward.Avatar,    new List<string>() { EAvatar.None.ToString() }   },
                        { EAchievementReward.Border,    new List<string>() { EBorder.None.ToString() }   },
                        { EAchievementReward.Title,     new List<string>() { ETitle.None.ToString() }    },
                        { EAchievementReward.Badge,     new List<string>() { EBadge.None.ToString() }    },
                    };

                    Instance.m_Badges = Instance.FilterHighestLeague(EAchievementReward.Badge);
                    break;

                case KEY_GIFT_CODES:
                    Instance.m_Data[key] = new List<string>();
                    break;

                case KEY_IS_ADMIN:
                    Instance.m_Data[key] = false;
                    break;

                default:
                    ErrorHandler.Warning("Unknown key to reset : " +  key);
                    return;
            }

            if (save)
                Instance.SaveValue(key);
        }

        public override bool IsUnlockable(string key)
        {
            return new List<string>() { KEY_ACHIEVEMENT_REWARDS }.Contains(key) ;  
        }

        public override void Unlock(string key, bool save = true)
        {
            switch (key)
            {
                case KEY_ACHIEVEMENT_REWARDS:
                    foreach (EAchievementReward ar in Enum.GetValues(typeof(EAchievementReward)))
                    {
                        if (ar == EAchievementReward.None)
                            continue;

                        UnlockAchivementRewardAll(ar, false);
                    }

                    Instance.m_Badges = Instance.FilterHighestLeague(EAchievementReward.Badge);
                    break;

                default:
                    ErrorHandler.Warning("Unknown key to reset : " + key);
                    return;
            }

            if (save)
                SaveValue(key);
        }

        public void UnlockAchivementRewardAll(EAchievementReward ar, bool save = true)
        {
            foreach (Enum value in Enum.GetValues(GetTypeOf(ar)))
            {
                string name = value.ToString();

                if (ar != EAchievementReward.Badge)
                {
                    ProfileCloudData.AddAchievementReward(ar, value.ToString(), false);
                }

                else 
                { 
                    foreach (ELeague league in Enum.GetValues(typeof(ELeague)))
                    {
                        string badgeName = ProfileCloudData.BadgeToString((EBadge)value, league);

                        // check icon exists
                        if (AssetLoader.LoadBadgeIcon(badgeName) == null)
                            continue;

                        ProfileCloudData.AddAchievementReward(EAchievementReward.Badge, badgeName, false);
                    }
                }

            }

            if (save)
                SaveValue(KEY_ACHIEVEMENT_REWARDS);
        }

        #endregion


        #region Checkers

        void CheckAchievements()
        {
            if (! m_Data.ContainsKey(KEY_ACHIEVEMENTS) || Achievements == null)
            {
                ErrorHandler.Warning("Current Data are empty : use default ones");
                Reset(KEY_ACHIEVEMENTS); 
            }
        }

        void CheckAchievementRewards()
        {
            if (!m_Data.ContainsKey(KEY_ACHIEVEMENT_REWARDS) || AchievementRewards == null || AchievementRewards.Count == 0)
            {
                ErrorHandler.Warning("Achievement Rewards are empty : use default ones");
                Reset(KEY_ACHIEVEMENT_REWARDS);
            }

            Dictionary<EAchievementReward, List<string>> valuesToRemove     = new();
            Dictionary<EAchievementReward, List<string>> duplicatesToRemove = new();

            foreach (var item in AchievementRewards)
            {
                // init list of strings
                valuesToRemove[item.Key] = new();
                duplicatesToRemove[item.Key] = new();
                List<string> seenValues = new();

                // get all enums for this type
                var enumNames = Enum.GetNames(GetTypeOf(item.Key));

                // get threw all values in the database and check that they all matches
                foreach (string value in item.Value)
                {
                    bool test = true;

                    if (seenValues.Contains(value)) 
                    {
                        ErrorHandler.Error("Value " + value + " in " + item.Key + " is duplicated");
                        duplicatesToRemove[item.Key].Add(value);
                    }

                    seenValues.Add(value);

                    if (item.Key == EAchievementReward.Badge)
                    {
                        test &= TryGetBadgeFromString(value, out EBadge _, out ELeague _);
                    }

                    else if (!enumNames.Contains(value))
                    {
                        test = false;
                    }

                    if (! test)
                    {
                        ErrorHandler.Error("Unable to find " + value + " in EnumValues of " + item.Key);
                        valuesToRemove[item.Key].Add(value);
                    }
                }
            }

            // REMOVE VALUES
            foreach (var item in valuesToRemove)
                foreach (var value in item.Value)
                    RemoveAchievementReward(item.Key, value, false);

            // TODO : REMOVE DUPLICATES

            // SAVE 
            Instance.SaveValue(KEY_ACHIEVEMENT_REWARDS);
        }

        void CheckCurrentData()
        {
            if (!m_Data.ContainsKey(KEY_CURRENT_PROFILE_DATA))
            {
                ErrorHandler.Warning("Current Profile Data are empty : use default ones");
                Reset(KEY_CURRENT_PROFILE_DATA);
                return;
            }

            var data = CurrentProfileData;
            data.Check();
            Instance.SetData(KEY_CURRENT_PROFILE_DATA, data);
        }

        void CheckGiftCodes()
        {
            if (! m_Data.ContainsKey(KEY_GIFT_CODES) || GiftCodes == null)
            {
                ErrorHandler.Warning(KEY_GIFT_CODES + " are empty : reset value");
                Reset(KEY_GIFT_CODES);
                return;
            }
        }

        public async Task<EntityData> FindPlayerWithValue(string key, string value)
        {
            var query = new Query(
                new List<FieldFilter>() {
                    new FieldFilter(key, value, FieldFilter.OpOptions.EQ, true)
                },
                new HashSet<string> { KEY_TOKEN, KEY_GAMER_TAG, KEY_REGION }
            );

            List<EntityData> results = new List<EntityData>();
            try
            {
                results = await CloudSaveService.Instance.Data.Player.QueryAsync(query, new QueryOptions());
            }
            catch (Exception ex)
            {
                ErrorHandler.Error(ex.Message);
                ErrorHandler.Error("    + key : " + key);
                ErrorHandler.Error("    + value " + value);
                return null;
            }

            if (results.Count == 0)
                return null;

            if (results.Count > 1)
            {
                ErrorHandler.Error("Found multiple players (" + results.Count + ") using same key "+ key + " (" + value + ")");
            }

            return results[0];
        }

        /// <summary>
        /// Get a list of each players PublicData
        /// </summary>
        /// <returns></returns>
        public static async Task<List<SPublicProfileData>> GetAllPlayersPublicData(bool validOnly = true)
        {
            int nResults = 10;
            List<SPublicProfileData> results = new List<SPublicProfileData>();

            for (int i = 0; i < 999999; i++)
            {
                var query = new Query(
                    fields: default,
                    new HashSet<string> { KEY_GAMER_TAG, KEY_TAG, KEY_TOKEN, KEY_REGION },
                    limit: nResults,
                    offset: nResults * i
                );

                var returnedData = await CloudSaveService.Instance.Data.Player.QueryAsync(query, new QueryOptions());

                if (returnedData.Count == 0)
                    return results;

                foreach (var entityData in returnedData)
                {
                    var profileData = new SPublicProfileData(entityData);
                    if (validOnly && ! profileData.IsValid)
                        continue;
                    results.Add(profileData);
                }
            }

            return results;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            ProfileCloudData.BadgeUnlockedEvent += OnBadgeUnlocked;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProfileCloudData.BadgeUnlockedEvent -= OnBadgeUnlocked;
        }

        protected override void CheckData()
        {
            base.CheckData();

            CheckAchievementRewards();
            m_Badges = FilterHighestLeague(EAchievementReward.Badge);
            CheckAchievements();
            CheckCurrentData();
            CheckGiftCodes();
        }

        /// <summary>
        /// When a badge is unlocked, check if is in current build data. If so, update the league value
        /// </summary>
        /// <param name="badge"></param>
        /// <param name="league"></param>
        void OnBadgeUnlocked(EBadge badge, ELeague league)
        {
            for (int index=0; index < CurrentBadges.Length; index++)
            {
                string badgeName = CurrentBadges[index];
                if (!TryGetBadgeFromString(badgeName, out EBadge currentBadge, out ELeague currentLeague))
                    continue;

                // check if is same badge type
                if (badge != currentBadge)
                    continue;

                if (currentLeague >= league)
                {
                    ErrorHandler.Error("Badge " + badge + " unlocked with league " + league + " but is currently set with a league equal or higher : " + currentLeague);
                    continue;
                }

                SetCurrentBadge(currentBadge, index);
                return;
            }
        }

        #endregion
    }
}