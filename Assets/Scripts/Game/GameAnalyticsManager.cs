using Data;
using Data.GameManagement;
using Enums;
using Game;
using Game.Loaders;
using Managers;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Game
{
    /// <summary>
    /// Represents a single value of a hit with its type (Damage/Heal/Shield)
    /// and its category (Direct, Tick, Zone).
    /// </summary>
    public struct SHitDetailValue : INetworkSerializable
    {
        public EHitType HitType;            // Damage, Heal, Shield...
        public EHitCategory Category;       // Direct, Tick, Zone
        public int Value;                   // Amount

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HitType);
            serializer.SerializeValue(ref Category);
            serializer.SerializeValue(ref Value);
        }
    }

    /// <summary>
    /// Holds analytics data for a specific spell, including the breakdown
    /// of values per HitType and per SpellCategory.
    /// </summary>
    public struct SSpellHitTypeData : INetworkSerializable
    {
        public string SpellName;
        public int Counter;
        public List<SHitDetailValue> HitDetails;

        public SSpellHitTypeData(string spellName)
        {
            SpellName = spellName;
            Counter = 0;
            HitDetails = new List<SHitDetailValue>();
        }

        /// <summary>
        /// Increase counter
        /// </summary>
        public void IncreaseCounter(int counter = 1)
        {
            Counter += counter;
        }

        /// <summary>
        /// Add a hit value for the given type and category.
        /// If an entry already exists, increment it. Otherwise create one.
        /// </summary>
        public void AddHit(int qty, EHitType hitType, EHitCategory category)
        {
            int index = HitDetails.FindIndex(h => h.HitType == hitType && h.Category == category);
            if (index >= 0)
            {
                var temp = HitDetails[index];
                temp.Value += qty;
                HitDetails[index] = temp;
            }
            else
            {
                HitDetails.Add(new SHitDetailValue
                {
                    HitType = hitType,
                    Category = category,
                    Value = qty
                });
            }
        }

        /// <summary>
        /// Get the total amount for a given hit type (sum of Direct + Tick).
        /// </summary>
        public int GetTotal(EHitType hitType)
        {
            if (HitDetails.IsNullOrEmpty())
                return 0;
            return HitDetails.Where(h => h.HitType == hitType).Sum(h => h.Value);
        }

        /// <summary>
        /// Get the amount for a given hit type and spell category.
        /// </summary>
        public int GetCategoryValue(EHitType hitType, EHitCategory category)
        {
            if (HitDetails.IsNullOrEmpty())
                return 0;
            return HitDetails.Where(h => h.HitType == hitType && h.Category == category).Sum(h => h.Value);
        }

        /// <summary>
        /// Get all values of the requested type of hit, but split with each value/color for each category
        /// </summary>
        /// <param name="hitType"></param>
        /// <returns></returns>
        public List<(int, Color)> GetCategoryValuesSplitted(EHitType hitType)
        {
            List<(int, Color)> list = new();
            if (HitDetails.IsNullOrEmpty())
                return list;

            var hits = HitDetails.Where(h => h.HitType == hitType);
            if (hits.IsNullOrEmpty())
                return list;

            foreach (var hitDetail in hits)
            {
                list.Add((hitDetail.Value, PlayerSettings.GetHitTypeColor(hitType, hitDetail.Category)));
            }

            return list;
        }

        /// <summary>
        /// Required for network serialization.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SpellName);

            int count = HitDetails != null ? HitDetails.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsReader)
                HitDetails = new List<SHitDetailValue>(count);

            for (int i = 0; i < count; i++)
            {
                if (serializer.IsReader)
                {
                    var detail = new SHitDetailValue();
                    detail.NetworkSerialize(serializer);
                    HitDetails.Add(detail);
                }
                else
                {
                    var detail = HitDetails[i];
                    detail.NetworkSerialize(serializer);
                }
            }
        }
    }

    /// <summary>
    /// Manages all the analytics data for the match.
    /// Stores per-player spell hit data and synchronizes them via Netcode.
    /// </summary>
    public class GameAnalyticsManager : NetworkBehaviour
    {
        #region Singleton
        public static GameAnalyticsManager Instance => GameManager.Exists ? GameManager.Instance.GameAnalyticsManager : null;

        // -- local data
        ulong m_PlayerId;
        SPlayerData m_PlayerData;

        #endregion


        #region Members

        /// <summary>
        /// Special Values (resistance, ...) saved for end game analyse
        /// </summary>
        public Dictionary<ulong, Dictionary<ESpecialValue, float>> m_PlayersSpecialValues = new();
        
        /// <summary>
        /// List of damages/heals/... for each spells of each player
        /// </summary>
        private Dictionary<ulong, List<SSpellHitTypeData>>  m_PlayersDataDamage = new();

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

        /// <summary>
        /// [LOCAL] Add local build data that can be usefull 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="controller"></param>
        public void SetUpLocalData(ulong clientId, SPlayerData sPlayerData)
        {
            m_PlayerId = clientId;
            m_PlayerData = sPlayerData;
        }

        #endregion


        #region Special Info

        public void AddSpecialValue(ulong casterId, ESpecialValue specialValueName, float value)
        {
            if (!m_PlayersSpecialValues.ContainsKey(casterId))
                m_PlayersSpecialValues.Add(casterId, new());

            if (! m_PlayersSpecialValues[casterId].ContainsKey(specialValueName))
                m_PlayersSpecialValues[casterId].Add(specialValueName, 0f);

            m_PlayersSpecialValues[casterId][specialValueName] += value;
        }

        #endregion


        #region Data Collection

        public void IncreaseCounter(ulong casterId, string spellName, int counter = 1)
        {
            if (!m_PlayersDataDamage.ContainsKey(casterId))
                InitializePlayerData(casterId);

            // Find or create data for this spell
            var spellDataIndex = m_PlayersDataDamage[casterId].FindIndex(spellData => spellData.SpellName == spellName);

            SSpellHitTypeData spellHitTypeData;
            if (spellDataIndex >= 0)
            {
                spellHitTypeData = m_PlayersDataDamage[casterId][spellDataIndex];
                spellHitTypeData.IncreaseCounter(counter);
                m_PlayersDataDamage[casterId][spellDataIndex] = spellHitTypeData;
            }
            else
            {
                spellHitTypeData = new SSpellHitTypeData(spellName);
                spellHitTypeData.IncreaseCounter(counter);
                m_PlayersDataDamage[casterId].Add(spellHitTypeData);
            }
        }

        public void OnSpellHit(ulong casterId, ulong targetId, string spellName, int qty, EHitType hitType, EHitCategory category)
        {
            if (!m_PlayersDataDamage.ContainsKey(casterId))
                InitializePlayerData(casterId);

            // Find or create data for this spell
            var spellDataIndex = m_PlayersDataDamage[casterId].FindIndex(spellData => spellData.SpellName == spellName);

            if (spellDataIndex >= 0)
            {
                m_PlayersDataDamage[casterId][spellDataIndex].AddHit(qty, hitType, category);
            }
            else
            {
                var newSpellData = new SSpellHitTypeData(spellName);
                newSpellData.AddHit(qty, hitType, category);
                m_PlayersDataDamage[casterId].Add(newSpellData);
            }

            // Send hit info to UI
            DisplaySpellHitClientRPC(casterId, targetId, spellName, (ushort)qty, (byte)hitType, (byte)category);
        }

        [ClientRpc]
        public void DisplaySpellHitClientRPC(ulong casterClientId, ulong targetClientId, string spellName, ushort amount, byte hitTypeByte, byte spellCategoryByte)
        {
            var caster = GameManager.Instance.GetPlayer(casterClientId);

            EHitType hitType = (EHitType)hitTypeByte;
            EHitCategory spellCategory = (EHitCategory)spellCategoryByte;

            // Floating damage text
            if (PlayerPrefs.GetInt("DisplayDamage", 1) == 1)
            {
                HitDisplayUI.Instance?.DisplayHit(targetClientId, amount, hitType, spellCategory);
            }
        }
        
        #endregion


        #region Data Synchronization

        public void SendGameAnalytics()
        {
            // SEND special values
            foreach (ulong playerId in m_PlayersSpecialValues.Keys)
            {
                foreach (var item in m_PlayersSpecialValues[playerId])
                {
                    if (GameManager.Instance.IsOfflineMode)
                    {
                        SendSpecialValueData(playerId, 0, item.Key, item.Value);
                    }
                    else
                    {
                        SendSpecialValueClientRPC(playerId, item.Key, item.Value);
                    }
                }
            }

            // SEND classic player data
            foreach (ulong playerId in m_PlayersDataDamage.Keys)
            {
                List<SSpellHitTypeData> playerData = m_PlayersDataDamage[playerId];
                if (GameManager.Instance.IsOfflineMode)
                {
                    SendPlayerData(playerData.ToArray(), playerId, 0);
                }
                else
                {
                    SendPlayerDataClientRPC(playerData.ToArray(), playerId);
                }
            }
        }

        [ClientRpc]
        private void SendPlayerDataClientRPC(SSpellHitTypeData[] playerData, ulong playerId)
        {
            if (!NetworkManager.Singleton.IsClient)
                return;

            SendPlayerData(playerData, playerId, NetworkManager.LocalClientId);
        }

        private void SendPlayerData(SSpellHitTypeData[] playerData, ulong playerId, ulong myPlayerId)
        {
            GameUIManager.EndGameUI.EndGameAnalyticsUI.UpdateAnalytics(playerData.ToList(), index: playerId == myPlayerId ? 0 : 1);

            // TODO : OLD analytics method was copy/paste here - adapt to send game analytics AT THE END of the game
            // Send to client analytics
            //if (caster != null && caster.ClientAnalytics != null)
            //{
            //    caster.ClientAnalytics.SendSpellData(spellName, hitType, amount);
            //}
        }

        [ClientRpc]
        private void SendSpecialValueClientRPC(ulong playerId, ESpecialValue specialValue, float value)
        {
            if (!NetworkManager.Singleton.IsClient)
                return;

            SendSpecialValueData(playerId, NetworkManager.LocalClientId, specialValue, value);
        }

        void SendSpecialValueData(ulong playerId, ulong myPlayerId, ESpecialValue specialValue, float value)
        {
            GameUIManager.EndGameUI.EndGameAnalyticsUI.AddSpecialValue(specialValue, value, index: playerId == myPlayerId ? 0 : 1);
        }

        #endregion


        #region End Game Achievements

        public void CalculateAchievements()
        {
            if (!Enum.TryParse(m_PlayerData.BuildData.Character, out ECharacter character))
            {
                ErrorHandler.Error("Unable to parse " + m_PlayerData.BuildData.Character + " as character");
                return;
            }

            bool save = CalculateEndGameAchievements(character);
            save |= CalculatePropertyAchievements(character);
            save |= CalculateArenaAchievements(character);

            // save all achievements at once (if any has been incremented)
            if (save)
                ProfileCloudData.Instance.SaveValue(ProfileCloudData.KEY_ACHIEVEMENTS);
        }

        bool CalculateEndGameAchievements(ECharacter character)
        {
            bool test = false;
            foreach (var achievement in AchievementLoader.Get<EndGameAchievementData>(character))
            {
                test |= achievement.Check(EndGameUI.GameMode, EndGameUI.GameResult == EGameResult.Win, save: false);
            }

            return test;
        }

        bool CalculatePropertyAchievements(ECharacter character)
        {
            var endGameAnalytics = GameUIManager.EndGameUI.EndGameAnalyticsUI.GetDisplayer(0);
            if (endGameAnalytics == null)
                return false;

            bool test = false;
            foreach (var achievement in AchievementLoader.Get<PropertyAchievementData>(character))
            {
                test |= achievement.Check(endGameAnalytics.SpellHitSummary, endGameAnalytics.SpecialValues, endGameAnalytics.SpellHitTypeDatas, save: false);
            }

            return test;
        }

        bool CalculateArenaAchievements(ECharacter character)
        {
            if (EndGameUI.GameMode != EGameMode.Arena)
                return false;

            if (EndGameUI.GameResult != EGameResult.Win)
                return false;

            if (!ProgressionCloudData.CurrentArena.IsLastBoss())
                return false;

            bool test = false;
            foreach (ArenaAchievementData achievement in AchievementLoader.Get<ArenaAchievementData>(character))
            {
                test |= achievement.Check(
                    gameMode:           EndGameUI.GameMode, 
                    gameResult:         EndGameUI.GameResult, 
                    arenaType:          ProgressionCloudData.CurrentArena.ArenaType, 
                    arenaDifficulty:    ProgressionCloudData.CurrentArena.GetArenaDifficulty(), 
                    arenaMods:          ProgressionCloudData.CurrentArena.GetArenaMods(),
                    save:               false
                );
            }

            return test;
        }

        #endregion
    }
}
