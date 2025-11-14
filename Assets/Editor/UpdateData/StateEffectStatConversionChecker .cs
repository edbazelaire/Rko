using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Enums;
using Game.Spells;

/// <summary>
/// Editor tool that scans all StateEffect assets and lists their StatConversions.
/// It highlights those that have invalid conversions (ToStat.StateEffectProperty == None).
/// </summary>
public class StateEffectStatConversionChecker : EditorWindow
{
    private Vector2 _scroll;
    private List<StateEffect> _okEffects = new List<StateEffect>();
    private List<StateEffect> _invalidEffects = new List<StateEffect>();
    private bool _includeEmpty = false;

    [MenuItem("Tools/UpdateData/Check StatConversions")]
    public static void ShowWindow()
    {
        var window = GetWindow<StateEffectStatConversionChecker>("StateEffect Checker");
        window.minSize = new Vector2(600, 400);
        window.Scan();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🧩 StateEffect StatConversion Checker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans all StateEffect assets and separates them into valid and invalid groups.\n" +
            "An effect is considered INVALID if any of its StatConversions has ToStat.StateEffectProperty == None.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("🔁 Rescan All StateEffects", GUILayout.Height(28)))
        {
            Scan();
        }

        _includeEmpty = EditorGUILayout.ToggleLeft("Include empty StatConversions", _includeEmpty);

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // ⚠️ Group 1 — INVALID ones
        DrawGroup("❌ INVALID StatConversions", _invalidEffects, new Color(1f, 0.8f, 0.8f));

        EditorGUILayout.Space(15);

        // ✅ Group 2 — OK ones
        DrawGroup("✅ OK StatConversions", _okEffects, new Color(0.85f, 1f, 0.85f));

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draws a group of StateEffects in a colored section.
    /// </summary>
    private void DrawGroup(string title, List<StateEffect> effects, Color background)
    {
        if (effects == null || effects.Count == 0)
        {
            EditorGUILayout.HelpBox($"No {title.ToLower()}.", MessageType.None);
            return;
        }

        var bg = new GUIStyle(GUI.skin.box);
        bg.normal.background = Texture2D.whiteTexture;
        var oldColor = GUI.color;

        GUI.color = background;
        EditorGUILayout.BeginVertical(bg);
        GUI.color = oldColor;

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        foreach (var effect in effects)
        {
            if (effect == null) continue;

            var conversions = effect.StatConversions;
            int count = conversions != null ? conversions.Count : 0;

            if (!_includeEmpty && (conversions == null || count == 0))
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(effect.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                Selection.activeObject = effect;
                EditorGUIUtility.PingObject(effect);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Path: {AssetDatabase.GetAssetPath(effect)}");

            if (count == 0)
            {
                EditorGUILayout.LabelField("StatConversions: (empty)");
            }
            else
            {
                foreach (var conv in conversions)
                {
                    string status = conv.ToStat.StateEffectProperty == EStateEffectProperty.None
                        ? "❌ None"
                        : "✅ " + conv.ToStat.StateEffectProperty.ToString();

                    EditorGUILayout.LabelField($" - From: {conv.OriginalStatScaling.StateEffectProperty} → To: {status}");
                }
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Scans all StateEffect assets in the project and sorts them into two lists:
    /// Invalid (any ToStat.StateEffectProperty == None) and OK (all conversions valid).
    /// </summary>
    private void Scan()
    {
        _okEffects.Clear();
        _invalidEffects.Clear();

        string[] guids = AssetDatabase.FindAssets("t:StateEffect");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<StateEffect>(path);
            if (asset == null)
                continue;

            var list = asset.StatConversions;
            if (list == null || list.Count == 0)
            {
                if (_includeEmpty)
                    _okEffects.Add(asset);
                continue;
            }

            bool hasInvalid = list.Any(c => c.ToStat.StateEffectProperty == EStateEffectProperty.None);
            if (hasInvalid)
                _invalidEffects.Add(asset);
            else
                _okEffects.Add(asset);
        }

        // Sort for readability
        _invalidEffects = _invalidEffects.OrderBy(a => a.name).ToList();
        _okEffects = _okEffects.OrderBy(a => a.name).ToList();

        Repaint();
    }
}
