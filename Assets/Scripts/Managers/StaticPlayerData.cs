using Data;
using Data.DataStructures;
using Enums;
using Game;
using Save;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Structural data that can be provided to create a new character 
    /// </summary>
    [Serializable]
    public struct SBotData : INetworkSerializable
    {
        public float DecisionRefresh;
        public float Randomness;

        public SBotData(float decisionRefresh, float randomness)
        {
            DecisionRefresh = decisionRefresh;
            Randomness = randomness;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref DecisionRefresh);
            serializer.SerializeValue(ref Randomness);
        }
    }

    /// <summary>
    /// Structural data that can be provided to create a new character 
    /// </summary>
    [Serializable]
    public struct SPlayerData : INetworkSerializable
    {
        public FixedString32Bytes           PlayerName;
        public int                          CharacterLevel;
        public ECharacter                   Character;
        public ERune                        Rune;
        public ESpell[]                     Spells;
        public int[]                        SpellLevels;
        public SProfileDataNetwork          ProfileData;
        public bool                         IsPlayer;
        public STriggerEffect[]             TriggerEffects; 
        public SCharacterStatScaling[]      BonusStats; 
        public SBotData                     BotData; 

        public SPlayerData(FixedString32Bytes playerName, int characterLevel, ECharacter character, ERune rune, ESpell[] spells, int[] spellLevels, SProfileDataNetwork profileData, bool isPlayer, STriggerEffect[] triggerEffects = default, SCharacterStatScaling[] bonusStats = default, SBotData botData = default)
        {
            PlayerName      = playerName;
            CharacterLevel  = characterLevel;
            Character       = character;
            Rune            = rune;
            Spells          = spells;
            SpellLevels     = spellLevels;
            ProfileData     = profileData;
            IsPlayer        = isPlayer;
            TriggerEffects  = triggerEffects;
            BonusStats      = bonusStats;
            BotData         = botData;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // direct serialization
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref CharacterLevel);
            serializer.SerializeValue(ref Character);
            serializer.SerializeValue(ref Rune);
            serializer.SerializeValue(ref IsPlayer);

            // Sub - serialization
            ProfileData.NetworkSerialize(serializer);
            BotData.NetworkSerialize(serializer);

            // ARRAYS - serialization
            // -- Spells
            int length = Spells != null ? Spells.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                Spells = new ESpell[length];
            }
            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref Spells[i]);
            }

            // -- SpellLevels
            length = SpellLevels != null ? SpellLevels.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                SpellLevels = new int[length];
            }
            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref SpellLevels[i]);
            }

            // -- TriggerEffects
            length = TriggerEffects != null ? TriggerEffects.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                TriggerEffects = new STriggerEffect[length];
            }
            for (int i = 0; i < length; i++)
            {
                TriggerEffects[i].NetworkSerialize(serializer);
            }

            // -- BonusStats
            length = BonusStats != null ? BonusStats.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                BonusStats = new SCharacterStatScaling[length];
            }
            for (int i = 0; i < length; i++)
            {
                BonusStats[i].NetworkSerialize(serializer);
            }
        }
    }

    /// <summary>
    /// Static class containing access to all data necessary to start a game and can be converted in any necessary types 
    ///     + SPlayerData :                     for the GameManager
    ///     + Dict<string, PlayerDataObject> :  for the Lobby
    /// </summary>
    public static class StaticPlayerData
    {
        public const string KEY_PLAYER_NAME         = "PlayerName";
        public const string KEY_CHARACTER_LEVEL     = "CharacterLevel";
        public const string KEY_CHARACTER           = "Character";
        public const string KEY_RUNE                = "Rune";
        public const string KEY_SPELLS              = "Spells";
        public const string KEY_SPELL_LEVELS        = "SpellLevels";

        public static string        PlayerName      => ProfileCloudData.GamerTag;
        public static int           CharacterLevel  => InventoryCloudData.Instance.GetCollectable(Character).Level;
        public static ECharacter    Character       => CharacterBuildsCloudData.SelectedCharacter;
        public static ERune         Rune            => CharacterBuildsCloudData.CurrentRune;
        public static ESpell[]      Spells          => CharacterBuildsCloudData.CurrentBuild;
        public static int[]         SpellLevels
        {
            get
            {
                int[] spellLevels = new int[Spells.Length];
                for (int i=0; i < Spells.Length; i++)
                {
                    spellLevels[i] = InventoryCloudData.Instance.GetSpell(Spells[i]).Level;
                }

                return spellLevels;
            }
        }


        #region Data Conversion

        /// <summary>
        /// Convert it's data into a struct obj readable by the GameManager
        /// </summary>
        /// <returns></returns>
        public static SPlayerData ToStruct()
        {
            return new SPlayerData(PlayerName, CharacterLevel, Character, Rune, Spells, SpellLevels, ProfileCloudData.CurrentProfileData.AsNetworkSerializable(), true);
        }

        /// <summary>
        /// Convert it's data into data readable by the Lobby
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, PlayerDataObject> ToPlayerDataObject()
        {
            return new Dictionary<string, PlayerDataObject> {
                { KEY_PLAYER_NAME,          new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, StaticPlayerData.PlayerName) },
                { KEY_CHARACTER,            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ((int)Character).ToString()) },
                { KEY_RUNE,                 new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ((int)Rune).ToString()) },
                { KEY_SPELLS,               new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, Spells.ToString()) },
                { KEY_SPELL_LEVELS,         new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SpellLevels.ToString()) },
            };
        }

        #endregion

        #region Debug

        public static void Display()
        {
            Debug.LogWarning("PlayerData =================================================");
            Debug.Log("     + " + KEY_PLAYER_NAME + " : " + PlayerName);
            Debug.Log("     + " + KEY_CHARACTER + " : " + Character.ToString());
        }

        #endregion
    }
}