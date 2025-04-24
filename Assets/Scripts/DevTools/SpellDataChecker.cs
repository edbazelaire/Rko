using UnityEngine;
using UnityEditor;
using System.Linq;
using Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Game.Spells;

public class SpellDataChecker : EditorWindow
{
    [MenuItem("Tools/Fix Spell Descriptions")]
    public static void FixSpellDescriptions()
    {
        string[] guids = AssetDatabase.FindAssets("t:SpellData");
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpellData spellData = AssetDatabase.LoadAssetAtPath<SpellData>(path);
            bool wasModified = false;

            if (spellData == null)
                continue;

            Undo.RecordObject(spellData, "Fix Spell Description and Variables");

            // 1) Fix Description Text
            if (spellData.BaseDescription != null && spellData.BaseDescription.Contains("damages"))
            {
                spellData.SetBaseDescription(spellData.BaseDescription.Replace("damages", "damage"));
                wasModified = true;
            }

            // 2) Fix DescriptionVariables Names
            var descriptionVars = spellData.DescriptionVariables;
            for (int i = 0; i < descriptionVars.Count; i++)
            {
                if (descriptionVars[i].Name.Contains("Damages"))
                {
                    descriptionVars[i] = new SDescriptionVariable(
                        descriptionVars[i].Name.Replace("Damages", "Damage"),
                        descriptionVars[i].WithIcon
                    );
                    wasModified = true;
                }
            }

            if (wasModified)
            {
                spellData.SetDescriptionVariables(descriptionVars);
                EditorUtility.SetDirty(spellData);
                Debug.Log($"[Fixed] {spellData.name} at {path}", spellData);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Auto-correction complete. {fixedCount} spell(s) updated.");
    }

    [MenuItem("Tools/Fix Rune Descriptions")]
    public static void FixRuneDescriptions()
    {
        string[] guids = AssetDatabase.FindAssets("t:RuneData");
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RuneData runeData = AssetDatabase.LoadAssetAtPath<RuneData>(path);
            bool wasModified = false;

            if (runeData == null)
                continue;

            Undo.RecordObject(runeData, "Fix Rune Description and Variables");

            // 1) Fix RuneData.Description
            if (!string.IsNullOrEmpty(runeData.Description) && runeData.Description.Contains("damages"))
            {
                runeData.Description = runeData.Description.Replace("damages", "damage");
                wasModified = true;
            }

            // 2) Fix DescriptionVariables in each SRunePower (Minor, Major, Primal)
            wasModified |= FixRunePowerVariables(runeData, ref runeData.m_MinorPower);
            wasModified |= FixRunePowerVariables(runeData, ref runeData.m_MajorPower);
            wasModified |= FixRunePowerVariables(runeData, ref runeData.m_PrimalPower);

            if (wasModified)
            {
                EditorUtility.SetDirty(runeData);
                Debug.Log($"[Fixed] {runeData.name} at {path}", runeData);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Rune descriptions auto-correction complete. {fixedCount} rune(s) updated.");
    }

    private static bool FixRunePowerVariables(RuneData runeData, ref SRunePower runePower)
    {
        bool modified = false;

        // Fix Description text inside SRunePower
        if (!string.IsNullOrEmpty(runePower.Description) && runePower.Description.Contains("damages"))
        {
            runePower.Description = runePower.Description.Replace("damages", "damage");
            modified = true;
        }

        var descriptionVars = runePower.DescriptionVariables;
        for (int i = 0; i < descriptionVars.Count; i++)
        {
            if (descriptionVars[i].Name.Contains("Damages"))
            {
                descriptionVars[i] = new SDescriptionVariable(
                    descriptionVars[i].Name.Replace("Damages", "Damage"),
                    descriptionVars[i].WithIcon
                );
                modified = true;
            }
        }

        if (modified)
        {
            runePower.SetDescriptionVariables(descriptionVars);
        }

        return modified;
    }

    // Map old serialized names → new field names
    static readonly Dictionary<string, string> s_StateEffectFieldRenames = new()
    {
        { "m_Damages",          "m_Damage" },
        { "m_TickDamages",      "m_TickDamage" },
        { "m_EndDamages",       "m_EndDamage" },
        { "m_IsTrueDamages",    "m_IsTrueDamage" },
        
        { "m_BonusDamages",         "m_BonusDamage" },
        { "m_BonusDamagesPerc",     "m_IsTrueDamage" },
        { "m_BonusTickDamages",     "m_BonusTickDamage" },
        { "m_BonusTickDamagesPerc", "m_BonusTickDamagePerc" },
        { "m_BonusBurnDamages",     "m_BonusBurnDamage" },
    };

    [MenuItem("Tools/Preview StateEffect Field Recovery")]
    public static void PreviewStateEffectRecovery()
    {
        string oldAssetsFolder = EditorUtility.OpenFolderPanel("Select Folder with OLD StateEffect assets", "", "");
        if (string.IsNullOrEmpty(oldAssetsFolder)) return;

        string[] guids = AssetDatabase.FindAssets("t:StateEffect");
        int totalDetected = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StateEffect effectData = AssetDatabase.LoadAssetAtPath<StateEffect>(path);
            if (effectData == null) continue;

            string fileName = Path.GetFileName(path);
            string[] matchingOldFiles = Directory.GetFiles(oldAssetsFolder, fileName, SearchOption.AllDirectories);
            if (matchingOldFiles.Length == 0)
            {
                //Debug.LogWarning($"[Not Found] Old asset not found for {fileName}");
                continue;
            }

            string oldText = File.ReadAllText(matchingOldFiles[0]);
            SerializedObject serializedObject = new SerializedObject(effectData);

            foreach (var renamePair in s_StateEffectFieldRenames)
            {
                string oldField = renamePair.Key;
                string newField = renamePair.Value;

                var match = Regex.Match(oldText, $"{oldField}:\\s*(\\d+)");
                if (!match.Success) continue;

                int parsedValue = int.Parse(match.Groups[1].Value);
                SerializedProperty prop = serializedObject.FindProperty(newField);

                if (prop == null)
                {
                    Debug.LogWarning($"[Missing Field] Field '{newField}' not found on {fileName}");
                    continue;
                }

                string currentValueStr = prop.propertyType switch
                {
                    SerializedPropertyType.Boolean => prop.boolValue.ToString(),
                    SerializedPropertyType.Integer => prop.intValue.ToString(),
                    _ => "UnknownType"
                };

                string recoveredValueStr = prop.propertyType == SerializedPropertyType.Boolean ? (parsedValue != 0).ToString() : parsedValue.ToString();

                if (currentValueStr == recoveredValueStr)
                    continue;

                // Apply the recovered value
                if (prop.propertyType == SerializedPropertyType.Boolean)
                    prop.boolValue = parsedValue != 0;
                else if (prop.propertyType == SerializedPropertyType.Integer)
                    prop.intValue = parsedValue;

                // Log the change
                Debug.Log($"[Applied] {fileName} → {oldField} → {newField} set to {recoveredValueStr}", effectData);
                totalDetected++;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(effectData);
        }

        Debug.Log($"🔎 Recovery preview finished. {totalDetected} potential field updates detected.");
        AssetDatabase.SaveAssets();
    }
}
