// Assets/Editor/ImportCollectablesConfigWindow.cs
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Data.GameManagement;

public class ImportCollectablesConfigWindow : EditorWindow
{
    public enum TargetList { AccountLevel, CharacterLevel, SpellLevel, RuneLevel }
    private TargetList target;
    private List<SLevelData> newLevelData = new();
    private List<SLevelData> currentLevelData = new();
    private Vector2 scroll;

    public static void Open(TargetList target)
    {
        var window = CreateInstance<ImportCollectablesConfigWindow>();
        window.titleContent = new GUIContent("Importer Config");
        window.target = target;
        window.minSize = new Vector2(600, 400);
        window.ParseClipboard();
        window.ShowUtility();
    }

    [MenuItem("Tools/Collectables/Paste into ▸ AccountLevelData")]
    private static void PasteAccount() => Open(TargetList.AccountLevel);

    [MenuItem("Tools/Collectables/Paste into ▸ CharacterLevelData")]
    private static void PasteCharacter() => Open(TargetList.CharacterLevel);

    [MenuItem("Tools/Collectables/Paste into ▸ SpellLevelData")]
    private static void PasteSpell() => Open(TargetList.SpellLevel);

    [MenuItem("Tools/Collectables/Paste into ▸ RuneLevelData")]
    private static void PasteRune() => Open(TargetList.RuneLevel);

    private void OnGUI()
    {
        EditorGUILayout.LabelField($"Importer dans {target}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (newLevelData.Count == 0)
        {
            EditorGUILayout.HelpBox("Presse-papiers vide ou format invalide.\nColle deux colonnes (Qty,Gold) séparées par tabulation ou virgule.\n" +
                                    "Pour AccountLevelData : une seule colonne (XP).", MessageType.Warning);
            if (GUILayout.Button("Recharger le presse-papiers"))
                ParseClipboard();
            return;
        }

        EditorGUILayout.LabelField("Aperçu :", EditorStyles.miniBoldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        int maxRows = Mathf.Max(currentLevelData.Count, newLevelData.Count);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Actuel (Qty | Gold)", EditorStyles.boldLabel);
        GUILayout.Label("Nouveau (Qty | Gold)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < maxRows; i++)
        {
            EditorGUILayout.BeginHorizontal("box");

            string cur = i < currentLevelData.Count ? $"{currentLevelData[i].RequiredQty} | {currentLevelData[i].RequiredGold}" : "-";
            string nw = i < newLevelData.Count ? $"{newLevelData[i].RequiredQty} | {newLevelData[i].RequiredGold}" : "-";

            // Mets en évidence si différent
            GUI.color = (cur == nw) ? Color.gray : Color.white;

            GUILayout.Label(cur, GUILayout.Width(250));
            GUILayout.Label("→", GUILayout.Width(20));
            GUILayout.Label(nw, GUILayout.Width(250));

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Annuler"))
            Close();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Confirmer", GUILayout.Height(30)))
        {
            ApplyChanges();
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ParseClipboard()
    {
        newLevelData.Clear();
        currentLevelData.Clear();

        var data = LoadDataAsset();
        if (data == null) return;

        // Charger liste actuelle
        switch (target)
        {
            case TargetList.AccountLevel:
                foreach (var entry in data.AccountLevelData)
                    currentLevelData.Add(new SLevelData(0, entry.RequiredXp));
                break;
            case TargetList.CharacterLevel:
                currentLevelData = new List<SLevelData>(data.CharacterLevelData);
                break;
            case TargetList.SpellLevel:
                currentLevelData = new List<SLevelData>(data.SpellLevelData);
                break;
            case TargetList.RuneLevel:
                currentLevelData = new List<SLevelData>(data.RuneLevelData);
                break;
        }

        // Parser presse-papiers
        var lines = EditorGUIUtility.systemCopyBuffer.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('\t', ',', ';');
            if (parts.Length == 0) continue;

            if (target == TargetList.AccountLevel)
            {
                if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int xp))
                    newLevelData.Add(new SLevelData(0, xp));
            }
            else if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) &&
                    int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gold))
                {
                    newLevelData.Add(new SLevelData(gold, qty));
                }
            }
        }
    }

    private void ApplyChanges()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        Undo.RecordObject(data, "Import Collectables Data");

        switch (target)
        {
            case TargetList.AccountLevel:
                data.AccountLevelData.Clear();
                foreach (var lvl in newLevelData)
                    data.AccountLevelData.Add(new SAccountLevelData(lvl.RequiredQty, default));
                break;

            case TargetList.CharacterLevel:
                data.CharacterLevelData = new List<SLevelData>(newLevelData);
                break;

            case TargetList.SpellLevel:
                data.SpellLevelData = new List<SLevelData>(newLevelData);
                break;

            case TargetList.RuneLevel:
                data.RuneLevelData = new List<SLevelData>(newLevelData);
                break;
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ {target} mis à jour ({newLevelData.Count} entrées).");
    }

    private static CollectablesManagementData LoadDataAsset()
    {
        var data = Resources.Load<CollectablesManagementData>(
            "Data/GameManagement/CollectablesManagementData/CollectablesManagementData");

        if (data != null) return data;

        var guids = AssetDatabase.FindAssets($"t:{nameof(CollectablesManagementData)}");
        if (guids != null && guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            data = AssetDatabase.LoadAssetAtPath<CollectablesManagementData>(path);
        }

        if (data == null)
            Debug.LogError("❌ Impossible de charger CollectablesManagementData. Vérifie le chemin ou le nom de l’asset.");

        return data;
    }
}
