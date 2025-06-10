using Assets;
using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tools;
using Unity.Services.CloudSave.Models;
using Unity.VisualScripting;

namespace Save
{
    [Serializable]
    public struct SCharacterBuildData
    {
        public int CurrentBuildIndex { get; set; }
        public ESpell[][] Builds;
        public ERune[][] Runes;

        public SCharacterBuildData(int index = 0, ESpell[][] builds = default, ERune[][] runes = default)
        {
            if (builds == default)
                builds = new ESpell[CharacterBuildsCloudData.N_BUILDS][] { CharacterBuildsCloudData.DEFAULT_BUILD, CharacterBuildsCloudData.DEFAULT_BUILD, CharacterBuildsCloudData.DEFAULT_BUILD };

            if (runes == default)
                runes = new ERune[CharacterBuildsCloudData.N_BUILDS][] { CharacterBuildsCloudData.DEFAULT_RUNES, CharacterBuildsCloudData.DEFAULT_RUNES, CharacterBuildsCloudData.DEFAULT_RUNES };

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
        public new static CharacterBuildsCloudData Instance => CloudSaveManager.Instance.GetCloudData(typeof(CharacterBuildsCloudData)) as CharacterBuildsCloudData;

        // ===============================================================================================
        // CONSTANTS
        /// <summary> max allowed number of builds for each characters </summary>
        public const int N_BUILDS = 3;
        /// <summary> number of spells in one build </summary>
        public const int N_SPELLS_IN_BUILDS = 4;
        /// <summary> number of runes in one build </summary>
        public const int N_RUNE_IN_BUILDS = 3;

        /// <summary> default Character on loading </summary>
        public static ECharacter DEFAULT_CHARACTER => ECharacter.Alexander;
        /// <summary> default Rune on loading </summary>
        public static ERune DEFAULT_RUNE => ERune.None;
        /// <summary> default build if none was created by the player </summary>
        public static ESpell[] DEFAULT_BUILD => new ESpell[] { ESpell.RockShower, ESpell.Heal, ESpell.Blizzard, ESpell.IronSkin };
        /// <summary> defualt list of None runes when initializing a new character data </summary>
        public static ERune[] DEFAULT_RUNES => new ERune[] { ERune.None, ERune.None, ERune.None };

        // KEYS ------------------------------------
        public const string KEY_SELECTED_CHARACTER = "SelectedCharacter";
        public const string KEY_BUILDS = "Builds";

        // ===============================================================================================
        // EVENTS
        /// <summary> action fired when the amount of gold changed </summary>
        public static Action SelectedCharacterChangedEvent;
        public static Action CurrentBuildIndexChangedEvent;
        public static Action CurrentBuildValueChangedEvent;
        public static Action CurrentRuneChangedEvent;

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
        public static ECharacter        SelectedCharacter                       => (ECharacter)Instance.m_Data[KEY_SELECTED_CHARACTER];
        /// <summary> get all builds of all characters </summary>
        public static Dictionary<ECharacter, SCharacterBuildData> Builds        => Instance.m_Data[KEY_BUILDS] as Dictionary<ECharacter, SCharacterBuildData>;
        /// <summary> get selected character's currently selected build's index </summary>
        public static int               CurrentBuildIndex                       => Builds[SelectedCharacter].CurrentBuildIndex;
        /// <summary> get current build of the currently seleceted characters </summary>
        public static ESpell[]          CurrentBuild                            => Builds[SelectedCharacter].CurrentBuild;
        /// <summary> get current rune of the currently seleceted characters </summary>
        public static ERune[]           CurrentRunes                            => Builds[SelectedCharacter].Runes[CurrentBuildIndex];

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

        public static void SetInCurrentBuild(Enum collectable, int index)
        {
            if (collectable.GetType() == typeof(ECharacter))
            {
                SetSelectedCharacter((ECharacter)collectable);
                return;
            }

            if (collectable.GetType() == typeof(ESpell))
            {
                SetSpellInCurrentBuild((ESpell)collectable, index);
                return;
            }

            if (collectable.GetType() == typeof(ERune))
            {
                SetCurrentRune((ERune)collectable, index);
                return;
            }

            ErrorHandler.Error("Unhandled case : " + collectable);
        }

        /// <summary>
        /// Check if provided collectable is used in current build
        /// </summary>
        /// <param name="collectable"></param>
        /// <returns></returns>
        public static bool IsInCurrentBuild(Enum collectable)
        {
            if (collectable.GetType() == typeof(ECharacter))
            {
                return SelectedCharacter == (ECharacter)collectable;
            }

            if (collectable.GetType() == typeof(ESpell))
            {
                return CurrentBuild.Contains((ESpell)collectable);
            }

            if (collectable.GetType() == typeof(ERune))
            {
                return CurrentRunes.Contains((ERune)collectable);
            }

            ErrorHandler.Error("Unhandled case : " + collectable);
            return false;
        }

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
            CurrentBuild[index] = spell.HasValue ? spell.Value : ESpell.None;

            Instance.SaveValue(KEY_BUILDS);
            CurrentBuildValueChangedEvent?.Invoke();
        }

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetCurrentRune(ERune rune, ERuneActivation runeActivation)
        {
            SetCurrentRune(rune, GetRuneActivationIndex(runeActivation));
        }

