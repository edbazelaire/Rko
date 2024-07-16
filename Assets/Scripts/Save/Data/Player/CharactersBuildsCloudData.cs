using Assets;
using Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Tools;
using Unity.Services.CloudSave.Models;
using Unity.VisualScripting;
using UnityEngine;

namespace Save
{
    [Serializable]
    public struct SCharacterBuildData
    {
        public int CurrentBuildIndex { get; set; }
        public ESpell[][] Builds;
        public ERune[] Runes;

        public SCharacterBuildData(int index = 0, ESpell[][] builds = default, ERune[] runes = default)
        {
            if (builds == default)
                builds = new ESpell[CharacterBuildsCloudData.N_BUILDS][] { CharacterBuildsCloudData.DEFAULT_BUILD, CharacterBuildsCloudData.DEFAULT_BUILD, CharacterBuildsCloudData.DEFAULT_BUILD };

            if (runes == default)
                runes = CharacterBuildsCloudData.DEFAULT_RUNES;

            CurrentBuildIndex = index;
            Builds = builds;
            Runes = runes;
        }

        [DoNotSerialize]
        public readonly ESpell[] CurrentBuild => Builds[CurrentBuildIndex];

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SCharacterBuildData : { \n");
            sb.Append("     CurrentBuildIndex : "  + TextHandler.ToString(CurrentBuildIndex) + ",\n");
            sb.Append("     Builds : "          + TextHandler.ToString(Builds) + ",\n");
            sb.Append("     Runes : "           + TextHandler.ToString(Runes) + ",\n");
            sb.Append("}");
            return sb.ToString();
        }
    }

    public class CharacterBuildsCloudData : CloudData
    {
        #region Members
        public new static CharacterBuildsCloudData Instance => Main.CloudSaveManager.GetCloudData(typeof(CharacterBuildsCloudData)) as CharacterBuildsCloudData;

        // ===============================================================================================
        // CONSTANTS
        /// <summary> max allowed number of builds for each characters </summary>
        public const    int                 N_BUILDS                = 3;
        /// <summary> number of spells in one build </summary>
        public const    int                 N_SPELLS_IN_BUILDS      = 4;

        /// <summary> default Character on loading </summary>
        public static ECharacter            DEFAULT_CHARACTER       => ECharacter.Alexander;
        /// <summary> default Rune on loading </summary>
        public static ERune                 DEFAULT_RUNE            => ERune.None;
        /// <summary> default build if none was created by the player </summary>
        public static ESpell[]              DEFAULT_BUILD           => new ESpell[N_SPELLS_IN_BUILDS]    { ESpell.RockShower, ESpell.Heal, ESpell.Blizzard, ESpell.ScorchedEarth }; 
        /// <summary> defualt list of None runes when initializing a new character data </summary>
        public static ERune[]               DEFAULT_RUNES           => new ERune[N_BUILDS]               { ERune.None, ERune.None, ERune.None }; 

        // KEYS ------------------------------------
        public const    string              KEY_SELECTED_CHARACTER  = "SelectedCharacter";
        public const    string              KEY_BUILDS              = "Builds";

