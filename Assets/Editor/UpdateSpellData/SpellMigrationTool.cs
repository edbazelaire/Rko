using Data.DataStructures.SpellSubStructures;
using Data;
using Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool used to migrate old SpellData assets from the legacy damage system
/// (m_Damage / m_ExecutionDamage + m_SpellsScalingLevel)
/// into the new SDamage-based system (List<SDamage> m_Damages).
/// 
/// It will:
/// - Skip spells that already have entries in m_Damages.
/// - Create new SDamage entries based on old damage values and scaling.
/// - Preserve the old data fields (no deletion).
/// 
/// Optional: limit migration to 'N' spells for testing.
/// </summary>
public class SpellMigrationTool : EditorWindow
{
    private int _maxToProcess = 10;
    private bool _limitEnabled = true;
    private string _spellsFolder = "Assets/GameData/Spells"; // 🧩 change this to your real folder

    [MenuItem("Tools/Spells/Migrate Damages to SDamage System")]
    public static void ShowWindow()
    {
        GetWindow<SpellMigrationTool>("Spell Migration Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Spell Damage Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool will migrate old SpellData assets to the new SDamage format.", MessageType.Info);

        _spellsFolder = EditorGUILayout.TextField("Spells Folder:", _spellsFolder);
        _limitEnabled = EditorGUILayout.Toggle("Limit Migration Count", _limitEnabled);
        if (_limitEnabled)
            _maxToProcess = EditorGUILayout.IntField("Max Spells to Process", _maxToProcess);

        if (GUILayout.Button("Run Migration"))
        {
            RunMigration();
        }
    }

    /// <summary>
    /// Executes the migration process over all SpellData assets found in the specified folder.
    /// </summary>
    private void RunMigration()
    {
        string[] spellGuids = AssetDatabase.FindAssets("t:SpellData", new[] { _spellsFolder });
        int processed = 0;

        Debug.Log($"[SpellMigration] Found {spellGuids.Length} SpellData assets to check.");

        foreach (string guid in spellGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpellData spell = AssetDatabase.LoadAssetAtPath<SpellData>(path);

            if (spell == null)
            {
                Debug.LogWarning($"[SpellMigration] Could not load asset at {path}");
                continue;
            }

            if (spell.Damages != null && spell.Damages.Count > 0)
            {
                Debug.Log($"[SpellMigration] Skipping '{spell.name}' — already migrated.");
                continue;
            }

            bool modified = false;
            List<SDamage> newDamages = new();

            // 🧩 Get scaling for normal and execution damages
            var scalingDamage = spell.SpellsScalingLevel?.FirstOrDefault(s => s.Property == ESpellProperty.Damage);
            var scalingExec = spell.SpellsScalingLevel?.FirstOrDefault(s => s.Property == ESpellProperty.ExecutionDamage);

            // --- Determine damage category based on SpellType
            EDamageCategory damageCategory = GetDamageCategoryForSpell(spell.SpellType);

            // --- Normal Damage
            if (spell.m_Damage != 0)
            {
                SDamage dmg = new SDamage();
                SetPrivateField(dmg, "m_BaseValue", (float)spell.m_Damage);
                SetPrivateField(dmg, "m_DamageCategory", damageCategory);
                SetPrivateField(dmg, "m_HitCategory", EHitCategory.Direct);
                SetPrivateField(dmg, "m_SpellPropertyScaling", scalingDamage != null ? scalingDamage.Value : new SSpellPropertyScaling(ESpellProperty.Damage, 0f));

                newDamages.Add(dmg);
                modified = true;
                Debug.Log($"[SpellMigration] Added base damage for '{spell.name}' → {spell.m_Damage} ({damageCategory})");
            }

            // --- Execution Damage
            if (spell.m_ExecutionDamage != 0)
            {
                SDamage dmg = new SDamage();
                SetPrivateField(dmg, "m_BaseValue", (float)spell.m_ExecutionDamage);
                SetPrivateField(dmg, "m_DamageCategory", damageCategory);
                SetPrivateField(dmg, "m_HitCategory", EHitCategory.Execution);
                SetPrivateField(dmg, "m_SpellPropertyScaling", scalingExec != null ? scalingExec.Value : new SSpellPropertyScaling(ESpellProperty.ExecutionDamage, 0f));

                newDamages.Add(dmg);
                modified = true;
                Debug.Log($"[SpellMigration] Added execution damage for '{spell.name}' → {spell.m_ExecutionDamage} ({damageCategory})");
            }

            // --- If modified, assign new damages list
            if (modified)
            {
                Undo.RecordObject(spell, "Migrate SpellData Damages");
                SetPrivateField(spell, "m_Damages", newDamages);
                EditorUtility.SetDirty(spell);
                Debug.Log($"[SpellMigration] ✅ '{spell.name}' migrated successfully with {newDamages.Count} damage entries.");
            }

            processed++;
            if (_limitEnabled && processed >= _maxToProcess)
            {
                Debug.Log($"[SpellMigration] Migration stopped after {_maxToProcess} spells (limit enabled).");
                break;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SpellMigration] Migration complete. Processed {processed} spell(s).");
    }

    /// <summary>
    /// Determines whether the spell is Physical or Magical based on its SpellType.
    /// </summary>
    private static EDamageCategory GetDamageCategoryForSpell(ESpellType type)
    {
        switch (type)
        {
            case ESpellType.Projectile:
            case ESpellType.Jump:
            case ESpellType.MultiProjectiles:
                return EDamageCategory.Physical;

            default:
                return EDamageCategory.Magical;
        }
    }

    /// <summary>
    /// Helper to set protected/private serialized fields via reflection.
    /// This version searches recursively through the inheritance chain
    /// and handles protected fields correctly.
    /// </summary>
    /// <param name="obj">The object instance that owns the field.</param>
    /// <param name="fieldName">The internal or protected field name (e.g. "m_Damages").</param>
    /// <param name="value">The value to assign to that field.</param>
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        if (obj == null)
        {
            Debug.LogWarning("[SpellMigration] Tried to set a field on a null object.");
            return;
        }

        System.Type type = obj.GetType();
        System.Reflection.FieldInfo field = null;

        // 🔍 Walk up the inheritance hierarchy to find the field
        while (type != null)
        {
            field = type.GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public
            );

            if (field != null)
                break;

            type = type.BaseType;
        }

        if (field != null)
        {
            field.SetValue(obj, value);
            Debug.Log($"[SpellMigration] ✅ Successfully set '{fieldName}' on {obj.GetType().Name} (declared in {field.DeclaringType.Name})");
        }
        else
        {
            Debug.LogWarning($"[SpellMigration] ⚠️ Could not find field '{fieldName}' on {obj.GetType().Name} or its base types");
        }
    }

}
