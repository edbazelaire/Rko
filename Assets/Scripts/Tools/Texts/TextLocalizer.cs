using System;
using System.Text.RegularExpressions;

namespace Tools
{
    public static class TextLocalizer
    {
        #region Members

        const string TIMER_FORMAT_HMS = "{0:D2} : {1:D2} : {2:D2}";

        #endregion


        #region Text Display
        public static string LocalizeText(string text)
        {
            return text;
        }

        #endregion


        #region Formats

        public static string SplitCamelCase(string input)
        {
            if (input == null || input == "")
                return "";

            // Use regular expression to split UpperCamelCase string with spaces
            string output = Regex.Replace(input, "(\\B[A-Z])", " $1");

            // Convert first character to uppercase
            return char.ToUpper(output[0]) + output.Substring(1);
        }

        public static string GetAsCounter(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return string.Format(TIMER_FORMAT_HMS,
                time.Hours,
                time.Minutes,
                time.Seconds
            );
        }

        #endregion

    }
}