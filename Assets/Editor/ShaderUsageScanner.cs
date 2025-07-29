#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShaderUsageScanner : EditorWindow
{
    private Vector2 scrollPos;
    private Dictionary<Shader, int> shaderUsageCount = new Dictionary<Shader, int>();

    [MenuItem("Tools/Analyze/Scan Shader Usage in Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<ShaderUsageScanner>("Shader Usage Scanner").ScanShaders();
    }

    private void ScanShaders()
    {
        shaderUsageCount.Clear();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader != null)
                    {
                        if (!shaderUsageCount.ContainsKey(mat.shader))
                            shaderUsageCount[mat.shader] = 0;

                        shaderUsageCount[mat.shader]++;
                    }
                }
            }

            ParticleSystemRenderer[] particles = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var ps in particles)
            {
                var mat = ps.sharedMaterial;
                if (mat != null && mat.shader != null)
                {
                    if (!shaderUsageCount.ContainsKey(mat.shader))
                        shaderUsageCount[mat.shader] = 0;

                    shaderUsageCount[mat.shader]++;
                }
            }
        }
    }

    private void OnGUI()
    {
        if (shaderUsageCount.Count == 0)
        {
            if (GUILayout.Button("Scan Shaders"))
            {
                ScanShaders();
            }
        }
        else
        {
            GUILayout.Label("Shader Usage in Prefabs:", EditorStyles.boldLabel);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var pair in shaderUsageCount)
            {
                GUILayout.Label($"{pair.Key.name}: {pair.Value} uses");
            }
            GUILayout.EndScrollView();
        }
    }
}
#endif
