using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using System.Threading.Tasks;
using Managers;

public class PlayerSearchTool : EditorWindow
{
    private string gamerTag = "";
    private bool searching = false;
    private List<EntityData> searchResults = new List<EntityData>();

    [MenuItem("Tools/Player Search Tool")]
    public static void Open()
    {
        var window = GetWindow<PlayerSearchTool>("Player Search Tool");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("Search Player by GamerTag (Cloud Save Public Data)", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        gamerTag = EditorGUILayout.TextField("GamerTag:", gamerTag);

        GUI.enabled = !string.IsNullOrWhiteSpace(gamerTag) && !searching;
        if (GUILayout.Button("Search"))
        {
            _ = StartSearch();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(20);

        DrawResults();
    }

    private async Task StartSearch()
    {
        searching = true;
        searchResults.Clear();

        try
        {
            await EnsureUnityServices();

            var query = new Query(
                new List<FieldFilter>
                {
                    new FieldFilter("GamerTag", gamerTag, FieldFilter.OpOptions.EQ, true)
                },
                new HashSet<string> { "GamerTag", "Level", "Avatar" }
            );

            var results = await CloudSaveService.Instance.Data.Player.QueryAsync(query, new QueryOptions());

            searchResults = results;

            Debug.Log($"PlayerSearchTool: Found {results.Count} result(s).");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("PlayerSearchTool Error: " + ex);
        }

        searching = false;
        Repaint();
    }

    private void DrawResults()
    {
        if (searchResults.Count == 0)
        {
            GUILayout.Label("No results.");
            return;
        }

        GUILayout.Label("Results:", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        foreach (var player in searchResults)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Player ID:", player.Id);

            foreach (var kv in player.Data)
                EditorGUILayout.LabelField($"{kv.Key}:", kv.Value.GetAsString());

            EditorGUILayout.Space();

            if (GUILayout.Button("Copy Player ID"))
                EditorGUIUtility.systemCopyBuffer = player.Id;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }


    // Ensures Cloud Save is initialized in the Editor
    private static async Task EnsureUnityServices()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return;

        var options = new InitializationOptions()
            .SetProfile("EditorTools");

        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
