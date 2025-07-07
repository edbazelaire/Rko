using Data.GameManagement;
using Enums;
using Game.Loaders;
using Menu;
using MyBox;
using Save;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Managers.Bots
{
    public static class AchievementGenerator
    {
        public static string    Avatar;
        public static string    Border;
        public static string[]  Badges;
        public static string    Title;

        static float NHours;

        static Dictionary<string, int> AchievementValues = new Dictionary<string, int>()
        {
            { "PlayedGames",        0 },
            { "Wins",               0 },
            { "SoloGamesPlayed",    0 },
            { "Damage",             0 },
            { "Heals",              0 },
            { "GoldCollected",      0 },
        };

        public static void Generate(ELeague league, float botPlaytimeHours, int level)
        {
            AchievementValues["PlayedGames"]        = (int)Mathf.Round(NHours * Random.Range(8f, 11f));
            AchievementValues["Wins"]               = (int)Mathf.Round(AchievementValues["PlayedGames"] * Random.Range(0.5f, 1f));
            AchievementValues["SoloGamePlayed"]     = (int)Mathf.Round(AchievementValues["PlayedGames"] * Random.Range(0f, 0.8f));
            AchievementValues["Damage"]             = (int)Mathf.Round(AchievementValues["PlayedGames"] * Mathf.Pow(Random.Range(500, 800), 1 + level * 0.15f));
            AchievementValues["Heals"]              = (int)Mathf.Round(AchievementValues["PlayedGames"] * Mathf.Pow(Random.Range(200, 400), 1 + level * 0.15f));
            AchievementValues["GoldCollected"]      = (int)Mathf.Round(botPlaytimeHours * Random.Range(2000, 3000));

            Dictionary<string, float>   avatars = new () { { "None", 0.2f } };
            Dictionary<string, float>   borders = new () { { "None", 0.2f } };
            Dictionary<string, float>   badges  = new () { { "None", 0.2f } };
            Dictionary<string, float>   titles  = new () { { "None", 0.2f } };

            // add current league's border
            if (league >= ELeague.Bronze)
                borders.Add("League" + league.ToString(), Mathf.Pow(2, (int)league));

            // Collect rewards in all achivements
            foreach (var achievementData in AchievementLoader.Achievements)
            {
                // if not generated achievement value - skip
                if (! AchievementValues.ContainsKey(achievementData.Name))
                {
                    continue;
                }

                int value = AchievementValues[achievementData.Name];
                float weight = 1f;
                foreach (var data in achievementData.AchievementSubData)
                {
                    if (data.MaxValue > value)
                        break;

                    if (data.Rewards.AchievementRewards.Count == 0)
                        continue;

                    // higher tiers reward : triple the weight
                    weight *= 3;

                    // EXTRACT REWARDS 
                    foreach (var reward in data.Rewards.AchievementRewards)
                    {
                        switch (reward.AchievementReward)
                        {
                            // no weight increase for avatars
                            case EAchievementReward.Avatar:
                                avatars.Add(reward.Value.ToString(), 1f);
                                break;

                            case EAchievementReward.Border:
                                borders.Add(reward.Value.ToString(), weight);
                                break;

                            case EAchievementReward.Badge:
                                // remove previous badge tiers from list of badges
                                if (reward.League > ELeague.Bronze)
                                    badges.Remove(ProfileCloudData.BadgeToString((EBadge)reward.EnumValue, reward.League - 1));

                                badges.Add(ProfileCloudData.BadgeToString((EBadge)reward.EnumValue, reward.League), weight);
                                break;

                            case EAchievementReward.Title:
                                titles.Add(reward.Value.ToString(), weight);
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            // -----------------------------------------------------------------------------
            // generate data from rewards
            Avatar  = PickFromWeight(avatars);
            Border  = PickFromWeight(borders);
            Badges  = PickFromWeight(badges, 3);
            Title   = PickFromWeight(titles);
        }

        static string PickFromWeight(Dictionary<string, float> weights)
        {
            if (weights == null || weights.Count == 0)
                return string.Empty;

            // Calculate the total weight
            float totalWeight = weights.Values.Sum();

            float randomPoint = Random.Range(0, totalWeight);
            foreach (var item in weights)
            {
                if (randomPoint < item.Value)
                    return item.Key;

                randomPoint -= item.Value;
            }

            // Fallback, should normally not reach this point
            return weights.Keys.Last();
        }

        static string[] PickFromWeight(Dictionary<string, float> weights, int nPicks)
        {
            if (weights == null || weights.Count == 0 || nPicks <= 0)
                return new string[0];

            List<string> picked = new();
            for (int i = 0; i < nPicks; i++)
            {
                var filteredWeights = weights
                    .Where(kv => kv.Key == "None" || !picked.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                if (filteredWeights.Count == 0)
                    break;

                string pick = PickFromWeight(filteredWeights);
                picked.Add(pick);
            }

            return picked.ToArray();
        }
    }
}