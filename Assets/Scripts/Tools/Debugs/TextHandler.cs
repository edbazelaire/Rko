using Data;
using Data.DataStructures;
using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Loaders;
using Game.Spells;
using Game.UI;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Handles the cleaning of text, formats, colors, ...
    /// </summary>
    public static class TextHandler
    {
        /// <summary> when put in a text, all lignes will with this tag will have enought spaces to match alignement </summary>
        public const string TAG_ALIGNMENT = "%%ALIGNMENT%%";
        public const string UNDEFINED = "<b>UNDEFINED</b>";

        public static List<string> IGNORED_ICONS => new()
        {
            EStateEffectProperty.Tick.ToString(),
            EStateEffectProperty.Level.ToString(),

            ESpellProperty.Size.ToString(),
            ESpellProperty.Delay.ToString(),
            ESpellProperty.DelayBetweenLaunches.ToString(),
            ESpellProperty.DelayBetweenWaves.ToString(),
            ESpellProperty.DurationTick.ToString(),
            ESpellProperty.NProjectiles.ToString(),
            ESpellProperty.GrowSizeFactor.ToString(),
        };

 
        #region Cleaning 

        public static string Clean(string text)
        {
            CleanAlignment(ref text);

            return text;
        }

        public static string CleanMaterialName(string name)
        {
            const string instanceSuffix = " (Instance)";
            if (name.EndsWith(instanceSuffix))
            {
                return name.Substring(0, name.Length - instanceSuffix.Length);
            }
            return name;
        }

        static void CleanAlignment(ref string text)
        {
            // check if has tag
            if (!HasTag(text, TAG_ALIGNMENT))
                return;

            // =================================================================================================
            // FIND ALIGNEMENT POSITIONS

            // split text line by line
            string[] lignes = text.Split("\n");

            // get max alignement pos of each ALIGNMENT sections (can be multiples in one line)
            List<int> alignementPos = new(0);
            foreach (string line in lignes)
            {
                // skip line if has no TAG
                if (!HasTag(line, TAG_ALIGNMENT))
                    continue;

                string[] lineSplits = line.Split(TAG_ALIGNMENT);
                for (int i = 0; i < lineSplits.Length - 1; i++)
                {
                    // save provided position of the alignement if is sup to last one (or if does not exists)
                    if (i < alignementPos.Count)
                        alignementPos[i] = Math.Max(lineSplits[i].Length, alignementPos[i]);
                    else
                        alignementPos.Add(lineSplits[i].Length);
                }
            }

            // =================================================================================================
            // APPLY ALIGNEMENT

            // reset text
            text = "";
            foreach (string line in lignes)
            {
                string lineCleaned = "";
                string[] lineSplits = line.Split(TAG_ALIGNMENT);
                for (int i = 0; i < lineSplits.Length; i++)
                {
                    // check if must add spaces (for the alignement) before adding the next line
                    string spaces = i > 0 ? string.Concat(Enumerable.Repeat(" ", 2 * (alignementPos[i - 1] - lineSplits[i - 1].Length))) : "";
                    lineCleaned += spaces + lineSplits[i];
                }

                text += (text != "" ? "\n" : "") + lineCleaned;
            }
        }

        /// <summary>
        /// Clean the effect name of the quest to get the base name of the effect 
        /// <example>
        ///     "_QuestBurnBurnBurn - Major" -> "BurnBurnBurn"
        /// </example>
        /// </summary>
        /// <param name="effectName"></param>
        /// <returns></returns>
        public static string TrimQuestEffectName(string effectName, bool isActivated = true)
        {
            // remove "_Quest" prefix
            if (effectName.StartsWith("_Quest"))
                effectName = effectName.Substring(6);

            // remove tune activation suffix (ex: "Effect - Minor" -> "Effect")
            if (effectName.Contains(" - "))
                effectName = effectName.Split(" - ")[0];

            if (!isActivated)
                effectName += "_Inactive";

            return effectName;
        }

        static bool HasTag(string text, string tag)
        {
            return text.Contains(tag);
        }

        #endregion


        #region Format

        public static string Split(string text, string by = "_")
        {
            if (text == null || text == "")
                return "";

            if (! text.Contains(" "))
                text = SplitCamelCase(text);

            return text.Replace(by, " ");
        }

        public static string SplitCamelCase(string input)
        {
            if (input == null || input == "")
                return "";

            // Use regular expression to split UpperCamelCase string with spaces
            string output = Regex.Replace(input, "(\\B[A-Z])", " $1");

            // Replace dots by spaces (in case of special condition attached)
            output = output.Replace(".", " ");

            // Convert first character to uppercase
            return char.ToUpper(output[0]) + output.Substring(1);
        }

        public static string FormatSpecialPropertyName(string propertyName, string specialCondition)
        {
            if (specialCondition.IsNullOrEmpty())
                return propertyName;

            return $"{propertyName}.{specialCondition}";
        }

        public static bool IsSpecialPropertyName(string baseName, out string propertyName, out string specialCondition)
        {
            propertyName = baseName;
            specialCondition = "";
            if (! baseName.Contains("."))
                return false;

            var split = baseName.Split(".");
            if (split.Count() > 2)
            {
                ErrorHandler.Warning("Multiple . found in property name : " + baseName);
                return false;
            }

            propertyName = split[0];
            specialCondition = split[1];
            return true;
        }

        public static string FormatPropertyValue(float value, string propertyName)
        {
            if (CharacterData.CheckIsInt(propertyName))
                return value.ToString("0");

            if (CharacterData.CheckIsPercentageValue(propertyName))
                return FormatPercValue(value);

            return value.ToString(Mathf.Round(value) == value ? "0" : "F2");
        }

        public static string FormatPercValue(float value)
        {
            return (value >= 0.01 ? Mathf.Round(value * 100).ToString("0") : (Mathf.Round(value * 1000) / 10).ToString("F1")) + "%";
        }

        public static string FormatNumericalString(int number, string separator = " ")
        {
            string MyString = number.ToString();
            StringBuilder formattedNumber = new StringBuilder();

            // Iterate over the characters of the input number from right to left
            for (int i = MyString.Length - 1, count = 0; i >= 0; i--)
            {
                // Add the current character to the formatted string
                formattedNumber.Insert(0, MyString[i]);

                // Insert a space after every third character, except for the last character
                if (++count % 3 == 0 && i != 0)
                {
                    formattedNumber.Insert(0, separator); // Insert a space
                }
            }

            return formattedNumber.ToString();
        }

        /// <summary>
        /// Format a timestamp into "HH : MM : SS"
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static string FormatTimestamp(int timestamp)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timestamp);
            return string.Format("{0:D2} : {1:D2} : {2:D2}", timeSpan.Days * 24 + timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        public static string ToRoman(int number)
        {
            switch (number)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                case 4:
                    return "IV";
                case 5:
                    return "V";

                default:
                    ErrorHandler.Error("Unable to transform " + number + " into roman value");
                    return "";
            }
        }

        public static int FromRoman(string number)
        {
            switch (number)
            {
                case "I":
                    return 1;
                case "II":
                    return 2;
                case "III":
                    return 3;
                case "IV":
                    return 4;
                case "V":
                    return 5;

                default:
                    ErrorHandler.Error("Unable to transform " + number + " into roman value");
                    return 0;
            }
        }

        /// <summary>
        /// Format description name of a state effect to add the icon if necessary
        /// </summary>
        /// <param name="stateEffectName"></param>
        /// <param name="withIcon"></param>
        /// <returns></returns>
        public static string FormatStateEffectIcon(string stateEffectName, bool withIcon = true, bool withPropertyName = true)
        {
            string formatedString = "";
            stateEffectName = stateEffectName.Trim();

            if (withPropertyName)
                formatedString += $"<i>{stateEffectName}</i>";

            if (withIcon)
                formatedString += FormatIcon(stateEffectName);

            return formatedString;
        }

        public static string FormatPropertyIcon(string propertyName, object propertyValue, bool withIcon = true, bool withPropertyName = true, EScalingDirection scaling = EScalingDirection.None)
        {
            string formatedString;
            if (float.TryParse(propertyValue.ToString(), out float fValue))
                formatedString = $"<b>{FormatPropertyValue(fValue, propertyName)}</b>";
            else
                formatedString = $"<b>{propertyValue}</b>";

            // apply color if value is scaling
            formatedString = FormatScaling(formatedString, scaling);

            // add name of the property if requested
            if (withPropertyName)
                formatedString += $" <i>{propertyName}</i>";

            // add icon of the property if requested
            if (withIcon)
                formatedString += FormatIcon(propertyName);

            // return formated string
            return formatedString;
        }

        public static string FormatScaling(string value, EScalingDirection scaling)
        {
            switch (scaling)
            {
                case EScalingDirection.None:
                    return $"<color={Color.yellow.ToHex()}>{value}</color>";

                case EScalingDirection.Up:
                    return $"<color={Color.green.ToHex()}>{value}</color>";

                case EScalingDirection.Down:
                    return $"<color={Color.red.ToHex()}>{value}</color>";

                default:
                    ErrorHandler.Warning("Unahandled case : " + scaling);
                    return value;
            }
        }

        public static string FormatIcon(string propName)
        {
            if (IGNORED_ICONS.Contains(propName))
                return "";
            
            return $" <sprite name=\"{"Ic_" + propName}\">";
        }

        public static string FormatActivatedText(string text, bool isActivated, bool isOverwritten)
        {
            if (! isActivated)
                text = "<i><color=#FF000088>" + text + "</color></i>";

            if (isOverwritten)
                text = "<s>" + text + "</s>";

            return text;         
        }

        #endregion


        #region Replace Token

        public static string ReplaceStateEffectProperties(string text, StateEffect stateEffect)
        {
            // Matches patterns like [Property] or [Property.SpecialCondition]
            string pattern = @"\[(\w+)(?:\.(\w+))?\]";

            return Regex.Replace(text, pattern, match =>
            {
                string propertyStr = match.Groups[1].Value;
                string specialCondition = match.Groups[2].Success ? match.Groups[2].Value : null;

                if (!Enum.TryParse(propertyStr, out EStateEffectProperty property))
                {
                    Debug.LogWarning($"Invalid property: {propertyStr}");
                    return match.Value; // leave the original text unchanged
                }

                object value = stateEffect.GetProperty(property, specialCondition: specialCondition);

                // is float : format into clean string
                if (float.TryParse(value.ToString(), out float fValue))
                    value = FormatPropertyValue(fValue, property.ToString());

                if (stateEffect.IsScalingProperty(property, out EScalingDirection scaling))
                    value = FormatScaling(value.ToString(), scaling);

                return value?.ToString() ?? string.Empty;
            });
        }

        public static string ReplaceCharacterStat(string text, List<SCharacterStatScaling> characterStats, int level)
        {
            // Matches patterns like [Property] or [Property.SpecialCondition]
            string pattern = @"\[(\w+)(?:\.(\w+))?\]";

            return Regex.Replace(text, pattern, match =>
            {
                string propertyStr = match.Groups[1].Value;
                string specialCondition = match.Groups[2].Success ? match.Groups[2].Value : null;

                if (!Enum.TryParse(propertyStr, out EStateEffectProperty property))
                {
                    ErrorHandler.Warning($"Invalid property: {propertyStr}");
                    return match.Value; // leave the original text unchanged
                }

                // filter bonus stats
                characterStats = characterStats.Where(t => t.StateEffectProperty == property && t.HasSpecialCondition(specialCondition)).ToList();
                if (characterStats.Count() == 0)
                {
                    ErrorHandler.Warning($"Invalid property: {propertyStr}");
                    return "";
                }

                object value = characterStats[0].GetValue(level);

                // is float : format into clean string
                if (float.TryParse(value.ToString(), out float fValue))
                    value = FormatPropertyValue(fValue, property.ToString());

                value = FormatScaling(value.ToString(), characterStats[0].ScalingDirection);
                return value?.ToString() ?? string.Empty;
            });
        }

        public static string ReplaceStateEffectTokens(string text)
        {
            foreach (EStateEffect stateEffect in Enum.GetValues(typeof(EStateEffect)))
            {
                text = text.Replace("[" + stateEffect.ToString() + "]", FormatStateEffectIcon(stateEffect.ToString(), true));
            }
            return text;
        }

        public static string ReplaceTriggerEffectTokens(string text, List<STriggerEffect> triggerEffects)
        {
            for (int i = 0; i < triggerEffects.Count; i++)
            {
                string token = $"[TriggerEffect.{i}]";
                if (text.Contains(token))
                    text = text.Replace(token, GetTriggerEffectDescription(triggerEffects[i]));
            }
            return text;
        }

        public static string ReplaceSubSpellData(string text, SpellData subSpellData)
        {
            string pattern = @"\[(SubSpellData\.[A-Za-z_][A-Za-z0-9_]*)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            if (subSpellData == null)
            {
                if (matches.Count > 0)
                    ErrorHandler.Error("Found SubSpell tokens in description but no subSpelLData were provided");
                return text;
            }

            foreach (Match match in matches)
            {
                string token = match.Value;
                string propertyName = token.Split('.')[1].TrimEnd(']');
                int tokenIndex = match.Index;

                // Get preceding character ignoring spaces
                char precedingChar = ' ';
                for (int i = tokenIndex - 1; i >= 0; i--)
                {
                    char c = text[i];
                    if (!char.IsWhiteSpace(c))
                    {
                        precedingChar = c;
                        break;
                    }
                }

                bool lowerFirstChar = precedingChar == '.' || precedingChar == '\n';

                string replacement = propertyName == "Description"
                    ? subSpellData.GetDescription()
                    : subSpellData.ConvertDescriptionVariable(new SDescriptionVariable(propertyName, true), subSpellData.GetInfo());

                if (lowerFirstChar)
                    replacement = replacement.FirstCharacterToLower();

                text = text.Replace(token, replacement);
            }

            return text;
        }

        public static string GetTriggerEffectDescription(STriggerEffect triggerEffect)
        {
            // description of the Rune is the description of the Trigger Effect
            if (SpellLoader.IsSpell(triggerEffect.SpellDataName))
            {
                return SpellLoader.GetSpellDescription(triggerEffect.SpellDataName, triggerEffect.Level);
            }

            // description of the Rune is the description of the Trigger Effect 
            else if (SpellLoader.IsStateEffect(triggerEffect.SpellDataName))
            {
                return SpellLoader.GetStateEffectDescription(triggerEffect.SpellDataName, triggerEffect.Level, overridingData: triggerEffect.OverridingData);
            }

            ErrorHandler.Error("Unable to find description for trigger effect " + triggerEffect.SpellDataName);
            return "";
        }

        public static string ReplaceSpellRequirements(string text, SpellData spellData)
        {
            // Define a regex to find tokens in the format [SubSpellData.PROPERTY_NAME]
            string pattern = @"\[(SpellRequirement\.[0-9]+)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                string token = match.Value; // The full token, e.g., "[SubSpellData.PROPERTY_NAME]"

                // Extract the index and target from name
                int index = int.Parse(token.Split('.')[1].TrimEnd(']'));
                if (index < 0)
                {
                    ErrorHandler.Error("Error with token " + token + " : BAD index (" + index + ")");
                    continue;
                }

                if (spellData.SpellRequirements.Count <= index)
                {
                    ErrorHandler.Error("Error with token " + token + " : index (" + index + ") is >= number of SpellRequirements (" + spellData.SpellRequirements.Count + ")" );
                    continue;
                }

                // Replace token with the property value from ConvertDescriptionVariable
                text = text.Replace(
                    token,
                    spellData.SpellRequirements[index].GetDescription()
                );
            }

            return text;
        }

        public static string ReplaceSubStateEffects(string text, List<SStateEffectData> stateEffects)
        {
            // Updated regex to match both ".Description" and ".EffectDescription"
            string pattern = @"\[([a-zA-Z]*StateEffect\.[0-9]+)\.(Description|EffectDescription)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                string token = match.Value;  // Full token, e.g., "[SubSpellStateEffect.2.Description]"
                string baseToken = match.Groups[1].Value;  // Extracts "SubSpellStateEffect.2"
                string property = match.Groups[2].Value;  // Extracts "Description" or "EffectDescription"

                // Extract index safely
                int index;
                if (!int.TryParse(baseToken.Split('.')[1], out index))
                {
                    ErrorHandler.Error("❌ Error parsing index from token: " + token);
                    continue;
                }

                if (index < 0 || index >= stateEffects.Count)
                {
                    ErrorHandler.Error($"❌ Error with token {token}: Index ({index}) is out of range (Max: {stateEffects.Count - 1})");
                    continue;
                }

                // Determine the correct replacement based on property type
                string replacement = (property == "Description")
                    ? stateEffects[index].Description
                    : stateEffects[index].EffectDescription;

                // Replace token in text
                text = text.Replace(token, replacement);
            }

            return text;
        }

        public static string ReplaceSubStateEffects(string text, SpellData spellData)
        {
            // Define a regex to find tokens in the format [StateEffect.N_EFFECT]
            string pattern = @"\[((Enemy|Ally)*StateEffect\.[0-9]+)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                string token = match.Value;

                // Extract the index and target from name
                int index = int.Parse(token.Split('.')[1].TrimEnd(']'));
                List<SStateEffectData> stateEffects = token.Split("StateEffect.")[0].Equals("Ally") ? spellData.AllyStateEffects : spellData.EnemyStateEffects;

                if (index < 0)
                {
                    ErrorHandler.Error("Error with token " + token + " : BAD index (" + index + ")");
                    continue;
                }

                if (stateEffects.Count <= index)
                {
                    ErrorHandler.Error("Error with token " + token + " : index (" + index + ") is >= number of StateEffects (" + stateEffects.Count + ")" );
                    continue;
                }

                var stateEffect = stateEffects[index];
                stateEffect.SetLevel(spellData.Level);

                // Replace token with the property value from ConvertDescriptionVariable
                text = text.Replace(
                    token,
                    stateEffect.Description
                );
            }

            return text;
        }

        #endregion


        #region ToString()

        public static string ToString(object obj, int indent = 1)
        {
            StringBuilder sb = new StringBuilder();
            switch (obj)
            {
                case null:
                    sb.Append("null");
                    break;

                case double d:
                    sb.Append(d.ToString("G17"));
                    break;

                case Enum em:
                    sb.Append(em.ToString());
                    break;

                case IDictionary dict:
                    {
                        sb.Append("[\n");
                        var i = 0;
                        foreach (var key in dict.Keys)
                        {
                            sb.Append(new string(Enumerable.Repeat(' ', indent * 2).ToArray()));
                            sb.Append('"');
                            sb.Append(key);
                            sb.Append("\": ");
                            sb.Append(ToString(dict[key], indent + 1));

                            if (i < dict.Count - 1) sb.Append(",\n");
                            i++;
                        }

                        sb.Append('\n');
                        sb.Append(new string(Enumerable.Repeat(' ', indent * 2).ToArray()));
                        sb.Append(']');
                        break;
                    }

                case IList iList:
                    var list = iList.Cast<object>().ToList();

                    sb.Append("[\n");
                    for (var i = 0; i < list.Count; i++)
                    {
                        sb.Append(new string(Enumerable.Repeat(' ', indent * 2).ToArray()));
                        sb.Append(ToString(list[i],indent + 1));
                        if (i < list.Count - 1) sb.Append(",\n");
                    }

                    sb.Append('\n');
                    sb.Append(new string(Enumerable.Repeat(' ', indent * 2).ToArray()));
                    sb.Append(']');
                    break;

                default:
                    sb.Append(obj != null ? obj.ToString() : "null");
                    break;
            }

            return sb.ToString();
        }

        #endregion
    }
}