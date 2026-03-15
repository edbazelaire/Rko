using Data;
using Enums;
using Save;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tools;
using UnityEngine;
using UnityEngine.Networking;

namespace Network
{
    public static class InGameEventsApiConfig
    {
        // Plug-and-play switch: disable this to unplug integration.
        public static bool Enabled = true;
        public static bool AllowUnsignedRequests = false;
        public static int RequestTimeoutSeconds = 15;

        // Fill these to activate API calls.
        //public static string BaseUrl = "http://localhost:8080";
        public static string BaseUrl = "https://web-production-d510a.up.railway.app";
        public static string SharedSecret   = "";

        public static string ResolveDiscordId()
        {
            return ProfileCloudData.DiscordId;
        }
    }

    public static class InGameEventsApiClient
    {
        const string EVENT_TYPE_ACHIEVEMENT_UNLOCKED = "achievement_unlocked";
        const string EVENT_TYPE_DAILY_REWARD_COLLECTED = "daily_reward_collected";

        public struct ApiResponse
        {
            public bool Ok;
            public long StatusCode;
            public string ResponseBody;

            public ApiResponse(bool ok, long statusCode, string responseBody)
            {
                Ok = ok;
                StatusCode = statusCode;
                ResponseBody = responseBody ?? "";
            }
        }

        [Serializable]
        class EventRequestBody
        {
            public string event_id;
            public string event_type;
            public string player_game_id;
            public AchievementUnlockedPayload payload;
        }

        [Serializable]
        class AchievementUnlockedPayload
        {
            public string arena;
            public string difficulty;
            public List<string> mods;
        }

        [Serializable]
        class DailyRewardCollectedPayload
        {
            public string collect_day_utc;
            public int streak;
        }

        [Serializable]
        class DailyRewardRequestBody
        {
            public string event_id;
            public string event_type;
            public string player_game_id;
            public DailyRewardCollectedPayload payload;
        }

        [Serializable]
        class RewardCollectRequestBody
        {
            public string gamer_id;
            public string reward_id;
        }

        public static async Task<ApiResponse> SendArenaAchievementUnlockedEventAsync(ArenaAchievementData arenaAchievement)
        {
            if (!InGameEventsApiConfig.Enabled || arenaAchievement == null)
                return new ApiResponse(false, 0, "api_disabled_or_null_achievement");

            return await SendArenaAchievementUnlockedEventAsync(
                arenaAchievement.ArenaType,
                arenaAchievement.ArenaDifficulty,
                arenaAchievement.ArenaMods
            );
        }

