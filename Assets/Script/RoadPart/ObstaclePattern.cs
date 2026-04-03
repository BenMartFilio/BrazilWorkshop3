using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single row of the pattern grid (one lane position in depth).
/// </summary>
[Serializable]
public class PatternRow
{
    /// <summary>
    /// The obstacle prefab to spawn in each lane (null = empty lane).
    /// Array size must match the number of lanes (3).
    /// </summary>
    public GameObject[] lanes = new GameObject[3];
}

/// <summary>
/// ScriptableObject that defines an obstacle pattern.
/// Each pattern is a grid: columns = lanes (3), rows = depth slots.
/// </summary>
[CreateAssetMenu(fileName = "NewObstaclePattern", menuName = "Road Game/Obstacle Pattern")]
public class ObstaclePattern : ScriptableObject
{
    [Tooltip("Number of lane positions along the depth axis.")]
    [Min(1)]
    public int rowCount = 3;

    [Tooltip("Spacing between rows along the Z axis (world units).")]
    public float rowSpacing = 2f;

    [Tooltip("Grid rows, from front to back. Each row has 3 lane slots.")]
    public List<PatternRow> rows = new List<PatternRow>();

    [Tooltip("Nombre de véhicules 'Good' (portant un formulaire, i.e. avec ChangeSkin) " +
             "contenus dans ce pattern. À renseigner manuellement pour que le système " +
             "de budget FormulaireSpawnBudget puisse calculer le nombre de patterns " +
             "nécessaires entre deux barrages.")]
    [Min(0)]
    public int bonusVehiculeCount = 0;

    private void OnValidate()
    {
        SyncRowCount();
    }

    /// <summary>
    /// Ensures the rows list matches rowCount and each row has exactly 3 lanes.
    /// </summary>
    public void SyncRowCount()
    {
        while (rows.Count < rowCount)
        {
            PatternRow newRow = new PatternRow();
            newRow.lanes = new GameObject[3];
            rows.Add(newRow);
        }

        while (rows.Count > rowCount)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == null)
                rows[i] = new PatternRow();

            if (rows[i].lanes == null || rows[i].lanes.Length != 3)
            {
                GameObject[] updated = new GameObject[3];
                if (rows[i].lanes != null)
                {
                    int copy = Mathf.Min(rows[i].lanes.Length, 3);
                    for (int j = 0; j < copy; j++)
                        updated[j] = rows[i].lanes[j];
                }
                rows[i].lanes = updated;
            }
        }
    }
}
