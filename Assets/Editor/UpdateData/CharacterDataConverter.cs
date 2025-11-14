using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Data;

public class DataTypeConverterWindow : EditorWindow
{
    private Type detectedBaseType;
    private string detectedFolder;
    private string[] availableScripts;
    private int selectedScriptIndex = -1;
    private MonoScript selectedScript;

    [MenuItem("Tools/UpdateData/Convert CollectableType")]
    public static void ShowWindow()
    {
        GetWindow<DataTypeConverterWindow>("Data Type Converter");
    }

    private void OnSelectionChange()
    {
        // Recharger la fenêtre dès qu'on change la sélection
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🧬 Data Type Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        var selectedObjects = Selection.objects;

        if (selectedObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("Sélectionne un ou plusieurs assets (RuneData, SpellData ou CharacterData).", MessageType.Info);
            return;
        }

        // Vérifier le type commun
        var types = selectedObjects
            .Select(o => o.GetType())
            .Distinct()
            .ToList();

        if (types.Count != 1)
        {
            EditorGUILayout.HelpBox("Erreur : La sélection contient plusieurs types de données différents.", MessageType.Error);
            return;
        }

        Type selectedType = types[0];

        // Déterminer la base (RuneData, SpellData ou CharacterData)
        detectedBaseType = GetDataBaseType(selectedType);

        if (detectedBaseType == null)
        {
            EditorGUILayout.HelpBox($"Type non reconnu : {selectedType.Name} n’hérite pas de RuneData, SpellData ou CharacterData.", MessageType.Warning);
            return;
        }

        // Déterminer le dossier cible selon la base
        detectedFolder = GetFolderForBaseType(detectedBaseType);

        EditorGUILayout.LabelField($"Type détecté : {selectedType.Name}");
        EditorGUILayout.LabelField($"Base : {detectedBaseType.Name}");
        EditorGUILayout.LabelField($"Dossier : {detectedFolder}");
        EditorGUILayout.Space();

        // Charger automatiquement les scripts disponibles
        if (availableScripts == null)
            LoadAvailableScripts();

        if (availableScripts == null || availableScripts.Length == 0)
        {
            EditorGUILayout.HelpBox($"Aucun script trouvé dans {detectedFolder}", MessageType.Warning);
            return;
        }

        selectedScriptIndex = EditorGUILayout.Popup("Nouveau type :", selectedScriptIndex, availableScripts);

        if (selectedScriptIndex >= 0)
        {
            selectedScript = AssetDatabase.LoadAssetAtPath<MonoScript>(availableScripts[selectedScriptIndex]);
        }

        EditorGUILayout.Space();

        GUI.enabled = selectedScript != null;
        if (GUILayout.Button("🚀 Convertir la sélection"))
        {
            ConvertSelection(selectedObjects, selectedScript);
        }
        GUI.enabled = true;
    }

    private Type GetDataBaseType(Type type)
    {
        // Recherche récursive dans la hiérarchie
        while (type != null)
        {
            if (type == typeof(RuneData) || type == typeof(SpellData) || type == typeof(CharacterData))
                return type;
            type = type.BaseType;
        }
        return null;
    }

    private string GetFolderForBaseType(Type baseType)
    {
        if (baseType == typeof(RuneData))
            return "Assets/Scripts/Data/Runes";
        if (baseType == typeof(SpellData))
            return "Assets/Scripts/Data/Spells";
        if (baseType == typeof(CharacterData))
            return "Assets/Scripts/Data/Characters";
        return "Assets";
    }

    private void LoadAvailableScripts()
    {
        var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { detectedFolder });
        availableScripts = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".cs"))
            .ToArray();
    }

    private void ConvertSelection(UnityEngine.Object[] objects, MonoScript newScript)
    {
        if (newScript == null)
        {
            EditorUtility.DisplayDialog("Erreur", "Aucun script cible sélectionné.", "OK");
            return;
        }

        foreach (var obj in objects)
        {
            var so = new SerializedObject(obj);
            var scriptProp = so.FindProperty("m_Script");

            if (scriptProp != null)
            {
                scriptProp.objectReferenceValue = newScript;
                so.ApplyModifiedProperties();
                Debug.Log($"✅ {obj.name} converti en {newScript.name}");
            }
            else
            {
                Debug.LogWarning($"❌ Impossible de trouver m_Script pour {obj.name}");
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Conversion terminée", "Les fichiers sélectionnés ont été convertis avec succès.", "OK");
    }
}