        public static async Task<ApiResponse> SendArenaAchievementUnlockedEventAsync(EArenaType arenaType, EArenaDifficulty arenaDifficulty, List<EArenaMod> arenaMods)
        {
            if (!InGameEventsApiConfig.Enabled)
                return new ApiResponse(false, 0, "api_disabled");

            if (string.IsNullOrWhiteSpace(InGameEventsApiConfig.BaseUrl))
            {
                ErrorHandler.Warning("InGameEvents API disabled: BaseUrl is empty.");
                return new ApiResponse(false, 0, "missing_base_url");
            }

            string playerGameId = InGameEventsApiConfig.ResolveDiscordId();
            if (string.IsNullOrWhiteSpace(playerGameId))
            {
                ErrorHandler.Warning("InGameEvents API skipped: player_game_id is empty.");
                return new ApiResponse(false, 0, "missing_discord_id");
            }

            var mods = MapMods(arenaMods);
            var requestBody = new EventRequestBody
            {
                event_id = Guid.NewGuid().ToString(),
                event_type = EVENT_TYPE_ACHIEVEMENT_UNLOCKED,
                player_game_id = playerGameId,
                payload = new AchievementUnlockedPayload
                {
                    arena = arenaType.ToString(),
                    difficulty = arenaDifficulty.ToString(),
                    mods = mods
                }
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            return await PostEventAsync(jsonBody);
        }

        public static async Task<ApiResponse> SendDailyRewardCollectedEventAsync(int streak)
        {
            if (!InGameEventsApiConfig.Enabled)
                return new ApiResponse(false, 0, "api_disabled");

            if (string.IsNullOrWhiteSpace(InGameEventsApiConfig.BaseUrl))
            {
                ErrorHandler.Warning("InGameEvents API disabled: BaseUrl is empty.");
                return new ApiResponse(false, 0, "missing_base_url");
            }

            string playerGameId = InGameEventsApiConfig.ResolveDiscordId();
            if (string.IsNullOrWhiteSpace(playerGameId))
            {
                ErrorHandler.Warning("InGameEvents API skipped: player_game_id is empty.");
                return new ApiResponse(false, 0, "missing_discord_id");
            }

            var requestBody = new DailyRewardRequestBody
            {
                event_id = Guid.NewGuid().ToString(),
                event_type = EVENT_TYPE_DAILY_REWARD_COLLECTED,
                player_game_id = playerGameId,
                payload = new DailyRewardCollectedPayload
                {
                    collect_day_utc = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    streak = streak
                }
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            return await PostEventAsync(jsonBody);
        }

        public static async Task<ApiResponse> GetPendingRewardsAsync()
        {
            if (!InGameEventsApiConfig.Enabled)
                return new ApiResponse(false, 0, "api_disabled");

            if (string.IsNullOrWhiteSpace(InGameEventsApiConfig.BaseUrl))
            {
                ErrorHandler.Warning("InGameEvents API disabled: BaseUrl is empty.");
                return new ApiResponse(false, 0, "missing_base_url");
            }

            string gamerId = InGameEventsApiConfig.ResolveDiscordId();
            if (string.IsNullOrWhiteSpace(gamerId))
            {
                ErrorHandler.Warning("InGameEvents API skipped: gamer_id is empty.");
                return new ApiResponse(false, 0, "missing_discord_id");
            }

            string endpoint = InGameEventsApiConfig.BaseUrl.TrimEnd('/') + "/rewards/" + UnityWebRequest.EscapeURL(gamerId) + "?collected=false";
            return await SendRequestAsync(endpoint, UnityWebRequest.kHttpVerbGET);
        }

        public static async Task<ApiResponse> CollectRewardAsync(string gamerId, string rewardId)
        {
            if (!InGameEventsApiConfig.Enabled)
                return new ApiResponse(false, 0, "api_disabled");

            if (string.IsNullOrWhiteSpace(InGameEventsApiConfig.BaseUrl))
            {
                ErrorHandler.Warning("InGameEvents API disabled: BaseUrl is empty.");
                return new ApiResponse(false, 0, "missing_base_url");
            }

            if (string.IsNullOrWhiteSpace(gamerId))
            {
                ErrorHandler.Warning("InGameEvents API skipped: gamer_id is empty.");
                return new ApiResponse(false, 0, "missing_discord_id");
            }

            if (string.IsNullOrWhiteSpace(rewardId))
            {
                ErrorHandler.Warning("InGameEvents API skipped: reward_id is empty.");
                return new ApiResponse(false, 0, "missing_reward_id");
            }

            string normalizedGamerId = gamerId.Trim().ToUpperInvariant();
            string normalizedRewardId = rewardId.Trim();

            var requestBody = new RewardCollectRequestBody
            {
                gamer_id = normalizedGamerId,
                reward_id = normalizedRewardId
            };

            string endpoint = InGameEventsApiConfig.BaseUrl.TrimEnd('/') + "/rewards";
            string jsonBody = JsonUtility.ToJson(requestBody);
            var response = await SendRequestAsync(endpoint, UnityWebRequest.kHttpVerbPOST, jsonBody);
            if (!response.Ok)
                ErrorHandler.Warning("CollectRewardAsync failed with payload " + jsonBody + " response=" + response.ResponseBody);

            return response;
        }

        public static async Task<ApiResponse> CollectRewardAsync(string rewardId)
        {
            return await CollectRewardAsync(InGameEventsApiConfig.ResolveDiscordId(), rewardId);
        }

        static async Task<ApiResponse> PostEventAsync(string jsonBody)
        {
            string endpoint = InGameEventsApiConfig.BaseUrl.TrimEnd('/') + "/v1/events";
            return await SendRequestAsync(endpoint, UnityWebRequest.kHttpVerbPOST, jsonBody);
        }

        static async Task<ApiResponse> SendRequestAsync(string endpoint, string method, string jsonBody = null)
        {
            using (var req = new UnityWebRequest(endpoint, method))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = InGameEventsApiConfig.RequestTimeoutSeconds;

                if (!string.IsNullOrWhiteSpace(jsonBody))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.SetRequestHeader("Content-Type", "application/json");
                }

                if (!InGameEventsApiConfig.AllowUnsignedRequests)
                {
                    //if (string.IsNullOrWhiteSpace(InGameEventsApiConfig.SharedSecret))
                    //{
                    //    ErrorHandler.Warning("InGameEvents API skipped: SharedSecret is empty.");
                    //    return new ApiResponse(false, 0, "missing_shared_secret");
                    //}

                    string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    string signedPayload = timestamp + "." + (jsonBody ?? "");
                    //string signature = ComputeHmacSha256Hex(InGameEventsApiConfig.SharedSecret, signedPayload);

                    req.SetRequestHeader("X-Timestamp", timestamp);
                    //req.SetRequestHeader("X-Signature", signature);
                }

                var op = req.SendWebRequest();
                while (!op.isDone)
                    await Task.Yield();

                string responseBody = req.downloadHandler != null ? req.downloadHandler.text : "";
                string networkError = req.error ?? "";

                if (req.result != UnityWebRequest.Result.Success)
                {
                    string details = string.IsNullOrWhiteSpace(responseBody) ? networkError : responseBody;
                    if (string.IsNullOrWhiteSpace(details))
                        details = "unknown_network_error";

                    ErrorHandler.Warning(
                        "InGameEvents API error (" + req.responseCode + ") [" + endpoint + "]: " + details
                    );
                    return new ApiResponse(false, req.responseCode, details);
                }

                ErrorHandler.Log(() => "InGameEvents API success: " + responseBody, ELogTag.System);
                return new ApiResponse(true, req.responseCode, responseBody);
            }
        }

        static List<string> MapMods(List<EArenaMod> arenaMods)
        {
            var mapped = new List<string>();
            if (arenaMods == null)
                return mapped;

            foreach (var mod in arenaMods)
            {
                if (mod == EArenaMod.None)
                    continue;

                // Keep in-game values but normalize known legacy casing for API compatibility.
                if (mod == EArenaMod.HardCore)
                    mapped.Add("Hardcore");
                else
                    mapped.Add(mod.ToString());
            }

            return mapped;
        }

        static string ComputeHmacSha256Hex(string secret, string message)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
