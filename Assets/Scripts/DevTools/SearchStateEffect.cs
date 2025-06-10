using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using MyBox;

public class SpellDataSearcher: EditorWindow
{
    [MenuItem("Tools/Find StateEffect")]
    private static void FindSpellDataWithEnergy()
    {
        string[] guids = AssetDatabase.FindAssets("t:StateEffect");

        List<Object> matchingSpells = new List<Object>();

        List<string> fields = new() {
            "m_IsTrueDamage",
            //"m_CooldownReduction",
            "m_CooldownReductionPerc",
            //"m_Shield",
            //"m_ResistanceFix",
            "m_ResistancePerc",
            //"m_BonusDamage",
            "m_BonusDamagePerc",
            "m_BonusLifeSteal",
            "m_BonusHeal",
            "m_BonusHealPerc",
            "m_BonusBurnDamage",
            "m_BonusSlowPerc",
            "m_LifeSteal",
        };

        foreach (string fieldName in fields)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset == null) continue;

                var type = asset.GetType();

                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (field == null) continue;

                if (field.FieldType == typeof(int))
                {
                    int value = (int)field.GetValue(asset);
                    if (value != 0)
                    {
                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
                        matchingSpells.Add(asset);
                    }
                } else if (field.FieldType == typeof(float))
                {
                    float value = (float)field.GetValue(asset);
                    if (value != 0)
                    {
                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
                        matchingSpells.Add(asset);
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool value = (bool)field.GetValue(asset);
                    if (value != false)
                    {
                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
                        matchingSpells.Add(asset);
                    }
                }
                else if (field.FieldType == typeof(string))
                {
                    string value = (string)field.GetValue(asset);
                    if (! value.IsNullOrEmpty())
                    {
                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
                        matchingSpells.Add(asset);
                    }
                }
            }
        }

        if (matchingSpells.Count > 0)
        {
            Selection.objects = matchingSpells.ToArray(); // auto-select in Project
        }
        else
        {
            Debug.Log("No StateEffect assets found with any of these fields");
        }
    }
}
