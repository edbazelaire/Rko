#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Data;

/// <summary>
/// Editor window to search for any tag (e.g. "[ExecutionDamage]") inside all CollectableData descriptions.
/// Displays a clickable list of results to open the matching assets.
/// </summary>
public class DescriptionTagFinder : EditorWindow
{
    private string searchTag = string.Empty;
    private Vector2 scroll;
    private List<(string path, CollectableData data)> results = new();

    [MenuItem("Tools/UpdateData/Description Tag Finder")]
    public static void OpenWindow()
    {
        var window = GetWindow<DescriptionTagFinder>("Description Tag Finder");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        // --- Search field
        EditorGUILayout.LabelField("Search for a tag in CollectableData descriptions", EditorStyles.boldLabel);
        searchTag = EditorGUILayout.TextField("Tag to find", searchTag);

        EditorGUILayout.Space();

        // --- Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Search", GUILayout.Height(25)))
            SearchForTag();

        if (GUILayout.Button("Clear", GUILayout.Height(25)))
            results.Clear();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // --- Results section
        if (results.Count == 0)
        {
            EditorGUILayout.HelpBox("No results found yet.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Found {results.Count} matching assets:", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var (path, data) in results)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button(data.name, EditorStyles.linkLabel))
            {
                Selection.activeObject = data;
                EditorGUIUtility.PingObject(data);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(Path.GetFileName(path), EditorStyles.miniLabel, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Scans all CollectableData assets and collects those containing the given tag.
    /// </summary>
    private void SearchForTag()
    {
        results.Clear();

        if (string.IsNullOrWhiteSpace(searchTag))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a tag to search (e.g. [Damage] or [ExecutionDamage])", "OK");
            return;
        }

        // Find all CollectableData assets in the project
        string[] guids = AssetDatabase.FindAssets("t:CollectableData");
        int total = guids.Length;

        for (int i = 0; i < total; i++)
        {
            string guid = guids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<CollectableData>(path);
            if (data == null || string.IsNullOrEmpty(data.BaseDescription))
                continue;

            // Check if the tag is present (case-insensitive)
            if (data.BaseDescription.IndexOf(searchTag, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add((path, data));
            }
        }

        if (results.Count == 0)
            Debug.Log($"🔍 No occurrences of \"{searchTag}\" found.");
        else
            Debug.Log($"✅ Found {results.Count} assets containing \"{searchTag}\".");

        Repaint();
    }
}
#endif
