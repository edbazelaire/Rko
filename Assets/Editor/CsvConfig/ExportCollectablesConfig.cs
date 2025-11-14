using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Data.GameManagement; // <- ton namespace

public static class ExportCollectablesConfig
{
    // ===== MENU =====
    [MenuItem("Tools/Collectables/Copy All to Clipboard (TSV)")]
    public static void CopyAllToClipboard()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        var sb = new StringBuilder();

        // NOTE: L’utilisateur veut 3 colonnes : index, RequiredQty, RequiredGold.
        // Pour AccountLevelData il n’y a pas Qty/Gold → on met RequiredXp dans la colonne "RequiredQty" et 0 en "RequiredGold".
        sb.AppendLine(BuildSection("AccountLevelData", data.AccountLevelData.Count,
            i => GetSafe(data.AccountLevelData, i).RequiredXp,     // mapped to Qty
            i => 0                                                // Gold = 0
        ));

        sb.AppendLine(BuildSection("CharacterLevelData", data.CharacterLevelData.Count,
            i => GetSafe(data.CharacterLevelData, i).RequiredQty,
            i => GetSafe(data.CharacterLevelData, i).RequiredGold
        ));

        sb.AppendLine(BuildSection("SpellLevelData", data.SpellLevelData.Count,
            i => GetSafe(data.SpellLevelData, i).RequiredQty,
            i => GetSafe(data.SpellLevelData, i).RequiredGold
        ));

