using Assets;
using Assets.Scripts.Managers;
using Assets.Scripts.Save.RSDs;
using Data.GameManagement;
using Enums;
using MyBox;
using System;
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
                return (false, "Code has already been used");
            }
            codeData = data[0];

            return (true, null);
        }

        public async Task<(bool, string)> Collect(SPromoCodeData codeData)
        {
            if (! await ProfileCloudData.AddGiftCode(codeData.Code))
                return (false, "Unable to reach the database");

            if (HandleSpecialCases(codeData.Code))
                return (true, null);

            ScreenManager.DisplayRewards(codeData.Rewards, "PromoCode");
            return (true, null);
        }

        public bool HandleSpecialCases(string code)
        {
            if (code == ESpecialGiftCodes.FULLUNLOCK.ToString())
            {
                InventoryCloudData.Instance.UnlockAllCollectables();
                return true;
            }

            if (code == ESpecialGiftCodes.MEOWMEOW.ToString())
            {
                InventoryCloudData.Instance.Unlock(InventoryCloudData.KEY_RUNES);

                var rewards = new SRewardsData();
                rewards.Add(EChest.Common,  2);
                rewards.Add(EChest.Rare,    1);
                rewards.Add(EChest.Epic,    1);
                ScreenManager.DisplayRewards(rewards, "BugFix");
                return true;
            }

            return false;
        }

        #endregion
    }
}