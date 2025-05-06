using Enums;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Game
{
    public struct SHitTypeValue : INetworkSerializable
    {
        public EHitType HitType;
        public int Value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HitType);
            serializer.SerializeValue(ref Value); ;
        }
    }


    public struct SSpellHitTypeData : INetworkSerializable
    {
        public string SpellName;
        public SHitTypeValue[] HitTypeValues;

        public SSpellHitTypeData(string spellName)
        {
            SpellName = spellName;
            HitTypeValues = new SHitTypeValue[Enum.GetValues(typeof(EHitType)).Length]; // Preallocate array with max possible HitTypes

            int i = 0;
            foreach (EHitType hitType in Enum.GetValues(typeof(EHitType)))
            {
                HitTypeValues[i] = new SHitTypeValue { HitType = hitType, Value = 0 };
                i++;
            }
        }

        public void AddHit(int qty, EHitType hitType)
        {
            // Check if the HitType already exists in the array
            var hitTypeIndex = Array.FindIndex(HitTypeValues, hit => hit.HitType == hitType);
            if (hitTypeIndex < 0)
            {
                ErrorHandler.Error("("+SpellName+") : Unable to find any HitTypeValue with HitType = " + hitType.ToString());
                return;    
            }

            // Increment the value for the existing HitType
            var temp = HitTypeValues[hitTypeIndex];
            temp.Value += qty;
            HitTypeValues[hitTypeIndex] = temp;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Serialize the SpellName
            serializer.SerializeValue(ref SpellName);

            // Serialize the array length
            int length = HitTypeValues != null ? HitTypeValues.Length : 0;
            serializer.SerializeValue(ref length);

            if (serializer.IsReader)
            {
                HitTypeValues = new SHitTypeValue[length];
            }

            // Serialize the HitTypeValues array
            for (int i = 0; i < length; i++)
            {
                HitTypeValues[i].NetworkSerialize(serializer); // Now using an array
            }
        }
    }


    public class GameAnalyticsManager : NetworkBehaviour
    {
        #region Singleton

        public static GameAnalyticsManager Instance => GameManager.Exists ? GameManager.Instance.GameAnalyticsManager : null;

        #endregion


        #region Members

        private Dictionary<ulong, List<SSpellHitTypeData>> m_PlayersDataDamage = new();

        #endregion


        #region Player Data Management

        public void InitializePlayerData(ulong playerId)
        {
            m_PlayersDataDamage[playerId] = new List<SSpellHitTypeData>();
        }

        public List<SSpellHitTypeData> GetPlayerData(ulong playerId)
        {
            if (m_PlayersDataDamage.TryGetValue(playerId, out var data))
                return data;

            ErrorHandler.Warning($"Player data for ID {playerId} not found.");
            return new List<SSpellHitTypeData>();
        }

        #endregion


        #region Damage

        public void OnSpellHit(ulong casterId, ulong targetId, string spellName, int qty, EHitType hitType, ESpellCategory spellCategory)
        {
            if (!m_PlayersDataDamage.ContainsKey(casterId))
                InitializePlayerData(casterId);

            // Find or create data for the spell
            var spellDataIndex = m_PlayersDataDamage[casterId].FindIndex(spellData => spellData.SpellName == spellName);

            if (spellDataIndex >= 0)
            {
                m_PlayersDataDamage[casterId][spellDataIndex].AddHit(qty, hitType);
            }
            else
            {
                var newSpellData = new SSpellHitTypeData(spellName);
                newSpellData.AddHit(qty, hitType);
                m_PlayersDataDamage[casterId].Add(newSpellData);
            }

            // Send data to damage displayer & damage client analytics
            DisplaySpellHitClientRPC(casterId, targetId, spellName, (ushort)qty, (byte)hitType, (byte)spellCategory);
        }

        [ClientRpc]
        public void DisplaySpellHitClientRPC(ulong casterClientId, ulong targetClientId, string spellName, ushort amount, byte hitTypeByte, byte spellCategoryByte)
        {
            var caster = GameManager.Instance.GetPlayer(casterClientId);

            EHitType hitType = (EHitType)hitTypeByte;
            ESpellCategory spellCategory = (ESpellCategory)spellCategoryByte;

            // Display to the DamageDisplayManager
            if (PlayerPrefs.GetInt("DisplayDamage", 1) == 1)
            {
                HitDisplayUI.Instance?.DisplayHit(targetClientId, amount, hitType, spellCategory);
            }

            // Send data to Analytics
            if (caster != null && caster.ClientAnalytics != null)
            {
                caster.ClientAnalytics.SendSpellData(spellName, hitType, amount);
            }
        }

        #endregion


        #region Sending / Receiving data

        public void SendGameAnalytics()
        {
            foreach (ulong playerId in m_PlayersDataDamage.Keys)
            {
                // Send the data to the player
                List<SSpellHitTypeData> playerData = m_PlayersDataDamage[playerId];
                SendPlayerDataClientRPC(playerData.ToArray(), playerId);
            }
        }

        /// <summary>
        /// Send the player data to the client
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="playerId"></param>
        [ClientRpc]
        private void SendPlayerDataClientRPC(SSpellHitTypeData[] playerData, ulong playerId)
        {
            // Handle the data on the client side
            if (!NetworkManager.Singleton.IsClient)
                return;

            if (playerId != NetworkManager.LocalClientId)
                return;

            // update the analytics data for end game display
            GameUIManager.EndGameUI.EndGameAnalyticsUI.UpdateAnalytics(playerData.ToList());
        }

        #endregion
    }
}
