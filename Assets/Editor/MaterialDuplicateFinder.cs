#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialDuplicateAnalyzer : EditorWindow
{
    private Vector2 scroll;
    private Dictionary<string, List<string>> duplicates = new();

    [MenuItem("Tools/Analyze/Find Truly Identical Materials")]
    public static void ShowWindow()
    {
        GetWindow<MaterialDuplicateAnalyzer>("Material Deduplicator").ScanMaterials();
    }

    private void ScanMaterials()
    {
        duplicates.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Material");

        Dictionary<string, List<string>> keyToPaths = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null)
                continue;

            string shaderName = mat.shader.name;
            string textureName = mat.mainTexture != null ? mat.mainTexture.name : "NULL";

            int srcBlend = mat.GetInt("_SrcBlend");
            int dstBlend = mat.GetInt("_DstBlend");

            string key = $"{shaderName}|{textureName}|Src:{srcBlend}|Dst:{dstBlend}";

            if (!keyToPaths.ContainsKey(key))
                keyToPaths[key] = new List<string>();

            keyToPaths[key].Add(path);
        }

        foreach (var pair in keyToPaths)
        {
            if (pair.Value.Count > 1)
                duplicates[pair.Key] = pair.Value;
        }
    }

    private void OnGUI()
    {
        if (duplicates.Count == 0)
        {
            if (GUILayout.Button("Scanner les matériaux"))
                ScanMaterials();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll);

        foreach (var group in duplicates)
        {
            EditorGUILayout.SelectableLabel($"Groupe: {group.Key} ({group.Value.Count})", EditorStyles.boldLabel);
            foreach (var path in group.Value)
                EditorGUILayout.SelectableLabel(path);
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();
    }
}

#endif