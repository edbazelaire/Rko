#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialReplacer : EditorWindow
{
    [MenuItem("Tools/Optimize/Replace Materials in Prefabs")]
    static void ReplaceMaterials()
    {
        string masterMatPath = "Assets/Resources/Visuals/Materials/SharedFX/";
        Dictionary<string, Material> masterMaterials = new();

        // Load master materials
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { masterMatPath });
        foreach (var guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.mainTexture != null)
                masterMaterials[mat.mainTexture.name] = mat;
        }

        // Find all prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            bool dirty = false;
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat != null && mat.mainTexture != null)
                    {
                        string texName = mat.mainTexture.name;
                        if (masterMaterials.ContainsKey(texName))
                        {
                            materials[i] = masterMaterials[texName];
                            dirty = true;
                        }
                    }
                }
                renderer.sharedMaterials = materials;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(prefab);
                Debug.Log($"✔ Replaced materials in {prefab.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Material replacement complete.");
    }
}
#endif
