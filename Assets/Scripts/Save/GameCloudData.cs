using Assets;
using Enums;
using Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Save
{
    [Serializable]
    public struct SPlayerTag
    {
        public string GamerTag;
        public string UnityID;
        public string Token;
    }

    public class GameCloudData: CloudData
    {
        #region Members

        public new static GameCloudData Instance => Main.CloudSaveManager.GameCloudData;

        // ===============================================================================================
        // CONSTANTS
        // KEYS ------------------------------------
        public const string KEY_PLAYER_TAGS     = "PlayerTags";
        public const string KEY_ALLOWED_TOKENS  = "AllowedTokens";

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_PLAYER_TAGS,          new List<SPlayerTag>()      },
            { KEY_ALLOWED_TOKENS,       new List<string>()          },
        };

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        public static List<SPlayerTag>  PlayerTags      => Instance.m_Data[KEY_PLAYER_TAGS] as List<SPlayerTag>;
        public static List<string>      AllowedTokens   => Instance.m_Data[KEY_ALLOWED_TOKENS] as List<string>;

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert SCharacterBuildsCloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <param name="charsBuildsList"></param>
        /// <returns></returns>
        protected override object Convert(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(List<SPlayerTag>))
                return item.Value.GetAs<List<SPlayerTag>>();

            return base.Convert(item);
        }

        #endregion


        #region Players

        public static void AddNewPlayer(string gamerTag, string token)
        {
            string unityId = AuthenticationService.Instance.PlayerId;
            if (IsUnityIdExisting(unityId, out SPlayerTag playerTag))
            {
                playerTag.GamerTag = gamerTag;
                playerTag.Token = token;
            }
        }

        #endregion


        #region Checkers

        static bool IsUnityIdExisting(string unityId,  out SPlayerTag playerTag)
        {
            foreach (SPlayerTag tag in PlayerTags) 
            {
                if (tag.UnityID == unityId)
                {
                    playerTag = tag;
                    return true;
                }
            }

            playerTag = new SPlayerTag();
            return false;
        }


        #endregion
    }
}