        /// <summary>
        /// Set Rune of the current build
        /// </summary>
        /// <param name="rune"></param>
        public static void SetCurrentRune(ERune rune, int index)
        {
            if (index < 0 || index > CurrentRunes.Length)
            {
                ErrorHandler.Error("Trying to set rune " + rune + " at wrong index : " + index);
                return;
            }

            // update the value
            SCharacterBuildData characterBuildData = Builds[SelectedCharacter];
            characterBuildData.Runes[characterBuildData.CurrentBuildIndex][index] = rune;
            Builds[SelectedCharacter] = characterBuildData;

            Instance.SaveValue(KEY_BUILDS);
            CurrentRuneChangedEvent?.Invoke();  
        }

        /// <summary>
        /// Try to find the rune in the build and get the Activation depending on index
        /// </summary>
        /// <param name="rune"></param>
        /// <param name="runeActivation"></param>
        /// <returns></returns>
        public static bool TryGetRuneActivationInBuild(ERune rune, out ERuneActivation runeActivation)
        {
            runeActivation = ERuneActivation.None;
            foreach (ERuneActivation activation in Enum.GetValues(typeof(ERuneActivation)))
            {
                if (activation == ERuneActivation.None)
                    continue;

                if (CurrentRunes[GetRuneActivationIndex(activation)] == rune)
                {
                    runeActivation = activation;
                    return true;
                }
            }

            // rune not found in build
            return false;
        }

        /// <summary>
        /// Get index of the corresponding rune Activation
        /// </summary>
        /// <param name="runeActivation"></param>
        /// <returns></returns>
        public static int GetRuneActivationIndex(ERuneActivation runeActivation)
        {
            switch (runeActivation)
            {
                case ERuneActivation.Primal:
                    return 0;

                case ERuneActivation.Major: 
                    return 1;

                case ERuneActivation.Minor:
                    return 2;

                default:
                    ErrorHandler.Error("Unhandled case " + runeActivation);
                    return -1;
            }
        }

        /// <summary>
        /// Get index of the corresponding rune Activation
        /// </summary>
        /// <param name="runeActivation"></param>
        /// <returns></returns>
        public static ERuneActivation GetRuneActivationFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return ERuneActivation.Primal;

                case 1: 
                    return ERuneActivation.Major;

                case 2:
                    return ERuneActivation.Minor;

