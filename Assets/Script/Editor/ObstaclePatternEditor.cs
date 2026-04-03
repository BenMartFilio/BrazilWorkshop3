using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for ObstaclePattern.
/// Shows a compact colour-coded visual grid above the editable object fields.
/// Each row = one depth slot (front → back). Each column = one lane (L / C / R).
/// A legend drawn below the grid shows only the prefab types present in that pattern.
/// The editable object-field grid is collapsed into an "Edit Cells" foldout.
/// </summary>
[CustomEditor(typeof(ObstaclePattern))]
public class ObstaclePatternEditor : Editor
{
    // ── Grid dimensions ───────────────────────────────────────────────────────
    private const float CellSize          = 36f;
    private const float CellSpacing       = 3f;
    private const float RowLabelWidth     = 44f;
    private const float LaneLabelHeight   = 18f;
    private const float GridLeftPad       = 8f;

    // ── Edit-field dimensions ─────────────────────────────────────────────────
    private const float EditCellWidth     = 152f;
    private const float EditCellHeight    = 18f;
    private const float EditColumnSpacing = 4f;

    // ── Lane names ────────────────────────────────────────────────────────────
    private static readonly string[] LaneNames = { "L", "C", "R" };
    private static readonly string[] LaneFull  = { "Left", "Center", "Right" };

    // ── Colour palette ────────────────────────────────────────────────────────
    // Evaluated in order — first match wins.
    private static readonly (string key, string label, Color color)[] ColorRules =
    {
        ("Coins",    "coin", new Color(1.00f, 0.85f, 0.10f, 1f)),  // gold
        ("Good",     "good", new Color(0.20f, 0.75f, 0.30f, 1f)),  // green
        ("Barrel",   "brl",  new Color(0.60f, 0.35f, 0.10f, 1f)),  // brown
        ("Barrer",   "bar",  new Color(0.80f, 0.20f, 0.20f, 1f)),  // red
        ("Barer",    "bar",  new Color(0.80f, 0.20f, 0.20f, 1f)),  // red
        ("Camion",   "trk",  new Color(0.50f, 0.20f, 0.70f, 1f)),  // purple
        ("Inversed", "inv",  new Color(0.90f, 0.50f, 0.10f, 1f)),  // orange
        ("Voiture",  "car",  new Color(0.20f, 0.50f, 0.90f, 1f)),  // blue
    };

    private static readonly Color ColorEmpty      = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color ColorBg         = new Color(0.13f, 0.13f, 0.13f, 1f);
    private static readonly Color ColorCellBorder = new Color(0.08f, 0.08f, 0.08f, 1f);

    // ── Style cache ───────────────────────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _cellLabelStyle;
    private GUIStyle _rowLabelStyle;
    private GUIStyle _laneLabelStyle;
    private GUIStyle _legendLabelStyle;

    // ── Foldout state ─────────────────────────────────────────────────────────
    private bool _showEditFields = false;

    // ─────────────────────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ObstaclePattern pattern = (ObstaclePattern)target;

        InitStyles();
        pattern.SyncRowCount();

        // ── Standard scalar fields ────────────────────────────────────────────
        EditorGUILayout.LabelField("Pattern Settings", _headerStyle);
        EditorGUILayout.Space(2);

