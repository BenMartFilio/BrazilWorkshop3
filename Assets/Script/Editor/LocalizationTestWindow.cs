#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Drakensland.Localization;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window to preview and switch locales at runtime or in edit mode.
/// Open via <b>Drakensland > Localization Tester</b>.
/// </summary>
public class LocalizationTestWindow : EditorWindow
{
    // ── Constants ──────────────────────────────────────────────────────────────

    private const string WINDOW_TITLE = "Localization Tester";
    private const string PREFS_LOCALE = "LocalizationTester_LastLocale";
    private const float  COL_KEY_W   = 260f;
    private const float  COL_VAL_W   = 300f;
    private const float  COL_STS_W   = 40f;

    // ── State ──────────────────────────────────────────────────────────────────

    private List<LocalizationData> _locales       = new();
    private LocalizationData       _selected;
    private int                    _selectedIndex = -1;
    private string                 _searchQuery   = string.Empty;
    private Vector2                _scrollPos;
    private bool                   _showMissingOnly;

    // Cached row textures — created once in OnEnable, destroyed in OnDisable
    private Texture2D _rowTexEven;
    private Texture2D _rowTexOdd;
    private GUIStyle  _rowStyleEven;
    private GUIStyle  _rowStyleOdd;

    // Cached flat array of all keys from LocalizationKeys
    private string[] _allKeys;

    // ── Menu item ──────────────────────────────────────────────────────────────

    [MenuItem("Drakensland/Localization Tester")]
    public static void Open()
    {
        LocalizationTestWindow window = GetWindow<LocalizationTestWindow>(WINDOW_TITLE);
        window.minSize = new Vector2(640f, 440f);
        window.Show();
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        BuildRowStyles();
        CacheAllKeys();
        RefreshLocales();
    }

    private void OnDisable()
    {
        DestroyImmediate(_rowTexEven);
        DestroyImmediate(_rowTexOdd);
    }

    // ── GUI ────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        // Rebuild styles if they were lost (e.g. after domain reload)
        if (_rowStyleEven == null || _rowTexEven == null)
            BuildRowStyles();

        DrawToolbar();
        EditorGUILayout.Space(4f);

