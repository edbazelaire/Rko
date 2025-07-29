using Data;
using Data.DataStructures;
using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Loaders;
using Game.Spells;
using NUnit.Framework.Internal;
using Save;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static Unity.Collections.Unicode;
using static UnityEngine.UI.ScrollRect;

namespace Managers
{
    /// <summary>
    /// Structural data that can be provided to create a new character 
    /// </summary>
    [Serializable]
    public struct SBotData : INetworkSerializable
    {
        public FixedString32Bytes           Difficulty;
        public float                        DecisionRefresh;
        public float                        Randomness;
        public float                        MinReactionTime;
        public float                        MaxReactionTime;
        public float                        MinMovementTime;
        public float                        MaxMovementTime;
        public float                        MinMovementRefresh;
        public float                        MaxMovementRefresh;
        public Dictionary<string, float>    ExtraVariables;

        public SBotData(string difficulty, float decisionRefresh = 0f, float randomness = 0f, (float, float) reactionTime = default, (float, float) movementTime = default, (float, float) movementRefresh = default, Dictionary<string, float> extraVariables = default)
        {
            Difficulty          = difficulty;
            DecisionRefresh     = decisionRefresh;
            Randomness          = randomness;
            MinReactionTime     = reactionTime.Item1;
            MaxReactionTime     = reactionTime.Item2;
            MinMovementTime     = movementTime.Item1;
            MaxMovementTime     = movementTime.Item2;
            MinMovementRefresh  = movementRefresh.Item1;
            MaxMovementRefresh  = movementRefresh.Item2;
            ExtraVariables      = extraVariables;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Difficulty);
            serializer.SerializeValue(ref DecisionRefresh);
            serializer.SerializeValue(ref Randomness);
            serializer.SerializeValue(ref MinReactionTime);
            serializer.SerializeValue(ref MaxReactionTime);
            serializer.SerializeValue(ref MinMovementRefresh);
            serializer.SerializeValue(ref MaxMovementRefresh);

            // Serialize Dictionary
            int count = ExtraVariables?.Count ?? 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                ExtraVariables ??= new Dictionary<string, float>();

                foreach (var kvp in ExtraVariables)
                {
                    FixedString64Bytes key = kvp.Key;
                    float value = kvp.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref value);
                }
            }
            else
            {
                ExtraVariables = new Dictionary<string, float>(count);
                for (int i = 0; i < count; i++)
                {
                    FixedString64Bytes key = default;
                    float value = 0f;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref value);
                    ExtraVariables[key.ToString()] = value;
                }
            }
        }

        public float GetExtraVar(string key)
        {
            if (ExtraVariables == null || ! ExtraVariables.ContainsKey(key))
                return 0f;

            return ExtraVariables[key];
        }

        public float GetExtraVar(Enum key)
        {
            return GetExtraVar(key.ToString());
        }
    }

    [Serializable] 
    public struct SBuildData : INetworkSerializable
    {
        public int                  CharacterLevel;
        public string               Character;
        public ERune[]              Runes;
        public int[]                RuneLevels;
        public ESpell[]             Spells;
        public int[]                SpellLevels;

        public SBuildData(int characterLevel, string character, ERune[] runes = default, int[] runeLevels = default, ESpell[] spells = default, int[] spellLevels = default)
        {
            CharacterLevel      = characterLevel;
            Character           = character;
            Runes               = runes != default ? runes : new ERune[3];
            RuneLevels          = runeLevels != default ? runeLevels : new int[3];
            Spells              = spells != default ? spells : new ESpell[4];
            SpellLevels         = spellLevels != default ? spellLevels : new int[4];
        }

        public List<Enum> Get(ECollectableType collectableType)
        {
            switch(collectableType)
            {
                case ECollectableType.Spell:
                    return Spells.Cast<Enum>().ToList();

                case ECollectableType.Rune:
                    return Runes.Cast<Enum>().ToList();

                default:
                    return new List<Enum>(); 
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // direct serialization
            serializer.SerializeValue(ref CharacterLevel);

            // Use FixedString for network transfer, convert from string
            FixedString64Bytes fixedChar = new FixedString64Bytes(Character ?? "");
            serializer.SerializeValue(ref fixedChar);
            if (serializer.IsReader)
            {
                Character = fixedChar.ToString();
            }

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
        }

        public bool Check(bool throwError = true)
        {
            bool test = true;

            if (Character == null || CharacterLoader.GetCharacterData(Character.ToString(), 1, destroy: true) == null)
            {
                if (throwError)
                    ErrorHandler.Error($"Unable to find character : " + Character);
                test = false;
            }    

            if (Spells == null || Spells.Count() != 4)
            {
                if (throwError)
                    ErrorHandler.Error($"Bad number of Spells ({(Spells == null ? 0 : Spells.Count())}) : expected {4}");
                test = false;
            } 

            if (Runes == null || Runes.Count() != 3)
            {
                if (throwError)
                    ErrorHandler.Error($"Bad number of Runes ({(Runes == null ? 0 : Runes.Count())}) : expected {3}");
                test = false;
            }

            return test;
        }
    }

    /// <summary>
    /// Structural data that can be provided to create a new character 
    /// </summary>
    [Serializable]
    public struct SPlayerData : INetworkSerializable
    {
        public FixedString64Bytes           PlayerName;
        public SBuildData                   BuildData;
        public SProfileDataNetwork          ProfileData;
        public bool                         IsPlayer;
        public STriggerEffect[]             TriggerEffects; 
        public FixedString128Bytes[]        PowerUps; 
        public SCharacterStatScaling[]      BonusStats; 
        public SBotData                     BotData; 

        public SPlayerData(FixedString64Bytes playerName, int characterLevel, string character, ERune[] runes = default, int[] runeLevels = default, ESpell[] spells = default, int[] spellLevels = default, SProfileDataNetwork profileData = default, bool isPlayer = false, STriggerEffect[] triggerEffects = default, FixedString128Bytes[] powerUps = default, SCharacterStatScaling[] bonusStats = default, SBotData botData = default)
        {
            PlayerName      = playerName;
            BuildData       = new SBuildData(characterLevel, character, runes, runeLevels, spells, spellLevels);
            ProfileData     = profileData;
            IsPlayer        = isPlayer;
            TriggerEffects  = triggerEffects    != default ? triggerEffects : new STriggerEffect[0];
            PowerUps        = powerUps          != default ? powerUps       : new FixedString128Bytes[0];
            BonusStats      = bonusStats        != default ? bonusStats     : new SCharacterStatScaling[0];
            BotData         = botData;
        }

        public void SetBuild(SBuildData buildData)
        {
            BuildData = buildData;
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
            serializer.SerializeValue(ref IsPlayer);

            // Sub - serialization
            BuildData.NetworkSerialize(serializer);
            ProfileData.NetworkSerialize(serializer);
            BotData.NetworkSerialize(serializer);

            // -- TriggerEffects
            var length = TriggerEffects != null ? TriggerEffects.Length : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                TriggerEffects = new STriggerEffect[length];
            }
            for (int i = 0; i < length; i++)
            {
                TriggerEffects[i].NetworkSerialize(serializer);
            }

            // -- Power Ups
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
        public static ESpell[]      Spells          => CharacterBuildsCloudData.CurrentSpells;
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