        EditorGUI.BeginChangeCheck();
        int newRowCount = EditorGUILayout.IntField("Row Count", pattern.rowCount);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pattern, "Change Row Count");
            pattern.rowCount = Mathf.Max(1, newRowCount);
            pattern.SyncRowCount();
            EditorUtility.SetDirty(pattern);
        }

        EditorGUI.BeginChangeCheck();
        float newSpacing = EditorGUILayout.FloatField("Row Spacing", pattern.rowSpacing);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pattern, "Change Row Spacing");
            pattern.rowSpacing = newSpacing;
            EditorUtility.SetDirty(pattern);
        }

        EditorGUI.BeginChangeCheck();
        int newBonus = EditorGUILayout.IntField("Bonus Vehicule Count", pattern.bonusVehiculeCount);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pattern, "Change Bonus Vehicule Count");
            pattern.bonusVehiculeCount = Mathf.Max(0, newBonus);
            EditorUtility.SetDirty(pattern);
        }

        EditorGUILayout.Space(10);

        // ── Visual grid ───────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Visual Grid  (front → back)", _headerStyle);
        EditorGUILayout.Space(4);

        DrawVisualGrid(pattern);
        EditorGUILayout.Space(4);
        DrawLegend(pattern);

        EditorGUILayout.Space(10);

        // ── Editable object-field grid (collapsible) ──────────────────────────
        _showEditFields = EditorGUILayout.Foldout(_showEditFields, "Edit Cells", true);
        if (_showEditFields)
        {
            EditorGUILayout.Space(4);
            DrawEditColumnHeaders();

            for (int rowIndex = 0; rowIndex < pattern.rows.Count; rowIndex++)
                DrawEditRow(pattern, rowIndex);

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Clear All Cells"))
            {
                Undo.RecordObject(pattern, "Clear Pattern");
                foreach (PatternRow row in pattern.rows)
                    for (int l = 0; l < 3; l++)
                        row.lanes[l] = null;
                EditorUtility.SetDirty(pattern);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Visual grid ──────────────────────────────────────────────────────────

    private void DrawVisualGrid(ObstaclePattern pattern)
    {
        int rows  = pattern.rows.Count;
        int lanes = 3;

        float gridW = RowLabelWidth + (CellSize + CellSpacing) * lanes;
        float gridH = LaneLabelHeight + (CellSize + CellSpacing) * rows;

        Rect gridRect = GUILayoutUtility.GetRect(gridW, gridH + 4f, GUILayout.ExpandWidth(false));
        gridRect.x += GridLeftPad;

        EditorGUI.DrawRect(gridRect, ColorBg);

        // Lane header labels.
        for (int lane = 0; lane < lanes; lane++)
        {
            float cellX    = gridRect.x + RowLabelWidth + lane * (CellSize + CellSpacing);
            Rect labelRect = new Rect(cellX, gridRect.y, CellSize, LaneLabelHeight);
            GUI.Label(labelRect, LaneNames[lane], _laneLabelStyle);
        }

        // Rows.
        for (int rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            float cellY   = gridRect.y + LaneLabelHeight + rowIndex * (CellSize + CellSpacing);
            Rect rowLabel = new Rect(gridRect.x, cellY, RowLabelWidth, CellSize);
            GUI.Label(rowLabel, $"R{rowIndex}", _rowLabelStyle);

            PatternRow row = pattern.rows[rowIndex];

            for (int lane = 0; lane < lanes; lane++)
            {
                float cellX   = gridRect.x + RowLabelWidth + lane * (CellSize + CellSpacing);
                Rect cellRect = new Rect(cellX, cellY, CellSize, CellSize);

                GameObject prefab = (row?.lanes != null && lane < row.lanes.Length)
                    ? row.lanes[lane] : null;

                DrawCell(cellRect, prefab);
            }
        }

        if (gridRect.Contains(Event.current.mousePosition))
            Repaint();
    }

    private void DrawCell(Rect rect, GameObject prefab)
    {
        EditorGUI.DrawRect(rect, ColorCellBorder);
        Rect inner = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);

        if (prefab == null)
        {
            EditorGUI.DrawRect(inner, ColorEmpty);
            return;
        }

        (string _, string label, Color color) = ClassifyPrefab(prefab.name);
        EditorGUI.DrawRect(inner, color);
        GUI.Label(inner, label, _cellLabelStyle);

        // Tooltip on hover — full prefab name in a helpbox below the cell.
        if (inner.Contains(Event.current.mousePosition))
        {
            GUIContent tip = new GUIContent(prefab.name);
            Vector2 size   = EditorStyles.helpBox.CalcSize(tip);
            Rect tipRect   = new Rect(inner.x, inner.yMax + 2f, Mathf.Max(size.x, 80f), size.y + 2f);
            GUI.Box(tipRect, tip, EditorStyles.helpBox);
        }
    }

    // ── Legend ────────────────────────────────────────────────────────────────

    private void DrawLegend(ObstaclePattern pattern)
    {
        var present = new HashSet<string>();
        foreach (PatternRow row in pattern.rows)
        {
            if (row?.lanes == null) continue;
            foreach (GameObject go in row.lanes)
                if (go != null) present.Add(go.name);
        }

        if (present.Count == 0) return;

        var entries = new List<(string label, Color color, string shortName)>();
        foreach (string name in present)
        {
            (string _, string lbl, Color col) = ClassifyPrefab(name);
            string shortName = name
                .Replace("Obstacle", "")
                .Replace("Ennemy",   "")
                .Replace("Voiture",  "Car")
                .Replace("Normal",   "")
                .Trim();
            entries.Add((lbl, col, shortName));
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(GridLeftPad);
        GUILayout.Label("Legend:", _legendLabelStyle, GUILayout.Width(46f));

        foreach ((string lbl, Color col, string shortName) in entries)
        {
            Rect swatchRect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
            swatchRect.y += 2f;
            EditorGUI.DrawRect(swatchRect, col);
            GUILayout.Space(2f);
            GUILayout.Label($"{lbl} {shortName}", _legendLabelStyle, GUILayout.ExpandWidth(false));
            GUILayout.Space(8f);
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── Editable field grid ───────────────────────────────────────────────────

    private void DrawEditColumnHeaders()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(RowLabelWidth));

        for (int lane = 0; lane < 3; lane++)
        {
            GUILayout.Label(LaneFull[lane], EditorStyles.boldLabel, GUILayout.Width(EditCellWidth));
            if (lane < 2) GUILayout.Space(EditColumnSpacing);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawEditRow(ObstaclePattern pattern, int rowIndex)
    {
        PatternRow row = pattern.rows[rowIndex];

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"R{rowIndex}", _rowLabelStyle,
            GUILayout.Width(RowLabelWidth), GUILayout.Height(EditCellHeight));

        for (int lane = 0; lane < 3; lane++)
        {
            EditorGUI.BeginChangeCheck();

            GameObject newVal = (GameObject)EditorGUILayout.ObjectField(
                row.lanes[lane], typeof(GameObject), false,
                GUILayout.Width(EditCellWidth), GUILayout.Height(EditCellHeight));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pattern, "Set Pattern Cell");
                row.lanes[lane] = newVal;
                EditorUtility.SetDirty(pattern);
            }

            if (lane < 2) GUILayout.Space(EditColumnSpacing);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(1f);
    }

    // ── Classification ────────────────────────────────────────────────────────

    /// <summary>Returns the first matching colour rule for a prefab name, or a grey fallback.</summary>
    private static (string key, string label, Color color) ClassifyPrefab(string prefabName)
    {
        foreach ((string key, string label, Color color) rule in ColorRules)
            if (prefabName.IndexOf(rule.key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return rule;

        return ("?", "?", new Color(0.45f, 0.45f, 0.45f, 1f));
    }

    // ── Style init ────────────────────────────────────────────────────────────

    private void InitStyles()
    {
        if (_headerStyle != null) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

        _cellLabelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white },
        };

        _rowLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            padding   = new RectOffset(0, 4, 0, 0),
            normal    = { textColor = new Color(0.65f, 0.65f, 0.65f, 1f) },
        };

        _laneLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 11,
            normal    = { textColor = new Color(0.75f, 0.75f, 0.75f, 1f) },
        };

        _legendLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.75f, 0.75f, 0.75f, 1f) },
        };
    }
}