        sb.AppendLine(BuildSection("RuneLevelData", data.RuneLevelData.Count,
            i => GetSafe(data.RuneLevelData, i).RequiredQty,
            i => GetSafe(data.RuneLevelData, i).RequiredGold
        ));

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("✅ Export Collectables: tout a été copié dans le presse-papiers (TSV). Ouvre Excel et colle.");
    }

    [MenuItem("Tools/Collectables/Copy AccountLevelData to Clipboard (TSV)")]
    public static void CopyAccountToClipboard()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        var tsv = BuildSection("AccountLevelData", data.AccountLevelData.Count,
            i => GetSafe(data.AccountLevelData, i).RequiredXp,
            i => 0);
        EditorGUIUtility.systemCopyBuffer = tsv;
        Debug.Log("✅ AccountLevelData copié dans le presse-papiers (TSV).");
    }

    [MenuItem("Tools/Collectables/Copy CharacterLevelData to Clipboard (TSV)")]
    public static void CopyCharacterToClipboard()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        var tsv = BuildSection("CharacterLevelData", data.CharacterLevelData.Count,
            i => GetSafe(data.CharacterLevelData, i).RequiredQty,
            i => GetSafe(data.CharacterLevelData, i).RequiredGold);
        EditorGUIUtility.systemCopyBuffer = tsv;
        Debug.Log("✅ CharacterLevelData copié (TSV).");
    }

    [MenuItem("Tools/Collectables/Copy SpellLevelData to Clipboard (TSV)")]
    public static void CopySpellToClipboard()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        var tsv = BuildSection("SpellLevelData", data.SpellLevelData.Count,
            i => GetSafe(data.SpellLevelData, i).RequiredQty,
            i => GetSafe(data.SpellLevelData, i).RequiredGold);
        EditorGUIUtility.systemCopyBuffer = tsv;
        Debug.Log("✅ SpellLevelData copié (TSV).");
    }

    [MenuItem("Tools/Collectables/Copy RuneLevelData to Clipboard (TSV)")]
    public static void CopyRuneToClipboard()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        var tsv = BuildSection("RuneLevelData", data.RuneLevelData.Count,
            i => GetSafe(data.RuneLevelData, i).RequiredQty,
            i => GetSafe(data.RuneLevelData, i).RequiredGold);
        EditorGUIUtility.systemCopyBuffer = tsv;
        Debug.Log("✅ RuneLevelData copié (TSV).");
    }

    [MenuItem("Tools/Collectables/Save All as CSV...")]
    public static void SaveAllAsCsv()
    {
        var data = LoadDataAsset();
        if (data == null) return;

        var dir = EditorUtility.SaveFolderPanel("Choisir un dossier pour les CSV", Application.dataPath, "Exports");
        if (string.IsNullOrEmpty(dir)) return;

        SaveCsv(Path.Combine(dir, "AccountLevelData.csv"),
            "index,RequiredQty,RequiredGold",
            data.AccountLevelData.Count,
            i => GetSafe(data.AccountLevelData, i).RequiredXp, // Qty = Xp
            i => 0);

        SaveCsv(Path.Combine(dir, "CharacterLevelData.csv"),
            "index,RequiredQty,RequiredGold",
            data.CharacterLevelData.Count,
            i => GetSafe(data.CharacterLevelData, i).RequiredQty,
            i => GetSafe(data.CharacterLevelData, i).RequiredGold);

        SaveCsv(Path.Combine(dir, "SpellLevelData.csv"),
            "index,RequiredQty,RequiredGold",
            data.SpellLevelData.Count,
            i => GetSafe(data.SpellLevelData, i).RequiredQty,
            i => GetSafe(data.SpellLevelData, i).RequiredGold);

        SaveCsv(Path.Combine(dir, "RuneLevelData.csv"),
            "index,RequiredQty,RequiredGold",
            data.RuneLevelData.Count,
            i => GetSafe(data.RuneLevelData, i).RequiredQty,
            i => GetSafe(data.RuneLevelData, i).RequiredGold);

        EditorUtility.RevealInFinder(dir);
        Debug.Log("✅ CSV sauvegardés.");
    }

    // ===== CORE =====
    private static CollectablesManagementData LoadDataAsset()
    {
        // 1) Essaye via l’emplacement indiqué (dossier “Ressources” → Unity attend “Resources”)
        // Si ton projet utilise bien "Assets/Resources/Data/GameManagement/CollectablesManagementData/CollectablesManagementData.asset",
        // Resources.Load fonctionnera. Sinon, fallback via AssetDatabase.FindAssets.
        CollectablesManagementData data = null;

        // Tentative Resources (orthographe anglaise)
        data = Resources.Load<CollectablesManagementData>("Data/GameManagement/CollectablesManagementData/CollectablesManagementData");
        if (data != null) return data;

        // Fallback: cherche l’asset dans le projet (indépendant du chemin)
        var guids = AssetDatabase.FindAssets($"t:{nameof(CollectablesManagementData)}");
        if (guids != null && guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            data = AssetDatabase.LoadAssetAtPath<CollectablesManagementData>(path);
        }

        if (data == null)
        {
            Debug.LogError("❌ Impossible de charger CollectablesManagementData. Vérifie que l’asset existe (Resources ou n’importe où dans le projet).");
        }
        return data;
    }

    // Construit une section TSV : entête + lignes
    private static string BuildSection(string title, int count, System.Func<int, int> qtyGetter, System.Func<int, int> goldGetter)
    {
        var sb = new StringBuilder();
        //sb.AppendLine($"# {title}");
        //sb.AppendLine("index\tRequiredQty\tRequiredGold");
        for (int i = 0; i < count; i++)
        {
            //var idx = i + 1; // index humain 1..N
            var qty = SafeEval(() => qtyGetter(i), 0);
            var gold = SafeEval(() => goldGetter(i), 0);
            //sb.AppendLine($"{idx}\t{qty}\t{gold}");
            sb.AppendLine($"{qty}\t{gold}");
        }
        return sb.ToString();
    }

    // Construit une section TSV : entête + lignes
    private static string BuildSectionFull(string title, int count, System.Func<int, int> qtyGetter, System.Func<int, int> goldGetter)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine("index\tRequiredQty\tRequiredGold");
        for (int i = 0; i < count; i++)
        {
            var idx = i + 1; // index humain 1..N
            var qty = SafeEval(() => qtyGetter(i), 0);
            var gold = SafeEval(() => goldGetter(i), 0);
            sb.AppendLine($"{idx}\t{qty}\t{gold}");
        }
        return sb.ToString();
    }

    private static void SaveCsv(string path, string header, int count, System.Func<int, int> qtyGetter, System.Func<int, int> goldGetter)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        for (int i = 0; i < count; i++)
        {
            var idx = i + 1;
            var qty = SafeEval(() => qtyGetter(i), 0);
            var gold = SafeEval(() => goldGetter(i), 0);
            sb.AppendLine($"{idx},{qty},{gold}");
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
    }

    // Helpers robustesse
    private static T GetSafe<T>(System.Collections.Generic.IList<T> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
            Debug.LogError($"Index hors limites ({index}).");
            return default;
        }
        return list[index];
    }

    private static T SafeEval<T>(System.Func<T> getter, T fallback)
    {
        try { return getter(); }
        catch { return fallback; }
    }
}
