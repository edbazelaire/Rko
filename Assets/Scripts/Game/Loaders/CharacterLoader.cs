using Data;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using Unity.VisualScripting;
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
        public GameObject StructurePrefab;

        CharacterData[] m_CharactersList;
        Dictionary<ECharacter, CharacterData> m_Characters;
        Dictionary<string, CharacterData> m_Bosses;
        Dictionary<string, CharacterData> m_Spawns;

        public Dictionary<ECharacter, CharacterData> Characters => m_Characters;
        public Dictionary<string, CharacterData> Bosses => m_Bosses;
        public Dictionary<string, CharacterData> Spawns => m_Spawns;
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
        }

        void LoadCharacterData()
        {
            m_CharactersList = Resources.LoadAll<CharacterData>("Data/Characters");

            m_Characters = new Dictionary<ECharacter, CharacterData>();
            m_Bosses = new Dictionary<string, CharacterData>();
            m_Spawns = new Dictionary<string, CharacterData>();

            foreach (CharacterData characterData in m_CharactersList)
            {
                string characterName = characterData.Name;

                // =================================================================
                // Try load : CHARACTER
                if (Enum.TryParse(characterName, out ECharacter character))
                {
                    Instance.Characters[character] = characterData;
                }

                // =================================================================
                // Try load : BOSS
                else if (Enum.TryParse(characterName, out EBoss boss))
                {
                    Instance.Bosses[characterName] = characterData;
                }

                // =================================================================
                // Try load : SPAWN
                else if(Enum.TryParse(characterName, out ESpawn spawn))
                {
                    Instance.m_Spawns[characterName] = characterData;
                }

                else
                {
                    ErrorHandler.Error("Unhandled parse character name as any type of characters : " + characterName);
                }
            }
        }

        #endregion


        #region Data Accessors

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
                    ErrorHandler.Error($"CharacterLoader : Character {character} not found");
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
                    ErrorHandler.Error($"CharacterLoader : Boss {character} not found");
                    return null;
                }

                return Instance.Bosses[characterName].Clone(level, destroy);
            }

            // =================================================================
            // Try load : SPAWN
            if (Enum.TryParse(characterName, out ESpawn spawn))
            {
                if (!Instance.m_Spawns.ContainsKey(characterName))
                {
                    ErrorHandler.Error($"CharacterLoader : Spawn {character} not found");
                    return null;
                }

                return Instance.m_Spawns[characterName].Clone(level, destroy);
            }

            ErrorHandler.Error("Unhandled parse character name as any type of characters : " + characterName);
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

        #endregion


        #region Checkers

        public static bool IsCharacter(string characterName)
        {
            return Enum.TryParse(characterName, out ECharacter _);
        }

        public static bool IsBoss(string characterName)
        {
            return Enum.TryParse(characterName, out EBoss _);
        }

        public static bool IsSpawn(string characterName)
        {
            return Enum.TryParse(characterName, out ESpawn _);
        }

        #endregion


        #region Prefabs

        public static GameObject GetPrefab(string characterName, bool isPlayer)
        {
            var characterData = GetCharacterData(characterName);
            if (characterData.IsStructure)
                return Instance.StructurePrefab;
            else if (isPlayer)
                return Instance.PlayerPrefab;
            else
                return Instance.PlayerAIPrefab;
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
