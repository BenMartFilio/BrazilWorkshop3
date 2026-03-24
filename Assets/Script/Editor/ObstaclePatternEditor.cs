using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for ObstaclePattern.
/// Draws the rows as a visual grid where each cell is a lane slot (3 columns).
/// </summary>
[CustomEditor(typeof(ObstaclePattern))]
public class ObstaclePatternEditor : Editor
{
    // ── Style cache ───────────────────────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _cellStyle;
    private GUIStyle _rowLabelStyle;

    private const float CellWidth = 160f;
    private const float CellHeight = 20f;
    private const float RowLabelWidth = 55f;
    private const float ColumnSpacing = 4f;

    private static readonly string[] LaneNames = { "Left", "Center", "Right" };

    // ─────────────────────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ObstaclePattern pattern = (ObstaclePattern)target;

        InitStyles();

        // ── Standard fields ───────────────────────────────────────────────────
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

        EditorGUILayout.Space(8);

        // ── Grid header ───────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Obstacle Grid  (front → back)", _headerStyle);
        EditorGUILayout.Space(4);

        DrawColumnHeaders();

        // ── Rows ──────────────────────────────────────────────────────────────
        pattern.SyncRowCount();

        for (int rowIndex = 0; rowIndex < pattern.rows.Count; rowIndex++)
        {
            DrawRow(pattern, rowIndex);
        }

        EditorGUILayout.Space(8);

        // ── Clear button ──────────────────────────────────────────────────────
        if (GUILayout.Button("Clear All Cells"))
        {
            Undo.RecordObject(pattern, "Clear Pattern");
            foreach (PatternRow row in pattern.rows)
            {
                for (int l = 0; l < 3; l++)
                    row.lanes[l] = null;
            }
            EditorUtility.SetDirty(pattern);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Drawing helpers ───────────────────────────────────────────────────────

    private void DrawColumnHeaders()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(RowLabelWidth));

        for (int lane = 0; lane < 3; lane++)
        {
            GUILayout.Label(LaneNames[lane], EditorStyles.boldLabel, GUILayout.Width(CellWidth));
            if (lane < 2) GUILayout.Space(ColumnSpacing);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRow(ObstaclePattern pattern, int rowIndex)
    {
        PatternRow row = pattern.rows[rowIndex];

        EditorGUILayout.BeginHorizontal();

        // Row label
        GUILayout.Label($"Row {rowIndex}", _rowLabelStyle, GUILayout.Width(RowLabelWidth), GUILayout.Height(CellHeight));

        // Lane cells
        for (int lane = 0; lane < 3; lane++)
        {
            EditorGUI.BeginChangeCheck();

            GameObject newVal = (GameObject)EditorGUILayout.ObjectField(
                row.lanes[lane],
                typeof(GameObject),
                false,
                GUILayout.Width(CellWidth),
                GUILayout.Height(CellHeight)
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pattern, "Set Pattern Cell");
                row.lanes[lane] = newVal;
                EditorUtility.SetDirty(pattern);
            }

            if (lane < 2) GUILayout.Space(ColumnSpacing);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2);
    }

    // ── Style init ────────────────────────────────────────────────────────────

    private void InitStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
        }

        if (_cellStyle == null)
        {
            _cellStyle = new GUIStyle(EditorStyles.objectField);
        }

        if (_rowLabelStyle == null)
        {
            _rowLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(0, 4, 0, 0)
            };
        }
    }
}
