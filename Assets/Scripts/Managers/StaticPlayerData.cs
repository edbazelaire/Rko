using Data;
using Data.DataStructures;
using Enums;
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
        public EArenaDifficulty     ArenaDifficulty;
        public float                DecisionRefresh;
        public float                Randomness;

        public SBotData(EArenaDifficulty arenaDifficulty, float decisionRefresh, float randomness)
        {
            ArenaDifficulty     = arenaDifficulty;
            DecisionRefresh     = decisionRefresh;
            Randomness          = randomness;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ArenaDifficulty);
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
        public FixedString64Bytes           PlayerName;
        public int                          CharacterLevel;
        public FixedString64Bytes           Character;
        public ERune[]                      Runes;
        public int[]                        RuneLevels;
        public ESpell[]                     Spells;
        public int[]                        SpellLevels;
        public SProfileDataNetwork          ProfileData;
        public bool                         IsPlayer;
        public STriggerEffect[]             TriggerEffects; 
        public FixedString128Bytes[]        PowerUps; 
        public SCharacterStatScaling[]      BonusStats; 
        public SBotData                     BotData; 

        public SPlayerData(FixedString64Bytes playerName, int characterLevel, FixedString64Bytes character, ERune[] runes = default, int[] runeLevels = default, ESpell[] spells = default, int[] spellLevels = default, SProfileDataNetwork profileData = default, bool isPlayer = false, STriggerEffect[] triggerEffects = default, FixedString128Bytes[] powerUps = default, SCharacterStatScaling[] bonusStats = default, SBotData botData = default)
        {
            PlayerName      = playerName;
            CharacterLevel  = characterLevel;
            Character       = character;
            Runes           = runes             != default ? runes          : new ERune[0];
            RuneLevels      = runeLevels        != default ? runeLevels     : new int[0];
            Spells          = spells            != default ? spells         : new ESpell[0];
            SpellLevels     = spellLevels       != default ? spellLevels    : new int[0];
            ProfileData     = profileData;
            IsPlayer        = isPlayer;
            TriggerEffects  = triggerEffects    != default ? triggerEffects : new STriggerEffect[0];
            PowerUps        = powerUps          != default ? powerUps       : new FixedString128Bytes[0];
            BonusStats      = bonusStats        != default ? bonusStats     : new SCharacterStatScaling[0];
            BotData         = botData;
        }

        public void SetPowerUps(List<string> powerUps)
        {
            PowerUps = new FixedString128Bytes[powerUps.Count];

            // Iterate through the List<string> and convert each element to FixedString32Bytes
            for (int i = 0; i < powerUps.Count; i++)
            {
                // Convert each string to FixedString128Bytes
                PowerUps[i] = new FixedString128Bytes(powerUps[i]);  // Automatically truncates if string is longer than 32 bytes
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // direct serialization
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref CharacterLevel);
            serializer.SerializeValue(ref Character);
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

            // -- Runes
            length = Runes != null ? Runes.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                Runes = new ERune[length];
            }
            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref Runes[i]);
            }

            // -- RuneLevels
            length = RuneLevels != null ? RuneLevels.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                RuneLevels = new int[length];
            }
            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref RuneLevels[i]);
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

            // -- PowerUps
            length = PowerUps != null ? PowerUps.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                PowerUps = new FixedString128Bytes[length];
            }
            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref PowerUps[i]);
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
        public static ERune[]       Runes           => CharacterBuildsCloudData.CurrentRunes;
        public static ESpell[]      Spells          => CharacterBuildsCloudData.CurrentBuild;
        public static int[] RuneLevels
        {
            get
            {
                int[] runeLevels = new int[Runes.Length];
                for (int i = 0; i < Runes.Length; i++)
                {
                    runeLevels[i] = InventoryCloudData.Instance.GetCollectable(Runes[i]).Level;
                }

                return runeLevels;
            }
        }
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
            return new SPlayerData(
                playerName:     PlayerName, 
                characterLevel: CharacterLevel, 
                character:      Character.ToString(), 
                runes:          Runes, 
                runeLevels:     RuneLevels, 
                spells:         Spells, 
                spellLevels:    SpellLevels, 
                profileData:    ProfileCloudData.CurrentProfileData.AsNetworkSerializable(), 
                isPlayer:       true
            );
        }

        /// <summary>
        /// Convert it's data into data readable by the Lobby
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, PlayerDataObject> ToPlayerDataObject()
        {
            return new Dictionary<string, PlayerDataObject> {
                { KEY_PLAYER_NAME,          new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, StaticPlayerData.PlayerName) },
                { KEY_CHARACTER,            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, Character.ToString()) },
            };
        }

        #endregion

        #region Debug

        public static void Display()
        {
            Debug.LogWarning("PlayerData =================================================");
            Debug.Log("     + " + KEY_PLAYER_NAME + " : " + PlayerName);
            Debug.Log("     + " + KEY_CHARACTER + " : " + Character);
        }

        #endregion
    }
}