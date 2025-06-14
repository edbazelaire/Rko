#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Enums;
using System.Collections.Generic;

namespace Tools.GUITools
{
    [CustomPropertyDrawer(typeof(ESpell))]
    public class PropertyDrawerESpell : PropertyDrawer
    {
        // Static dictionaries to store state per-property
        private static Dictionary<string, bool> searchToggles = new Dictionary<string, bool>();
        private static Dictionary<string, string> searchStrings = new Dictionary<string, string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            float y = position.y;

            string key = property.propertyPath;

            // Get or init toggle state
            if (!searchToggles.ContainsKey(key)) searchToggles[key] = false;
            if (!searchStrings.ContainsKey(key)) searchStrings[key] = "";

            // Toggle search
            Rect toggleRect = new Rect(position.x, y, position.width, lineHeight);
            searchToggles[key] = EditorGUI.ToggleLeft(toggleRect, "Enable Spell Search", searchToggles[key]);
            y += lineHeight + spacing;

            // Optional search field
            if (searchToggles[key])
            {
                Rect searchRect = new Rect(position.x, y, position.width, lineHeight);
                searchStrings[key] = EditorGUI.TextField(searchRect, "Search", searchStrings[key]);
                y += lineHeight + spacing;
            }

            // Get enum values
            var enumValues = Enum.GetValues(typeof(ESpell)).Cast<ESpell>().ToList();
            var filtered = searchToggles[key]
                ? enumValues.Where(e => e.ToString().ToLower().Contains(searchStrings[key].ToLower())).OrderBy(e => e.ToString()).ToList()
                : enumValues;

            // Current value
            var currentValue = (ESpell)property.intValue;
            int currentIndex = filtered.IndexOf(currentValue);
            if (currentIndex < 0) currentIndex = 0;

            string[] options = filtered.Select(e => e.ToString()).ToArray();
            Rect popupRect = new Rect(position.x, y, position.width, lineHeight);
            int newIndex = EditorGUI.Popup(popupRect, label.text, currentIndex, options);

            if (newIndex >= 0 && newIndex < filtered.Count)
                property.intValue = (int)filtered[newIndex];

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string key = property.propertyPath;
            int lines = 2; // toggle + dropdown
            if (searchToggles.ContainsKey(key) && searchToggles[key])
                lines++; // + search field

            return lines * (EditorGUIUtility.singleLineHeight + 2f);
        }
    }
}

#endif
