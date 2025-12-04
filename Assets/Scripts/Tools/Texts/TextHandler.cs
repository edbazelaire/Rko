using AI;
using Data;
using Data.DataStructures;
using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.SpellSubStructures;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Loaders;
using Game.Spells;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            ESpellProperty.Cooldown.ToString(),
            ESpellProperty.Size.ToString(),
            ESpellProperty.Delay.ToString(),
            ESpellProperty.DelayBetweenLaunches.ToString(),
            ESpellProperty.DelayBetweenWaves.ToString(),
            ESpellProperty.DurationTick.ToString(),
            ESpellProperty.NProjectiles.ToString(),
            ESpellProperty.GrowSizeFactor.ToString(),

            EStateEffect.Purity.ToString(),
        };

        public static Dictionary<string, string> KEY_WORDS = new()
        {
            { "ArenaQuest",
                "Arena Quest : Stacks of this effect are carried on through the entire Arena."},

            { "RedirectableCast",
                "Redirectable Cast: the spell can be redirected during casting"},
            
            { "ExecutionDamage_Key",
                "Execution Damage : Damage that increases as the target’s health decreases: 0% of its value at full HP, up to 100% at 0% HP."},

            { "TrueDamage_Key",
                "True Damage : Damage that ignores all resistances and reductions."},

            { "DotDamage_Key",
                "DoT Damage : Damage dealt over time, applied at each tick until the effect ends."},
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

        public static string FormatPropertyValue(float value, string propertyName, EScalingDirection? scalingDirection = null)
        {
            string propertyValue;
            if (CharacterData.CheckIsInt(propertyName) && ! (value > 0 && value < 1))
                propertyValue = value.ToString("0");

            else if (CharacterData.CheckIsPercentageValue(propertyName))
                propertyValue = FormatPercValue(value);

            else
                propertyValue = value.ToString(Mathf.Round(value) == value ? "0" : "F2");

            if (scalingDirection != null)
                propertyValue = FormatScaling(propertyValue, scalingDirection.Value);

            return propertyValue;
        }

        public static string FormatPercValue(float value)
        {
            if (value >= 0.01)
                return (value * 100).ToString("0") + "%";
            
            if (value >= 0.001)
                return (value * 100).ToString("F1") + "%";
            
            return (value * 100).ToString("F2") + "%";
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

        public static string FirstCharToLowerCase(string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];

            return str;
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
                case "Ⅰ":
                case "I":
                    return 1;
                case "Ⅱ":
                case "II":
                    return 2;
                case "Ⅲ":
                case "III":
                    return 3;
                case "Ⅳ":
                case "IV":
                    return 4;
                case "Ⅴ":
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

            if (withIcon && ! IGNORED_ICONS.Contains(stateEffectName))
                formatedString += FormatIcon(stateEffectName);

            return formatedString;
        }

        public static string FormatPropertyName(string propertyName)
        {
            if (propertyName.EndsWith("Perc"))
                propertyName = propertyName.Substring(0, propertyName.Length - 4);

            return SplitCamelCase(propertyName);
        }

        public static string FormatPropertyIcon(string propertyName, object propertyValue, bool? withIcon = null, bool withPropertyName = true, EScalingDirection scaling = EScalingDirection.None)
        {
            string formatedString = "";
            if (propertyValue != null)
            {
                if (float.TryParse(propertyValue.ToString(), out float fValue))
                    formatedString = $"<b>{FormatPropertyValue(fValue, propertyName)}</b>";
                else
                    formatedString = $"<b>{propertyValue}</b>";

                // apply color if value is scaling
                formatedString = FormatScaling(formatedString, scaling);
            }

            // add name of the property if requested
            if (withPropertyName)
            {
                if (formatedString != "")
                    formatedString += " ";
                formatedString += $"<i>{Split(propertyName)}</i>";
            }
                
            // add icon of the property if requested
            if (withIcon ?? ! IGNORED_ICONS.Contains(propertyName))
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


        #region Price 

        public static string FormatPrice(float price, ECurrency currency)
        {
            if (price <= 0)
                return "Free";

            string value = Mathf.Round(price) == price ? price.ToString("0") : price.ToString("F2");

            if (currency == ECurrency.Real)
                value += GetLocalCurrencySymbol();

            return value;
        }

        public static string GetLocalCurrencySymbol()
        {
            var culture = CultureInfo.CurrentCulture;
            var region = new RegionInfo(culture.Name);

            return region.CurrencySymbol;  // e.g., "$", "€", "¥"
        }

        #endregion


        #region Replace Token

        public static string ReplaceKeyWords(string description)
        {
            // Matches patterns like {{KEY_WORD}}
            string pattern = @"\{(\w+)\}";

            return Regex.Replace(description, pattern, match =>
            {
                string keyWord = match.Groups[1].Value;

                if (KEY_WORDS.ContainsKey(keyWord))
                {
                    return KEY_WORDS[keyWord];
                } 
                else
                {
                    return FormatPropertyIcon(keyWord, null, true, withPropertyName: true);
                }
            });
        }

        /// <summary>
        /// Convert a description variable into a string implemented into the description
        /// </summary>
        /// <returns></returns>
        public static string ReplaceProperties(string description, Dictionary<string, object> infos, CollectableData data)
        {
            if (infos.ContainsKey("Damages"))
                description = ReplaceDamageTokens(description, infos["Damages"] as List<SDamage>, data.Level);

            // Matches patterns like [Property] or [Property.SpecialCondition]
            string pattern = @"\[(\w+)\]";

            return Regex.Replace(description, pattern, match =>
            {
                string propertyStr = match.Groups[1].Value;
                string specialCondition = match.Groups[2].Success ? match.Groups[2].Value : null;

                // try to convert name of the property if needed
                PropertyHandler.ConvertSpecialPropertyName(ref propertyStr);

                if (propertyStr == "Level")
                {
                    return data.Level.ToString();
                }
                else if (! infos.ContainsKey(propertyStr))
                {
                    return match.Value; // leave the original text unchanged
                }

                object value = infos[propertyStr];
                data.IsScalingProperty(propertyStr, out EScalingDirection scaling);

                return FormatPropertyIcon(propertyStr, value, true, withPropertyName: false, scaling: scaling);
            });
        }

        /// <summary>
        /// Replaces [Overriding.Property] or [Overriding.Property.SpecialCondition] tokens in a text
        /// using values defined in a list of SOverridingData.
        /// </summary>
        /// <param name="text">Input description containing tokens (e.g. "Deals [Overriding.Damage] damage")</param>
        /// <param name="overridingData">List of overrides (properties, scaling, values)</param>
        /// <param name="level">Current level to compute scaling</param>
        /// <returns>Formatted string with tokens replaced by their computed values</returns>
        public static string ReplaceSpellOverrides(string text, List<SOverridingData> overridingData, int level)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (overridingData == null || overridingData.Count == 0)
                return text;

            // Pattern: [Overriding.Property] or [Overriding.Property.SpecialCondition]
            string pattern = @"\[Overriding\.([A-Za-z0-9_]+)(?:\.([A-Za-z0-9_]+))?\]";

            return Regex.Replace(text, pattern, match =>
            {
                string propertyStr = match.Groups[1].Value;             // e.g. "Damage"
                string specialCondition = match.Groups[2].Success
                    ? match.Groups[2].Value                             // e.g. "Fire"
                    : null;

                // Try to find matching override data
                var overrideEntry = overridingData.FirstOrDefault(t => t.Property.ToString().Equals(propertyStr, StringComparison.OrdinalIgnoreCase));

                if (overrideEntry == null)
                {
                    ErrorHandler.Warning($"[OverridingData] Missing override for '{propertyStr}' in description.");
                    return $"<missing:{propertyStr}>";
                }

                // Compute scaled value
                float baseValue = 0f;
                float resultValue = overrideEntry.Get(baseValue, level);

                // Format final numeric value
                string formattedValue = TextHandler.FormatPropertyValue(resultValue, overrideEntry.Property.ToString(), overrideEntry.SpellPropertyScaling.GetScalingDirection());
                return formattedValue ?? string.Empty;
            });
        }

        /// <summary>
        /// Replaces [Overriding.Property] or [Overriding.Property.SpecialCondition] tokens in a text
        /// based on a list of SBonusStatsOverride (used inside StateEffects).
        /// </summary>
        /// <param name="text">The description text (e.g. "Gain [Overriding.AttackPower] for 5s")</param>
        /// <param name="bonusStatsOverrides">The list of overrides applied by the StateEffect</param>
        /// <param name="level">The current level used for scaling</param>
        /// <returns>The description text with all [Overriding.*] tokens replaced by computed values</returns>
        public static string ReplaceStateEffectOverridingProperties(string text, List<SBonusStatsOverride> bonusStatsOverrides, int level)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (bonusStatsOverrides == null || bonusStatsOverrides.Count == 0)
                return text;

            // Matches [Overriding.Property] or [Overriding.Property.SpecialCondition]
            string pattern = @"\[Overriding\.([A-Za-z0-9_]+)(?:\.([A-Za-z0-9_]+))?\]";

            return Regex.Replace(text, pattern, match =>
            {
                string propertyStr = match.Groups[1].Value;              // e.g. "Damage"
                string specialCondition = match.Groups[2].Success
                    ? match.Groups[2].Value                              // e.g. "Fire"
                    : null;

                // --- Find matching bonus stat
                var overrideEntry = bonusStatsOverrides.FirstOrDefault(t =>
                    t.BonusStat != null 
                    && t.BonusStat.StateEffectProperty.ToString().Equals(propertyStr, StringComparison.OrdinalIgnoreCase) 
                    && t.BonusStat.CheckConditions(null, null, specialCondition: specialCondition));

                if (overrideEntry == null)
                {
                    ErrorHandler.Warning($"[Overriding] Unable to find any stat '{propertyStr}' in bonusStatsOverrides.");
                    return $"<missing:{propertyStr}>"; // keeps placeholder visible in case of missing data
                }

                SBonusStats bonusStat = overrideEntry.BonusStat;

                // --- Compute base value at current level
                float computedValue = bonusStat.Get(level, 1, specialCondition: specialCondition);

                // --- Format output for display
                string valueStr = FormatPropertyValue(computedValue, bonusStat.StateEffectProperty.ToString());
                valueStr = FormatScaling(valueStr, bonusStat.ScalingDirection);

                return valueStr ?? string.Empty;
            });
        }

        /// <summary>
        /// Replace propert
        /// </summary>
        /// <param name="text"></param>
        /// <param name="stateEffect"></param>
        /// <returns></returns>
        public static string ReplaceStateEffectProperties(string text, StateEffect stateEffect)
        {
            // Matches patterns like [Property] or [Property.SpecialCondition]
            string pattern = @"\[(\w+)(?:\.(\w+))?\]";

            return Regex.Replace(text, pattern, match =>
            {
                string propertyStr = match.Groups[1].Value;

                object value;
                if (stateEffect.TryGetSpecialPropertyValue(propertyStr, out value))
                    return value.ToString();


                if (!Enum.TryParse(propertyStr, out EStateEffectProperty property))
                {
                    return match.Value; // leave the original text unchanged
                }

                EDamageCategory? damageCategory = null;
                EHitCategory? hitCategory = null;
                string specialCondition = "";

                if (match.Groups.Count > 2)
                {
                    for (int i = 2; i < match.Groups.Count(); i++)
                    {
                        if (Enum.TryParse(match.Groups[i].Value, out EDamageCategory dc))
                        {
                            damageCategory = dc;
                        } else if (Enum.TryParse(match.Groups[i].Value, out EHitCategory hc))
                        {
                            hitCategory = hc;
                        } else
                        {
                            specialCondition = match.Groups[i].Value;
                        }
                    }
                }
                
                value = stateEffect.GetProperty(property, damageCategory: damageCategory, hitCategory: hitCategory, specialCondition: specialCondition); ;

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
                var subCharacterStats = characterStats.Where(t => t.StateEffectProperty == property && t.HasSpecialCondition(specialCondition)).ToList();
                if (subCharacterStats.Count() == 0)
                {
                    ErrorHandler.Warning($"Invalid property: {propertyStr}");
                    return "";
                }

                object value = subCharacterStats[0].GetValue(level);

                // is float : format into clean string
                if (float.TryParse(value.ToString(), out float fValue))
                    value = FormatPropertyValue(fValue, property.ToString());

                value = FormatScaling(value.ToString(), subCharacterStats[0].ScalingDirection);
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
            if (triggerEffects == null || triggerEffects.Count == 0 || string.IsNullOrEmpty(text))
                return text;

            // Regex : [TriggerEffect.<index>(.<property>)*]
            var regex = new Regex(@"\[TriggerEffect\.(\d+)(?:\.(\w+))?\]", RegexOptions.Compiled);

            return regex.Replace(text, match =>
            {
                // get Index
                if (!int.TryParse(match.Groups[1].Value, out int index) || index < 0 || index >= triggerEffects.Count)
                    return match.Value; // not valid

                var trigger = triggerEffects[index];

                // get property
                string property = match.Groups[2].Success ? match.Groups[2].Value : null;

                if (string.IsNullOrEmpty(property))
                {
                    // not property -> return effect description
                    return GetTriggerEffectDescription(trigger);
                }

                // get requested property
                if (trigger.TryGetProperty(property, out string value))
                    return FormatScaling(value, EScalingDirection.None);

                // proparty not found -> return unchanged value
                return match.Value;
            });
        }


        public static string ReplaceSpawnTokens(string text, List<CharacterData> spawns)
        {
            for (int i = 0; i < spawns.Count; i++)
            {
                string token = $"[Spawn.{i}]";
                if (text.Contains(token))
                    text = text.Replace(token, spawns[i].GetDescription());
            }
            return text;
        }

        public static string ReplaceStatConversion(string text, List<SStatConversion> statConversions, int level)
        {
            for (int i = 0; i < statConversions.Count; i++)
            {
                string token = $"[StatConversion.{i}]";
                if (text.Contains(token))
                    text = text.Replace(token, statConversions[i].GetDescription(level, stacks: 1));
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
                    replacement = FirstCharToLowerCase(replacement);

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
            const string pattern = @"\[(?:(?<side>Enemy|Ally))?StateEffect\.(?<index>\d+)(?:\.(?<dtype>Description|EffectDescription))?\]";

            return Regex.Replace(text, pattern, m =>
            {
                var side = m.Groups["side"].Success ? m.Groups["side"].Value : "Enemy"; // défaut côté Enemy si absent
                var index = int.Parse(m.Groups["index"].Value);
                var dtype = m.Groups["dtype"].Success ? m.Groups["dtype"].Value : null;

                List<SStateEffectData> stateEffects = new();
                stateEffects.AddRange(side == "Ally" ? spellData.AllyStateEffects : spellData.EnemyStateEffects);

                // add Persistant State Effects to be accessible by replacement
                if (spellData is ZoneData zoneData && ! zoneData.PersistentStateEffects.IsNullOrEmpty())
                    stateEffects.AddRange(zoneData.PersistentStateEffects);

                // add OnHit State Effects to be accessible by replacement
                if (! spellData.OnHit.IsNullOrEmpty())
                {
                    foreach (SpellData onHitData in spellData.OnHit)
                    {
                        stateEffects.AddRange(side == "Ally" ? onHitData.AllyStateEffects : onHitData.EnemyStateEffects);
                    }
                }

                if (index < 0 || index >= stateEffects.Count)
                {
                    ErrorHandler.Error($"Error with token {m.Value} : index ({index}) is out of range (0..{stateEffects.Count - 1})");
                    return m.Value; // on laisse le token tel quel
                }

                var stateEffect = stateEffects[index];
                stateEffect.SetLevel(spellData.Level);

                // ajuste les propriétés selon ton modèle
                if (dtype == "Description") return stateEffect.Description;
                if (dtype == "EffectDescription") return stateEffect.EffectDescription;

                // pas de suffixe -> choisis une valeur par défaut
                return stateEffect.Description;
            });
        }

        public static string ReplacePassiveEffects(string text, SpellData spellData)
        {
            // Define a regex to find tokens in the format [StateEffect.N_EFFECT]
            string pattern = @"\[(PassiveEffects\.[0-9]+)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                string token = match.Value;

                // Extract the index and target from name
                int index = int.Parse(token.Split('.')[1].TrimEnd(']'));
                if (index < 0)
                {
                    ErrorHandler.Error("Error with token " + token + " : BAD index (" + index + ")");
                    continue;
                }

                if (spellData.PassiveEffects.Count <= index)
                {
                    ErrorHandler.Error("Error with token " + token + " : index (" + index + ") is >= number of StateEffects (" + spellData.PassiveEffects.Count + ")" );
                    continue;
                }

                StateEffect stateEffect = spellData.PassiveEffects[index].StateEffect.Clone(spellData.Level);

                // Replace token with the state effect description
                text = text.Replace(
                    token,
                    stateEffect.GetDescription()
                );
            }

            return text;
        }

        public static string ReplaceLinkedStateEffect(string text, CounterData spellData)
        {
            // Define a regex to find tokens in the format [StateEffect.N_EFFECT]
            string pattern = @"\[(LinkedStateEffect)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            if (matches.Count == 0)
                return text;

            // Replace token with the property value from ConvertDescriptionVariable
            return text.Replace(
                matches[0].Value,
                spellData.LinkedStateEffect.EffectDescription
            );
        }

        public static string ReplaceSubStateEffect(string text, StateEffect stateEffect)
        {
            // Define a regex to find tokens in the format [StateEffect.N_EFFECT]
            string pattern = @"\[((Enemy|Ally)*StateEffect)\]";
            MatchCollection matches = Regex.Matches(text, pattern);

            if (matches.Count == 0)
                return text;

            // Replace token with the property value from ConvertDescriptionVariable
            return text.Replace(
                matches[0].Value,
                stateEffect.GetDescription()
            );
        }

        #endregion


        #region Damage Tokens

        /// <summary>
        /// Replaces all [Damage...] tokens inside a text with their resolved numeric value and icon,
        /// based on a list of SDamage entries. Each token supports multiple filters joined by dots.
        /// </summary>
        /// <param name="text">Input text (e.g. "Deals [Damage.Magical.Dot] damage")</param>
        /// <param name="damages">List of SDamage entries that describe the spell's damage components</param>
        /// <param name="level">The spell level, used for scaling calculations</param>
        /// <returns>The input text with [Damage...] tags replaced by their computed values and icons</returns>
        public static string ReplaceDamageTokens(string text, List<SDamage> damages, int level)
        {
            if (string.IsNullOrEmpty(text) || damages == null || damages.Count == 0)
                return text;

            // Regex to match patterns like [Damage], [Damage.Physical], [Damage.Magical.Dot], [Damage.Fire.Burn], etc.
            var regex = new Regex(@"\[Damage(?:\.([A-Za-z0-9_.]+))?\]", RegexOptions.IgnoreCase);

            return regex.Replace(text, match =>
            {
                string filtersRaw = match.Groups[1].Success ? match.Groups[1].Value : string.Empty;

                // Split filters by '.' (e.g. "Magical.Dot" → ["Magical", "Dot"])
                var filters = string.IsNullOrEmpty(filtersRaw)
                    ? Array.Empty<string>()
                    : filtersRaw
                        .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(f => f.Trim())
                        .ToArray();

                // --- CASE 1️ : Generic [Damage] (no filters)
                if (filters.Length == 0)
                {
                    return FormatAllDamages(damages, level);
                }

                // --- CASE 2️ : Filtered [Damage....]
                var filtered = FilterDamages(damages, filters);

                // Determine scaling direction (Up, Down or None)
                EScalingDirection scalingDirection = EScalingDirection.None;
                if (filtered.Any(t => t.ScalingDirection == EScalingDirection.Up))
                    scalingDirection = EScalingDirection.Up;
                else if (filtered.Any(t => t.ScalingDirection == EScalingDirection.Down))
                    scalingDirection = EScalingDirection.Down;

                // Sum up values for filtered damages
                float total = filtered.Sum(d => d.Get(level, null, null));

                // Determine icon (if any)
                string icon = GetDamageIcon(filtered.First());

                // Format result
                string dmgText = FormatPropertyValue(total, ESpellProperty.Damage.ToString(), scalingDirection) + FormatIcon(icon);
                if (filtered.Count() == 1)
                {
                    GetDamageModifiersDescription(filtered.First().DamageModifiers, level);
                }
                return dmgText;
            });
        }

        /// <summary>
        /// Formats the special [Damage] token — when no filter is provided.
        /// Displays either a single summarized value or a breakdown of each damage type.
        /// Example:
        ///   - One line: "100 Ic_PhysicalDamage"
        ///   - Multiple lines: "180 (100 Ic_PhysicalDamage + 50 Ic_MagicalDamage + 30 Ic_ExecutionDamage)"
        /// </summary>
        /// <param name="damages">All SDamage entries</param>
        /// <param name="level">The spell level for scaling computation</param>
        /// <returns>A formatted string ready to replace the [Damage] tag</returns>
        private static string FormatAllDamages(List<SDamage> damages, int level)
        {
            // ===================================================================================
            // Single line → simple format
            if (damages.Count == 1)
            {
                var d = damages[0];
                return FormatPropertyValue(damages[0].Get(level, null, null), ESpellProperty.Damage.ToString(), damages[0].ScalingDirection) + FormatIcon(GetDamageIcon(damages[0])) + GetDamageModifiersDescription(damages[0].DamageModifiers, level);
            }

            // ===================================================================================
            // Calculate total and individual entries
            var entries = new List<(float value, string icon, EScalingDirection scaling)>();
            foreach (var dmg in damages)
            {
                float value = dmg.Get(level, null, null);
                string icon = GetDamageIcon(dmg);
                entries.Add((value, icon, dmg.ScalingDirection));
            }

            float total = entries.Sum(e => e.value);

            // Multiple lines → full breakdown
            string breakdown = string.Join(" + ",
                entries.Select(e =>
                    FormatPropertyValue(e.value, ESpellProperty.Damage.ToString(), e.scaling) + FormatIcon(e.icon))
            );

            // Determine global scaling direction for total (Up > Down > None)
            EScalingDirection totalScaling = EScalingDirection.None;
            if (entries.Any(e => e.scaling == EScalingDirection.Up))
                totalScaling = EScalingDirection.Up;
            else if (entries.Any(e => e.scaling == EScalingDirection.Down))
                totalScaling = EScalingDirection.Down;

            // Format total
            string totalValue = FormatPropertyValue(total, ESpellProperty.Damage.ToString(), totalScaling);
            return $"{totalValue} ({breakdown})";
        }

        public static string GetDamageModifiersDescription(List<SDamageModifier> damageModifiers, int level)
        {
            if (damageModifiers.IsNullOrEmpty())
                return "";

            string text = " (";
            foreach (var modifier in damageModifiers)
            {
                text += modifier.GetDescription(level);
            }
            text += ")";

            return text;
        }


        /// <summary>
        /// Filters the given list of SDamage entries based on a set of textual filters.
        /// </summary>
        /// <param name="damages">All possible damage entries</param>
        /// <param name="filters">A list of filters, e.g. ["Magical", "Dot", "Burn"]</param>
        /// <returns>A filtered enumerable of SDamage objects matching all provided filters</returns>
        private static IEnumerable<SDamage> FilterDamages(IEnumerable<SDamage> damages, string[] filters)
        {
            // Start with all damages
            var result = damages;

            // If no filters, default to "Direct" + no special condition
            if (filters.Length == 0)
            {
                return result.Where(d => d.HitCategory == EHitCategory.Direct);
            }

            // Extract known filter types
            var categories = Enum.GetNames(typeof(EDamageCategory))
                .Select(n => n.ToLowerInvariant())
                .ToHashSet();

            var hitTypes = Enum.GetNames(typeof(EHitCategory))
                .Select(n => n.ToLowerInvariant())
                .ToHashSet();

            string? specialCondition = null;
            EDamageCategory? damageCategory = null;
            EHitCategory? hitCategory = null;

            foreach (var f in filters)
            {
                var filter = f.ToLowerInvariant();

                // Check if this filter matches known enums
                if (categories.Contains(filter))
                {
                    if (Enum.TryParse<EDamageCategory>(f, true, out var cat))
                        damageCategory = cat;
                }
                else if (hitTypes.Contains(filter))
                {
                    if (Enum.TryParse<EHitCategory>(f, true, out var hit))
                        hitCategory = hit;
                }
                else
                {
                    // Anything else → treat as special condition
                    specialCondition = f;
                }
            }

            // Apply filters progressively
            if (damageCategory.HasValue)
                result = result.Where(d => d.DamageCategory == damageCategory.Value);

            if (hitCategory.HasValue)
                result = result.Where(d => d.HitCategory == hitCategory.Value);

            // Default fallbacks: assume Direct + no special condition if not specified
            if (!hitCategory.HasValue)
                result = result.Where(d => d.HitCategory == EHitCategory.Direct);

            return result;
        }

        /// <summary>
        /// Determines which icon to display for a group of SDamage values,
        /// based on the filters provided in a [Damage...] tag.
        /// 
        /// Priority rules:
        ///  1. If any filter matches a known HitCategory (e.g. Dot, Execution, Piercing),
        ///     return "Ic_[HitCategory]Damage".
        ///  2. Otherwise, use the DamageCategory of the first matching SDamage (e.g. "Ic_PhysicalDamage").
        ///  3. If no match is found, return an empty string.
        /// </summary>
        /// <param name="damages">The filtered collection of SDamage values (can be empty)</param>
        /// <param name="filters">The filters that appeared in the [Damage...] tag</param>
        /// <returns>A string such as "Ic_DotDamage" or "Ic_PhysicalDamage", or empty if nothing applies</returns>
        private static string GetDamageIcon(SDamage damage)
        {
            // Check has special HitCategory
            if (damage.HitCategory != EHitCategory.Direct)
            {
                // Found a valid HitCategory → return icon based on it
                return $"{damage.HitCategory}Damage";
            }

            return $"{damage.DamageCategory}Damage";
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


        #region Parser

        public static bool TryParseVector2(string str, out Vector2 result)
        {
            result = Vector2.zero;

            if (string.IsNullOrWhiteSpace(str))
                return false;

            // Remove parentheses if present
            str = str.Trim('(', ')', ' ');

            // Split by comma or whitespace
            var parts = str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;

            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                result = new Vector2(x, y);
                return true;
            }

            return false;
        }

        #endregion
    }
}