                default:
                    ErrorHandler.Error("Unhandled case " + index);
                    return ERuneActivation.Minor;
            }
        }

        #endregion


        #region Reset & Unlock

        public override void Reset(string key, bool save = true)
        {
            base.Reset(key, save);

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

            if (save)
                Instance.SaveValue(key);
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check :
        ///     - Each characters is in the keys
        ///     - Each characters has 3 builds provided
        ///     - Each build has 4 different spells
        /// </summary>
        void CheckAllCharactersBuilds()
        {
            Dictionary<ECharacter, SCharacterBuildData> buildsDictionary = m_Data[KEY_BUILDS] as Dictionary<ECharacter, SCharacterBuildData>;

            if (buildsDictionary == null)
            {
                ErrorHandler.Warning("Builds dictionary is null or not of expected type - creating new from scratch");
                buildsDictionary = new Dictionary<ECharacter, SCharacterBuildData>();
            }

            // Check each character
            bool save = false;
            foreach (ECharacter character in Enum.GetValues(typeof(ECharacter)))
            {
                if (character == ECharacter.None)
                    continue;

                CheckCharacterBuild(character, ref buildsDictionary, ref save);
            }

            SetData(KEY_BUILDS, buildsDictionary, save: save);
        }

        /// <summary>
        /// Check one character's build
        ///     + Check CHARACTER  
        ///         - character in dict keys
        ///     + Check SPELLS 
        ///         - correct number
        ///         - spell existance
        ///     + Check RUNES
        ///         - correct number
        ///         - rune existance
        /// </summary>
        /// <param name="character"></param>
        /// <param name="buildsDictionary"></param>
        void CheckCharacterBuild(ECharacter character, ref Dictionary<ECharacter, SCharacterBuildData> buildsDictionary, ref bool save)
        {
            // ======================================================================================
            // CHARACTER
            if (!buildsDictionary.ContainsKey(character))
            {
                ErrorHandler.Warning("Character " + character + " is missing from the builds dictionary - adding it with only default builds");
                buildsDictionary[character] = new SCharacterBuildData(index: 0, builds: new ESpell[N_BUILDS][] { DEFAULT_BUILD, DEFAULT_BUILD, DEFAULT_BUILD });
                save = true;
            }

            // Check number of builds for each character
            if (buildsDictionary[character].Builds.Length != N_BUILDS)
            {
                ErrorHandler.Warning("Character " + character + " does not have exactly " + N_BUILDS + " builds provided => reseting to default");
                buildsDictionary[character] = new SCharacterBuildData(index: 0, builds: new ESpell[N_BUILDS][] { DEFAULT_BUILD, DEFAULT_BUILD, DEFAULT_BUILD });
                save = true;
            }

            // check number of runes
            if (buildsDictionary[character].Runes == null || buildsDictionary[character].Runes.Length != N_BUILDS)
            {
                ErrorHandler.Warning("Character " + character + " does not have exactly " + N_BUILDS + " runes provided => reseting to default");
                SCharacterBuildData characterData = buildsDictionary[character];
                DEFAULT_RUNES.CopyTo(characterData.Runes, 0);
                buildsDictionary[character] = characterData;
                save = true;
            }

            // ======================================================================================
            // CHECK EACH BUILD (individually)
            for (int buildIndex = 0; buildIndex < buildsDictionary[character].Builds.Length; buildIndex++)
            {
                buildsDictionary[character].Builds[buildIndex] = CheckCharacterBuildSpells(buildsDictionary[character].Builds[buildIndex], out string reason);
                if (reason != "")
                {
                    ErrorHandler.Error("Build " + buildIndex + " of Character " + character + " : " + reason);
                    save = true;
                }

                buildsDictionary[character].Runes[buildIndex] = CheckCharacterBuildRunes(buildsDictionary[character].Runes[buildIndex], out reason);
                if (reason != "")
                {
                    ErrorHandler.Error("Build " + buildIndex + " of Character " + character + " : " + reason);
                    save = true;
                }
            }
        }

        ESpell[] CheckCharacterBuildSpells(ESpell[] spells, out string reason)
        {
            bool resetBuild = false;
            reason = "";
          
            // Check number of spells in each build
            if (spells.Length != 4)
            {
                reason = "bad length ("+ spells .Length + ") for number of spells in build (expected 4) - reseting with default build";
                return DEFAULT_BUILD;
            }

            // Check for duplicated values within each build
            HashSet<ESpell> uniqueSpells = new HashSet<ESpell>();
            for (int i = 0; i < spells.Length; i++)
            {
                // do not raise error for duplicated None values
                if (spells[i] == ESpell.None)
                {
                    reason = "None spell found in buil - reseting this build";
                    resetBuild = true;
                    break;
                }

                // CHECK : spell exists
                if (!Enum.IsDefined(typeof(ESpell), spells[i]))
                {
                    reason = $"undefined spell {spells[i]} in data - reseting this build";
                    resetBuild = true;
                    break;
                }

                // CHECK : SpellData exists
                var spellData = SpellLoader.GetSpellData(spells[i]);
                if (spellData == null)
                {
                    reason = $"unable to find spell {spells[i]} in data - reseting this build";
                    resetBuild = true;
                    break;
                }

                // CHECK : Not a Linked spell
                if (spellData.Linked)
                {
                    reason = $"found linked spell {spells[i]} in data - reseting this build";
                    resetBuild = true;
                    break;
                }

                // CHECK : is unique
                if (!uniqueSpells.Add(spells[i]))
                {
                    reason = "duplicated spell (" + spells[i] + ") - reseting this build";
                    resetBuild = true;
                    break;
                }
            }

            // reset of the build called
            if (resetBuild)
                spells = DEFAULT_BUILD;

            return spells;
        }

        ERune[] CheckCharacterBuildRunes(ERune[] runes, out string reason)
        {
            reason = "";

            // Check number of spells in each build
            if (runes.Length != 3)
            {
                reason = "bad length ("+ runes .Length + ") for number of runes in build (expected 3) - reseting with default runes";
                return DEFAULT_RUNES;
            }

            // Check for duplicated values within each build
            HashSet<ERune> uniqueRunes = new HashSet<ERune>();
            for (int i = 0; i < runes.Length; i++)
            {
                // do not raise error for duplicated None values
                if (runes[i] == ERune.None)
                    continue;

                // CHECK : rune exists
                if (! Enum.IsDefined(typeof(ERune), runes[i]))
                {
                    runes[i] = ERune.None;
                }

                // CHECK : rune data exists
                var runeData = SpellLoader.GetRuneData(runes[i]);
                if (runeData == null)
                {
                    reason = $"unable to find data for rune {runes[i]} - removing from build";
                    runes[i] = ERune.None;
                    break;
                }

                // CHECK : is unique
                if (!uniqueRunes.Add(runes[i]))
                {
                    reason = "duplicated rune (" + runes[i] + ") - removing from build";
                    runes[i] = ERune.None;
                }
            }

            return runes;
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
                    if (spell == ESpell.None)
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
                    CheckAllCharactersBuilds();
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
            str += "\n CurrentRunes : "         + TextHandler.ToString(CurrentRunes);
            return str;
        }

        #endregion
    }
}