using System.Collections.Generic;
using Tools;
using Unity.Services.Relay.Models;

namespace Save.RSDs
{
    public class TokenData : RSDData
    {
        public string Token;
        public string Name;
        public bool IsAdmin;
    }

    public class TokensRSD : RSD
    {
        #region Members

        public new static TokensRSD Instance => RSDManager.GetRSD<TokensRSD>();

        protected override string m_SheetId => "1xzYKzmTha3LlA2uX_gzLUpDEBurEmSz-2qsuKhsyoLk";
        protected override string m_SheetName => "Tokens";
        public override List<TokenData> Data { get; set; }

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Read and store data received from the sheets
        /// </summary>
        /// <param name="data"></param>
        protected override void ReadSheetsData(SSheetsData data) { Data = FormatSheetData<TokenData>(data); }

        #endregion


        #region Tokens

        /// <summary>
        /// Check if token exists in list of authorized tokens
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool IsTokenAuthorized(string token)
        {
            // special Apple testing re-useable token
            if (token == "ZrsaGG8j2WNXXyj")
            {
                return true;    
            }

            foreach (TokenData data in Instance.Data)
            {
                if (token == data.Token)
                    return true;
            }

            return false;
        }

        public static bool IsTokenAdmin(string token)
        {
            foreach (TokenData data in Instance.Data)
            {
                if (token == data.Token)
                    return data.IsAdmin;
            }

            ErrorHandler.Error("Unable to find token " + token);
            return false;
        }

        #endregion
    }
}