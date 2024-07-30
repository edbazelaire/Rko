using Data;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    /// <summary>
    /// Handles the cleaning of text, formats, colors, ...
    /// </summary>
    public static class TextHandler
    {
        /// <summary> when put in a text, all lignes will with this tag will have enought spaces to match alignement </summary>
        public const string TAG_ALIGNMENT = "%%ALIGNMENT%%";


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

            // Convert first character to uppercase
            return char.ToUpper(output[0]) + output.Substring(1);
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
            return string.Format("{0:D2} : {1:D2} : {2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
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

        /// <summary>
        /// Format description name of a state effect to add the icon if necessary
        /// </summary>
        /// <param name="stateEffectName"></param>
        /// <param name="withIcon"></param>
        /// <returns></returns>
        public static string FormatStateEffectIcon(string stateEffectName, bool withIcon = true)
        {
            string iconTag = withIcon ? $" <sprite name=\"{"Ic_" + stateEffectName}\">" : "";
            return $"<i>{stateEffectName}</i> {iconTag}";
        }

        #endregion


        #region ToString()

        public static string ToString(object obj, int indent = 1)
        {
            StringBuilder sb = new StringBuilder();
            switch (obj)
            {
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