        // ===============================================================================================
        // EVENTS
        /// <summary> action fired when the amount of gold changed </summary>
        public static   Action              SelectedCharacterChangedEvent;
        public static   Action              CurrentBuildIndexChangedEvent;
        public static   Action              CurrentBuildValueChangedEvent;
        public static   Action              CurrentRuneChangedEvent;

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_SELECTED_CHARACTER, DEFAULT_CHARACTER },
            { KEY_BUILDS, new Dictionary<ECharacter, SCharacterBuildData>() }
        };

        // ===============================================================================================
        // DEPENDENT STATIC ACCESSORS
        /// <summary> get currently selected character </summary>
        public static ECharacter    SelectedCharacter                       => (ECharacter)Instance.m_Data[KEY_SELECTED_CHARACTER];
        /// <summary> get all builds of all characters </summary>
        public static Dictionary<ECharacter, SCharacterBuildData> Builds    => Instance.m_Data[KEY_BUILDS] as Dictionary<ECharacter, SCharacterBuildData>;
        /// <summary> get selected character's currently selected build's index </summary>
        public static int           CurrentBuildIndex                       => Builds[SelectedCharacter].CurrentBuildIndex;
        /// <summary> get current build of the currently seleceted characters </summary>
        public static ESpell[]      CurrentBuild                            => Builds[SelectedCharacter].CurrentBuild;
        /// <summary> get current rune of the currently seleceted characters </summary>
        public static ERune         CurrentRune                             => Builds[SelectedCharacter].Runes[CurrentBuildIndex];

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Convert SCharacterBuildsCloudData into a dictionnary (easier to manipulate type of data)
        /// </summary>
        /// <param name="charsBuildsList"></param>
        /// <returns></returns>
        protected override object Convert(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(Dictionary<ECharacter, SCharacterBuildData>))
            {
                try
                {
                    return item.Value.GetAs<Dictionary<ECharacter, SCharacterBuildData>>();
                } 

                // [ERROR] : if a character has been deleted it can be impossible to reconstruct the dict from the json - just throw error and use default data
                catch (Exception e) 
                {
                    ErrorHandler.Error("Unable to construct the BuildData from the data - restoring with default values");
                    ErrorHandler.Error(e.Message);
                    return m_Data[item.Key];
                }
            }

            return base.Convert(item);
        }

        #endregion


        #region Current Selected Data Manipulator

        public static void SetSelectedCharacter(ECharacter character)
        {
            Instance.m_Data[KEY_SELECTED_CHARACTER] = character;

            // Save & Fire event of the change
            Instance.SaveValue(KEY_SELECTED_CHARACTER);
            SelectedCharacterChangedEvent?.Invoke();
        }

        public static void SelectBuild(int index)
        {
            // check validity of the index
            if (index < 0 || index >= Builds.Count)
            {
                ErrorHandler.Error("Bad build index provided : " + index);
                return;
            }

            // no need to change what is already in place
            if (index == CurrentBuildIndex)
            {
                ErrorHandler.Warning("Change of build with index " + index + " was requested but this is already the value of the build index. This behavior should not happen, fix");
                return;
            }

            // update the value
            SCharacterBuildData characterBuildData = Builds[SelectedCharacter];
            characterBuildData.CurrentBuildIndex = index;
            Builds[SelectedCharacter] = characterBuildData;

            // Save & Fire event of the change
            Instance.SaveValue(KEY_BUILDS);
            CurrentBuildIndexChangedEvent?.Invoke();
        }

        /// <summary>
        /// Change the current build at given index by provided spell
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="index"></param>
        public static void SetSpellInCurrentBuild(ESpell? spell, int index)
        {
            CurrentBuild[index] = spell.HasValue ? spell.Value : ESpell.Count;

            Instance.SaveValue(KEY_BUILDS);
            CurrentBuildValueChangedEvent?.Invoke();
        }

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetCurrentRune(ERune rune)
        {
            // update the value
            SCharacterBuildData characterBuildData = Builds[SelectedCharacter];
            characterBuildData.Runes[characterBuildData.CurrentBuildIndex] = rune;
            Builds[SelectedCharacter] = characterBuildData;

            Instance.SaveValue(KEY_BUILDS);
            CurrentRuneChangedEvent?.Invoke();  
        }

        #endregion


        #region Reset & Unlock

        public override void Reset(string key)
        {
            base.Reset(key);

            switch (key)
            {
                case KEY_SELECTED_CHARACTER:
                    SetSelectedCharacter(DEFAULT_CHARACTER);
                    break;

                case KEY_BUILDS:
                    var data = new Dictionary<ECharacter, SCharacterBuildData>();
                    foreach (ECharacter character in Enum.GetValues(typeof(ECharacter)))
                    {
                        data[character] = new SCharacterBuildData(index: 0, builds: new ESpell[N_BUILDS][] { DEFAULT_BUILD, DEFAULT_BUILD, DEFAULT_BUILD });
                    }

                    m_Data[key] = data;

                    CurrentBuildIndexChangedEvent?.Invoke();
                    break;

                default:
                    ErrorHandler.Error("Unhandled key : " + key);
                    return;
            }
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check :
        ///     - Each characters is in the keys
        ///     - Each characters has 3 builds provided
        ///     - Each build has 4 different spells
        /// </summary>
        void CheckCharactersBuilds()
        {
            Dictionary<ECharacter, SCharacterBuildData> buildsDictionary = m_Data[KEY_BUILDS] as Dictionary<ECharacter, SCharacterBuildData>;

            if (buildsDictionary == null)
            {
                ErrorHandler.Warning("Builds dictionary is null or not of expected type - creating new from scratch");
                buildsDictionary = new Dictionary<ECharacter, SCharacterBuildData>();
            }

            // Check each character
            foreach (ECharacter character in Enum.GetValues(typeof(ECharacter)))
            {
                if (character == ECharacter.Count)
                    continue;
                
                if (!buildsDictionary.ContainsKey(character))
                {
                    ErrorHandler.Warning("Character " + character + " is missing from the builds dictionary - adding it with only default builds");
                    buildsDictionary[character] = new SCharacterBuildData(index: 0, builds: new ESpell[N_BUILDS][] { DEFAULT_BUILD, DEFAULT_BUILD, DEFAULT_BUILD });
                }

                // Check number of builds for each character
                if (buildsDictionary[character].Builds.Length != N_BUILDS)
                {
                    ErrorHandler.Warning("Character " + character + " does not have exactly " + N_BUILDS + " builds provided => reseting to default");
                    buildsDictionary[character] = new SCharacterBuildData(index: 0, builds: new ESpell[N_BUILDS][] { DEFAULT_BUILD, DEFAULT_BUILD, DEFAULT_BUILD });
                }

                // check number of runes
                if (buildsDictionary[character].Runes == null || buildsDictionary[character].Runes.Length != N_BUILDS)
                {
                    ErrorHandler.Warning("Character " + character + " does not have exactly " + N_BUILDS + " runes provided => reseting to default");
                    SCharacterBuildData characterData = buildsDictionary[character];
                    DEFAULT_RUNES.CopyTo(characterData.Runes, 0);
                    buildsDictionary[character] = characterData;
                }

                // Check each build for each character
                for (int i = 0; i < buildsDictionary[character].Builds.Length; i++)
                {
                    ESpell[] build = buildsDictionary[character].Builds[i];

                    bool hasError = false;

                    // Check number of spells in each build
                    if (build.Length != 4)
                    {
                        ErrorHandler.Error("Build " + i + " of Character " + character + "  has an incorrect number of spells (" + build.Length + ") - reseting to default data");
                        hasError = true;
                    }

                    if (!hasError)
                    {
                        // Check for duplicated values within each build
                        HashSet<ESpell> uniqueSpells = new HashSet<ESpell>();
                        foreach (ESpell spell in build)
                        {
                            if (!uniqueSpells.Add(spell))
                            {
                                ErrorHandler.Error("Build " + i + " of Character " + character + "  has duplicated spell (" + spell + ") - reseting to default data");
                                hasError = true;
                                break;
                            }
                        }
                    }

                    if (hasError)
                        buildsDictionary[character].Builds[i] = DEFAULT_BUILD;
                }
            }

            m_Data[KEY_BUILDS] = buildsDictionary;
        }

        /// <summary>
        /// Check if current build is not missing any spell
        /// </summary>
        /// <returns></returns>
        public static bool IsCurrentBuildOk
        {
            get
            {
                foreach (ESpell spell in CurrentBuild)
                {
                    if (spell == ESpell.Count)
                        return false;
                }

                return true;
            }
        }

        #endregion


        #region Listeners

        protected override void OnCloudDataKeyLoaded(string key)
        {
            base.OnCloudDataKeyLoaded(key);

            // CHECKERS : check that data is conform to the expected data, add default data if some data is missing
            switch (key)
            {
                case KEY_BUILDS:
                    CheckCharactersBuilds();
                    break;
            }
        }

        #endregion


        #region Debug

        public override string ToString()
        {
            string str = base.ToString();
            str += "\n SelectedCharacter : "    + TextHandler.ToString(SelectedCharacter);
            str += "\n CurrentBuildIndex : "    + TextHandler.ToString(CurrentBuildIndex);
            str += "\n CurrentBuild : "         + TextHandler.ToString(CurrentBuild);
            str += "\n CurrentRune : "          + TextHandler.ToString(CurrentRune);
            return str;
        }

        #endregion
    }
}