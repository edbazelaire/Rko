using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using UnityEngine;

namespace Assets.Scripts.Save.RSDs
{
    public static class OAuthTokenGenerator
    {
        private static string m_ServiceAccountFilePath = Path.Combine(Application.streamingAssetsPath, "google_service_account.json");

        public static async Task<string> GetAccessToken()
        {
            Debug.Log($"📡 Checking Service Account JSON Path: {m_ServiceAccountFilePath}");

            if (!File.Exists(m_ServiceAccountFilePath))
            {
                Debug.LogError($"❌ ERROR: Service Account JSON file is missing at: {m_ServiceAccountFilePath}");
                return null;
            }

            try
            {
                Debug.Log("📖 Reading JSON file...");
                string jsonContent = File.ReadAllText(m_ServiceAccountFilePath);
                Debug.Log($"📖 JSON Content: {jsonContent.Substring(0, 100)}...");  // Log first 100 chars to confirm it's correct

                using (var stream = new FileStream(m_ServiceAccountFilePath, FileMode.Open, FileAccess.Read))
                {
                    var credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);

                    var tokenResponse = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                    if (string.IsNullOrEmpty(tokenResponse))
                    {
                        Debug.LogError("❌ ERROR: OAuth Token is empty!");
                        return null;
                    }

                    Debug.Log($"✅ OAuth Token retrieved successfully: {tokenResponse.Substring(0, 20)}...");
                    return tokenResponse;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ ERROR: Failed to retrieve OAuth Token: {ex.Message}");
                return null;
            }
        }
    }

}