        if (_locales.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No LocalizationData assets found in the project.\n" +
                "Create one via: Assets > Create > Drakensland > Localization > Localization Data",
                MessageType.Warning);
            return;
        }

        DrawLocaleSelector();
        EditorGUILayout.Space(6f);
        DrawRuntimeControls();
        EditorGUILayout.Space(6f);
        DrawSearchBar();
        EditorGUILayout.Space(4f);
        DrawEntryTable();
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(WINDOW_TITLE, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(62f)))
        {
            CacheAllKeys();
            RefreshLocales();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLocaleSelector()
    {
        EditorGUILayout.LabelField("Locale", EditorStyles.boldLabel);

        string[] options = BuildLocaleOptions();

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(_selectedIndex, options, GUILayout.Width(220f));
        if (EditorGUI.EndChangeCheck())
            SelectLocale(newIndex);

        GUILayout.Space(8f);

        GUI.enabled = _selected != null;
        if (GUILayout.Button("Ping", GUILayout.Width(50f)))
            EditorGUIUtility.PingObject(_selected);
        if (GUILayout.Button("Select", GUILayout.Width(56f)))
            Selection.activeObject = _selected;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRuntimeControls()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to apply a locale at runtime.",
                MessageType.Info);
            return;
        }

        if (LocalizationManager.Instance == null)
        {
            EditorGUILayout.HelpBox(
                "LocalizationManager not found in the scene.",
                MessageType.Warning);
            return;
        }

        string activeLabel = LocalizationManager.Instance.ActiveLocale != null
            ? $"{LocalizationManager.Instance.ActiveLocale.displayName} " +
              $"({LocalizationManager.Instance.ActiveLocale.languageCode})"
            : "—";
        EditorGUILayout.LabelField($"Runtime locale: {activeLabel}", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = _selected != null;
        if (GUILayout.Button(
            $"Apply '{(_selected != null ? _selected.displayName : "—")}' at Runtime",
            GUILayout.Height(24f)))
        {
            LocalizationManager.Instance.SetLocale(_selected.languageCode);
        }
        GUI.enabled = true;

        if (GUILayout.Button("Reset to Device Locale", GUILayout.Width(160f), GUILayout.Height(24f)))
            LocalizationManager.Instance.ResetToDeviceLocale();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filter:", GUILayout.Width(42f));
        _searchQuery = EditorGUILayout.TextField(_searchQuery, GUILayout.ExpandWidth(true));
        GUILayout.Space(8f);
        _showMissingOnly = GUILayout.Toggle(_showMissingOnly, "Missing only", GUILayout.Width(100f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEntryTable()
    {
        if (_selected == null)
        {
            EditorGUILayout.HelpBox("Select a locale above to inspect its entries.", MessageType.None);
            return;
        }

        // ── Column headers ──────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Key",    EditorStyles.miniLabel, GUILayout.Width(COL_KEY_W));
        EditorGUILayout.LabelField("Value",  EditorStyles.miniLabel, GUILayout.Width(COL_VAL_W));
        EditorGUILayout.LabelField("Status", EditorStyles.miniLabel, GUILayout.Width(COL_STS_W));
        EditorGUILayout.EndHorizontal();

        // ── Scrollable rows ─────────────────────────────────────────────────
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        bool alternate = false;
        bool hasFilter = !string.IsNullOrEmpty(_searchQuery);
        int  present   = 0;

        foreach (string key in _allKeys)
        {
            bool hasKey = _selected.HasKey(key);
            if (hasKey) present++;

            if (_showMissingOnly && hasKey) continue;
            if (hasFilter && !key.Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase)) continue;

            EditorGUILayout.BeginHorizontal(alternate ? _rowStyleOdd : _rowStyleEven);
            alternate = !alternate;

            EditorGUILayout.LabelField(key, EditorStyles.miniLabel, GUILayout.Width(COL_KEY_W));

            string val = hasKey ? _selected.Get(key) : string.Empty;
            EditorGUILayout.LabelField(val, EditorStyles.miniLabel, GUILayout.Width(COL_VAL_W));

            Color prev = GUI.contentColor;
            GUI.contentColor = hasKey ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            EditorGUILayout.LabelField(hasKey ? "✓" : "✗", EditorStyles.miniLabel, GUILayout.Width(COL_STS_W));
            GUI.contentColor = prev;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // ── Footer ──────────────────────────────────────────────────────────
        int total = _allKeys.Length;
        int pct   = total > 0 ? present * 100 / total : 0;
        EditorGUILayout.LabelField($"{present}/{total} keys translated  ({pct}%)", EditorStyles.miniLabel);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void RefreshLocales()
    {
        _locales.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LocalizationData data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
            if (data != null)
                _locales.Add(data);
        }

        _locales.Sort((a, b) => string.Compare(
            a.languageCode, b.languageCode, System.StringComparison.Ordinal));

        if (_selected != null)
        {
            int idx = _locales.IndexOf(_selected);
            _selectedIndex = idx >= 0 ? idx : 0;
            if (_locales.Count > 0) _selected = _locales[_selectedIndex];
        }
        else if (_locales.Count > 0)
        {
            if (PlayerPrefs.HasKey(PREFS_LOCALE))
            {
                string saved = PlayerPrefs.GetString(PREFS_LOCALE);
                int idx = _locales.FindIndex(l => l != null && l.languageCode == saved);
                SelectLocale(idx >= 0 ? idx : 0);
            }
            else
            {
                SelectLocale(0);
            }
        }

        Repaint();
    }

    private void SelectLocale(int index)
    {
        if (index < 0 || index >= _locales.Count) return;

        _selectedIndex = index;
        _selected      = _locales[index];

        if (_selected != null)
        {
            PlayerPrefs.SetString(PREFS_LOCALE, _selected.languageCode);
            PlayerPrefs.Save();
        }

        Repaint();
    }

    private string[] BuildLocaleOptions()
    {
        string[] options = new string[_locales.Count];
        for (int i = 0; i < _locales.Count; i++)
        {
            LocalizationData l = _locales[i];
            options[i] = l != null ? $"{l.displayName}  ({l.languageCode})" : "—";
        }
        return options;
    }

    private void CacheAllKeys()
    {
        FieldInfo[] fields = typeof(LocalizationKeys).GetFields(
            BindingFlags.Public | BindingFlags.Static);

        var keys = new List<string>(fields.Length);
        foreach (FieldInfo f in fields)
        {
            if (f.FieldType == typeof(string))
                keys.Add((string)f.GetValue(null));
        }

        _allKeys = keys.ToArray();
    }

    private void BuildRowStyles()
    {
        _rowTexEven = MakeTex(new Color(0.20f, 0.20f, 0.20f));
        _rowTexOdd  = MakeTex(new Color(0.24f, 0.24f, 0.24f));

        _rowStyleEven = new GUIStyle { normal = { background = _rowTexEven } };
        _rowStyleOdd  = new GUIStyle { normal = { background = _rowTexOdd  } };
    }

    private static Texture2D MakeTex(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
#endif
