#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialDuplicateFinder : EditorWindow
{
    private Vector2 scroll;
    private Dictionary<string, List<string>> duplicateMaterials = new();

    [MenuItem("Tools/Analyze/Find Duplicate Materials")]
    static void Init()
    {
        MaterialDuplicateFinder window = GetWindow<MaterialDuplicateFinder>("Material Duplicates");
        window.FindDuplicates();
    }

    void FindDuplicates()
    {
        duplicateMaterials.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Material");
        Dictionary<string, List<string>> hashToPaths = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string hash = mat.shader.name;

            if (mat.mainTexture != null)
                hash += "|" + mat.mainTexture.name;

            if (!hashToPaths.ContainsKey(hash))
                hashToPaths[hash] = new List<string>();

            hashToPaths[hash].Add(path);
        }

        foreach (var pair in hashToPaths)
        {
            if (pair.Value.Count > 1)
                duplicateMaterials[pair.Key] = pair.Value;
        }
    }

    void OnGUI()
    {
        if (duplicateMaterials.Count == 0)
        {
            GUILayout.Label("Aucune duplication détectée ou scan non lancé.");
            if (GUILayout.Button("Analyser"))
                FindDuplicates();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll);

        foreach (var pair in duplicateMaterials)
        {
            GUILayout.Label($"Shader + Texture: {pair.Key} ({pair.Value.Count})", EditorStyles.boldLabel);
            foreach (var path in pair.Value)
            {
                GUILayout.Label($"- {path}");
            }
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();
    }
}
#endif