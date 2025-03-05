using Assets;
using Assets.Scripts.Save.RSDs;
using Data.GameManagement;
using MyBox;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using UnityEngine;
using UnityEngine.Networking;

namespace Save.RSDs
{
    public class SPromoCodeData : RSDData
    {
        public string               Code;
        public SRewardsData         Rewards;
    }


    public class GiftCodeRSD : RSD<SPromoCodeData>
    {
        #region Members

        public new static GiftCodeRSD Instance => RSDManager.GetRSD<GiftCodeRSD>();

        protected override string m_SheetId => "1xzYKzmTha3LlA2uX_gzLUpDEBurEmSz-2qsuKhsyoLk";
        protected override string m_SheetName => "GiftCodes";

        #endregion

        #region Loading & Saving

        protected override void ReadSheetsData(SSheetsData data)
        {
            base.ReadSheetsData(data); // Ensures m_Data is properly set
        }

        #endregion


        #region Code Manipulation

        public (bool, string) IsPromoCodeValid(string code, out SPromoCodeData codeData)
        {
            codeData = null;

            if (m_Data.IsNullOrEmpty())
                return (false, "No RSD data");

            var data = m_Data.Where(t => t.Code == code).ToList();
            if (data.Count == 0)
            {
                return (false, "Incorrect Code");
            }

            if (data.Count > 1)
            {
                ErrorHandler.Warning("Found same code ("+code+") multiple times");
            }

            if (ProfileCloudData.HasGiftCode(data[0].Code))
            {
                return (false, "Code as already been used");
            }

            codeData = data[0];

            return (true, null);
        }

        public async Task<(bool, string)> Collect(SPromoCodeData codeData)
        {
            if (!await ProfileCloudData.AddGiftCode(codeData.Code))
                return (false, "Unable to reach the database");

            Main.DisplayRewards(codeData.Rewards, "PromoCode");
            return (true, null);
        }

        /// <summary>
        /// [OLD METHOD] - Remove or keep as example ?
        /// 
        /// This is how to update a value directly in the Sheets
        /// </summary>
        /// <param name="promoCode"></param>
        /// <returns></returns>
        IEnumerator MarkCodeAsUsed(SPromoCodeData promoCode)
        {
            int rowIndex = m_Data.FindIndex(t => t.Code == promoCode.Code);
            if (rowIndex == -1)
            {
                ErrorHandler.Error($"Could not find promo code {promoCode.Code} in local data!");
                yield break;
            }

            int sheetRowIndex = rowIndex + 2;
            string updateUrl = UpdateUrl(sheetRowIndex);

            string requestBody = "{ \"values\": [[\"TRUE\"]] }";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);

            // 🔹 FIXED: Properly wait for OAuth token retrieval
            Task<string> tokenTask = OAuthTokenGenerator.GetAccessToken();
            yield return new WaitUntil(() => tokenTask.IsCompleted); // Ensure Unity waits for the task

            string accessToken = tokenTask.Result; // Retrieve the token safely

            // CHECK Auth
            if (string.IsNullOrEmpty(accessToken))
            {
                ErrorHandler.Error("❌ Failed to retrieve OAuth token");
                yield break;
            }

            // Create request
            UnityWebRequest request = UnityWebRequest.Put(updateUrl, requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("Content-Type", "application/json");

            // Send request
            yield return request.SendWebRequest();

            // Check result
            if (request.result != UnityWebRequest.Result.Success)
            {
                ErrorHandler.Error($"❌ Failed to update promo code: {request.error}");
                ErrorHandler.Error($"📥 API Response: {request.downloadHandler.text}");
                yield break;
            }

            // Update value locally
            //promoCode.IsUsed = true;
        }

        #endregion
    }
}