#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Drakensland.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool that automatically binds <see cref="LocalizedText"/> components
/// to every TMP text in every game scene by matching the current text content
/// to the existing localization database (French fallback).
/// 
/// Run via <b>Drakensland > Bind All Localized Texts (All Scenes)</b>.
/// Safe to run multiple times — existing bindings are updated, not duplicated.
/// Disabled GameObjects are fully supported.
/// </summary>
public static class LocalizedTextBinder
{
    // ── Texts that are set dynamically at runtime and must NOT be bound ──────
    // Add any text that is purely numeric or set by code.
    private static readonly HashSet<string> DynamicPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "1", "100", "1000", "10000",       // counters / prices
        "new text",                              // Unity default placeholder
        "new textjyhnslbkvmcbkclmcbkmbvcbckvbnkv njlvcnjbnbvjl loremipsum dhfdbfvhbvkvbhdvbdchcdbhdvshdcjkhdvsjkdvshjkvncjcvnjkvkjvcnjkvhbjkbhjkxbfjkvbnvjkhvjkvhbkchvbklbhdjhfhdfj", // lorem ipsum
    };

    // ── GameObject names whose TMP text is always set by code ────────────────
    // The binder will skip any TMP_Text whose GameObject name matches one of these.
    private static readonly HashSet<string> DynamicGameObjectNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PriceLabel",          // shop item price — set to cost.ToString()
        "Price",               // alternate price label name
        "LabelQuantite",       // inventory quantity — set to quantity.ToString()
        "Number",              // generic numeric counter
        "LabelEtatEquipe",     // "Équipé" / "Non équipé" — set by SkinPurchaseButton
        "NomObjet",            // skin name — set dynamically by SkinEquipFocus
        "DescriptionObjet",    // skin description — set dynamically by SkinEquipFocus
        "LabelNom",            // item name in inventory focus
        "LabelDescription",    // item description in inventory focus
        "ConfirmationNameLabel",      // confirmation panel name — dynamic
        "ConfirmationDescriptionLabel", // confirmation panel desc — dynamic
        "TexteDescription",    // InventaireMenuUI dynamic description
        "ScoreText",           // runtime score
        "BestScoreText",       // runtime best score
        "RankText",            // leaderboard rank — dynamic
        "PlayerNameText",      // leaderboard player name — dynamic
        "ScoreValueText",      // leaderboard score value — dynamic
    };

    // ── Scenes to process ────────────────────────────────────────────────────
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Barrage.unity",
        "Assets/Scenes/BarrageTuto.unity",
        "Assets/Scenes/MapRoad.unity",
        "Assets/Scenes/MapTuto.unity",
        "Assets/Scenes/ResultScene.unity",
        "Assets/Scenes/Test.unity",
    };

    // ── Lookup: French text → localization key ───────────────────────────────
    // Built at runtime from all LocalizationData assets whose languageCode == "fr".
    private static Dictionary<string, string> _frenchToKey;

    // ── Entry point ──────────────────────────────────────────────────────────

    // ── Additional entries missing from the initial asset creation ────────────
    // Each tuple: (languageCode, key, value)
    private static readonly (string lang, string key, string value)[] MissingEntries =
    {
        // French
        ("fr", "menu_home",               "Accueil"),
        ("fr", "shop_section_bonus_items","Objets bonus"),
        ("fr", "shop_section_cosmetics",  "Cosmétiques"),
        ("fr", "shop_section_currency",   "Monnaies"),
        ("fr", "options_sound",           "Volume général"),
        ("fr", "options_music",           "Volume musical"),
        ("fr", "options_ambient",         "Volume ambiant"),
        // English
        ("en", "menu_home",               "Home"),
        ("en", "shop_section_bonus_items","Bonus Items"),
        ("en", "shop_section_cosmetics",  "Cosmetics"),
        ("en", "shop_section_currency",   "Currency"),
        ("en", "options_sound",           "Master Volume"),
        ("en", "options_music",           "Music Volume"),
        ("en", "options_ambient",         "Ambient Volume"),
    };

    [MenuItem("Drakensland/Bind All Localized Texts (All Scenes)")]
    public static void BindAllScenes()
    {
        PatchMissingEntries();
        BuildFrenchLookup();

        if (_frenchToKey.Count == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "No French LocalizationData found.\n" +
                "Make sure a LocalizationData asset with languageCode='fr' exists.",
                "OK");
            return;
        }

        string activeScenePath = SceneManager.GetActiveScene().path;
        bool   activeSceneDirty = SceneManager.GetActiveScene().isDirty;

        int totalBound    = 0;
        int totalSkipped  = 0;
        int totalUnmapped = 0;

        foreach (string scenePath in ScenePaths)
        {
            if (!System.IO.File.Exists(scenePath)) continue;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            int bound, skipped, unmapped;
            ProcessScene(scene, out bound, out skipped, out unmapped);

            totalBound    += bound;
            totalSkipped  += skipped;
            totalUnmapped += unmapped;

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LocalizedTextBinder] {scenePath} → bound={bound}, skipped={skipped}, unmapped={unmapped}");
        }

        // Restore the original scene if possible
        if (!string.IsNullOrEmpty(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        string summary =
            $"Binding complete across {ScenePaths.Length} scenes.\n\n" +
            $"  Bound   : {totalBound}\n" +
            $"  Skipped : {totalSkipped}  (dynamic / numeric)\n" +
            $"  Unmapped: {totalUnmapped}  (no matching key — check console)";

        Debug.Log($"[LocalizedTextBinder] {summary}");
        EditorUtility.DisplayDialog("Localization Binder", summary, "OK");
    }

    // ── Patch missing entries ─────────────────────────────────────────────────

    /// <summary>
    /// Injects any missing key/value pairs into the existing LocalizationData assets.
    /// Uses SerializedObject so Unity tracks the changes properly.
    /// </summary>
    private static void PatchMissingEntries()
    {
        // Group missing entries by language code
        Dictionary<string, List<(string key, string value)>> byLang = new();
        foreach ((string lang, string key, string value) in MissingEntries)
        {
            if (!byLang.ContainsKey(lang))
                byLang[lang] = new List<(string, string)>();
            byLang[lang].Add((key, value));
        }

        string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LocalizationData data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
            if (data == null) continue;
            if (!byLang.TryGetValue(data.languageCode, out List<(string key, string value)> toAdd)) continue;

            SerializedObject so       = new SerializedObject(data);
            SerializedProperty entries = so.FindProperty("_entries");

            foreach ((string key, string value) in toAdd)
            {
                // Check if key already exists
                bool found = false;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                entries.InsertArrayElementAtIndex(entries.arraySize);
                SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                newEntry.FindPropertyRelative("key").stringValue   = key;
                newEntry.FindPropertyRelative("value").stringValue = value;
                Debug.Log($"[LocalizedTextBinder] Patched '{data.languageCode}': {key} = {value}");
            }

            so.ApplyModifiedProperties();
            data.InvalidateCache();
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();
    }

    // ── Scene processing ─────────────────────────────────────────────────────

    private static void ProcessScene(Scene scene, out int bound, out int skipped, out int unmapped)
    {
        bound    = 0;
        skipped  = 0;
        unmapped = 0;

        // FindObjectsOfType does NOT include disabled GameObjects — we walk manually.
        List<TMP_Text> allTexts = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectTMPRecursive(root.transform, allTexts);

        foreach (TMP_Text tmp in allTexts)
        {
            // Skip texts already bound (key already set)
            LocalizedText existing = tmp.GetComponent<LocalizedText>();
            if (existing != null)
            {
                SerializedObject exSo = new SerializedObject(existing);
                string existingKey = exSo.FindProperty("_key").stringValue;
                if (!string.IsNullOrEmpty(existingKey))
                {
                    skipped++;
                    continue;
                }
            }

            string rawText = tmp.text ?? string.Empty;
            string trimmed = rawText.Trim();

            // Skip dynamic GameObjects by name (price labels, description panels, etc.)
            if (DynamicGameObjectNames.Contains(tmp.gameObject.name))
            {
                skipped++;
                continue;
            }

            // Skip purely dynamic / numeric / empty texts
            if (string.IsNullOrEmpty(trimmed) || IsDynamic(trimmed))
            {
                skipped++;
                continue;
            }

            // Try to find the matching key
            string key = FindKey(trimmed);
            if (key == null)
            {
                Debug.LogWarning($"[LocalizedTextBinder] No key for \"{Truncate(trimmed, 60)}\" " +
                                 $"on '{GetPath(tmp.transform)}' in '{scene.name}'");
                unmapped++;
                continue;
            }

            // Add or update LocalizedText
            LocalizedText lt = tmp.GetComponent<LocalizedText>();
            if (lt == null)
                lt = Undo.AddComponent<LocalizedText>(tmp.gameObject);

            SerializedObject so = new SerializedObject(lt);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tmp.gameObject);
            bound++;
        }

        if (bound > 0)
            EditorSceneManager.MarkSceneDirty(scene);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Recursively collects all TMP_Text components, including on disabled GameObjects.</summary>
    private static void CollectTMPRecursive(Transform t, List<TMP_Text> results)
    {
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
            results.Add(tmp);

        for (int i = 0; i < t.childCount; i++)
            CollectTMPRecursive(t.GetChild(i), results);
    }

    /// <summary>
    /// Tries to match a text string to a localization key.
    /// First tries an exact French match, then a case-insensitive match.
    /// </summary>
    private static string FindKey(string text)
    {
        if (_frenchToKey.TryGetValue(text, out string key))
            return key;

        // Case-insensitive fallback
        foreach (KeyValuePair<string, string> pair in _frenchToKey)
        {
            if (string.Equals(pair.Key, text, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }

    private static bool IsDynamic(string text)
    {
        // Pure number
        if (float.TryParse(text, out _)) return true;
        // Known dynamic placeholder
        return DynamicPatterns.Contains(text.ToLowerInvariant());
    }

    /// <summary>Builds the French text → key reverse lookup from all LocalizationData assets.</summary>
    private static void BuildFrenchLookup()
    {
        _frenchToKey = new Dictionary<string, string>(StringComparer.Ordinal);

        string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LocalizationData data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
            if (data == null || !string.Equals(data.languageCode, "fr", StringComparison.OrdinalIgnoreCase))
                continue;

            // Use reflection to read the private _entries list
            System.Reflection.FieldInfo field = typeof(LocalizationData)
                .GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null) continue;

            System.Collections.IList entries = field.GetValue(data) as System.Collections.IList;
            if (entries == null) continue;

            foreach (object entry in entries)
            {
                System.Type entryType = entry.GetType();
                string k = entryType.GetField("key")?.GetValue(entry) as string;
                string v = entryType.GetField("value")?.GetValue(entry) as string;

                if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v))
                    _frenchToKey[v] = k;
            }
        }

        Debug.Log($"[LocalizedTextBinder] French lookup built: {_frenchToKey.Count} entries.");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t    = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
#endif
