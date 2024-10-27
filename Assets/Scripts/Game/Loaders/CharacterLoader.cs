using Data;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.Loaders
{
    public class CharacterLoader: MonoBehaviour
    {
        #region Members

        static CharacterLoader s_Instance;

        public GameObject PlayerPrefab;
        public GameObject PlayerAIPrefab;
        public GameObject PlayerTutoAIPrefab;

        CharacterData[] m_CharactersList;
        Dictionary<ECharacter, CharacterData> m_Characters;
        Dictionary<string, CharacterData> m_Bosses;

        public Dictionary<ECharacter, CharacterData> Characters => m_Characters;
        public Dictionary<string, CharacterData> Bosses => m_Bosses;
        public CharacterData[] CharactersList => m_CharactersList;

        #endregion


        #region Initialization

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Initialize()
        {
            LoadCharacterData();
            LoadBossesData();
        }

        void LoadCharacterData()
        {
            m_CharactersList = Resources.LoadAll<CharacterData>("Data/Characters");

            m_Characters = new Dictionary<ECharacter, CharacterData>();
            foreach (CharacterData character in m_CharactersList)
            {
                if (Characters.ContainsKey(character.Character))
                {
                    ErrorHandler.Error($"CharacterLoader : Characters list contains duplicate : {character}");
                    continue;
                }
                Characters.Add(character.Character, character);
            }
        }

        void LoadBossesData()
        {
            m_CharactersList = Resources.LoadAll<CharacterData>("Data/Bosses");

            m_Bosses = new Dictionary<string, CharacterData>();
            foreach (CharacterData character in m_CharactersList)
            {
                if (m_Bosses.ContainsKey(character.Name))
                {
                    ErrorHandler.Error($"CharacterLoader : Bosses list contains duplicate : {character}");
                    continue;
                }
                m_Bosses.Add(character.Name, character);
            }
        }


        #endregion


        #region Public Static Manipulators

        /// <summary>
        /// Get data of a character (updated with level if provided)
        /// </summary>
        /// <param name="character"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static CharacterData GetCharacterData(string characterName, int level = 1, bool destroy = false)
        {
            // =================================================================
            // Try load : CHARACTER
            if (Enum.TryParse(characterName, out ECharacter character))
            {
                if (!CharacterLoader.Instance.Characters.ContainsKey(character))
                {
                    ErrorHandler.FatalError($"CharacterLoader : Character {character} not found");
                    return null;
                }

                return Instance.Characters[character].Clone(level, destroy);
            }

            // =================================================================
            // Try load : BOSS
            if (Enum.TryParse(characterName, out EBoss boss))
            {
                if (!Instance.Bosses.ContainsKey(characterName))
                {
                    ErrorHandler.FatalError($"CharacterLoader : Character / Boss {character} not found");
                    return null;
                }

                return Instance.Bosses[characterName].Clone(level, destroy);
            }

            ErrorHandler.Error("Unhandled character : " + characterName);
            return null;
        }

        /// <summary>
        /// Get data of a character (updated with level if provided)
        /// </summary>
        /// <param name="character"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static CharacterData GetCharacterData(ECharacter character, int level = 1, bool destroy = false)
        {
            if (!CharacterLoader.Instance.Characters.ContainsKey(character))
            {
                ErrorHandler.FatalError($"CharacterLoader : Character {character} not found");
                return null;
            }

            return Instance.Characters[character].Clone(level, destroy);
        }

        /// <summary>
        /// Get the character that posses the provided spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public static ECharacter? GetCharacterWithSpell(ESpell spell)
        {
            foreach (var item in Instance.m_Characters)
            {
                if (item.Value.Ultimate == spell || item.Value.AutoAttack == spell || item.Value.SpecialAbility == spell)
                    return item.Key;
            }

            ErrorHandler.Error("Unable to find character linked to spell : " + spell);
            return null;
        }

        public static bool IsBoss(string character)
        {
            return Instance.m_Bosses.ContainsKey(character);
        }

        #endregion


        #region Dependent Members

        public static CharacterLoader Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindFirstObjectByType<CharacterLoader>();
                    if (s_Instance == null)
                        return null;

                    s_Instance.Initialize();
                }
                return s_Instance;
            }
        }

        #endregion
    }

}
