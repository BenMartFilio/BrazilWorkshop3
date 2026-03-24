using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns obstacle patterns in a continuous chain.
/// Rows are emitted one by one: each row waits for the distance of one
/// rowSpacing to be travelled before emitting the next (empty rows still
/// consume their delay). After the last row, the coroutine waits for it
/// to fully exit the screen before starting the next pattern.
/// Speed scaling is preserved from SpawnObstacle.
/// </summary>
public class SpawnObstacleV2 : MonoBehaviour
{
    [Header("Patterns")]
    [Tooltip("List of ObstaclePattern assets to pick from.")]
    public ObstaclePattern[] patterns;

    [Header("Speed Reference")]
    [Tooltip("Must match the baseSpeed value on the obstacles' ScrollingElement.")]
    public float baseObstacleSpeed = 5f;

    [Tooltip("Extra distance (world units) added between the end of one pattern and the start of the next.")]
    public float gapBetweenPatterns = 2f;

    public bool isSpawning = false;

    [Header("References")]
    [SerializeField] private TimeManager _timeManager;

    [Tooltip("The three lane anchor GameObjects ordered: left, center, right.")]
    [SerializeField] private GameObject[] _fallingLines;

    // ── Speed ─────────────────────────────────────────────────────────────────
    private float _generalSpeed = 0f;

    // ── Pool ──────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, List<GameObject>> _pool = new();

    // ── Shuffle bag ───────────────────────────────────────────────────────────
    private readonly List<int> _shuffleBag = new();

    // ── Coroutine handle ──────────────────────────────────────────────────────
    private Coroutine _spawningCoroutine;

    /// <summary>Y threshold below which ScrollingElement.Update despawns objects.</summary>
    private const float DespawnY = -20f;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _timeManager.OnTimePassed += OnTimePassed;
    }

    private void OnDisable()
    {
        _timeManager.OnTimePassed -= OnTimePassed;
    }

    private void Start()
    {
        StartSpawning();
    }

    // ── Speed scaling (mirrors SpawnObstacle.SpawnRate) ───────────────────────

    private void OnTimePassed()
    {
        _generalSpeed = Mathf.Clamp(_generalSpeed + 1, 0, 30);

        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null)
                    obj.GetComponent<ScrollingElement>().UpdateSpeed(_generalSpeed);
            }
        }
    }

    // ── Public control ────────────────────────────────────────────────────────

    /// <summary>Starts the spawn coroutine.</summary>
    public void StartSpawning()
    {
        isSpawning = true;
        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null && obj.activeInHierarchy)
                {
                    obj.GetComponent<ScrollingElement>().StartMoving();
                    obj.GetComponent<ScrollingElement>().UpdateSpeed(_generalSpeed);
                    obj.GetComponent<ScrollingElement>().Dispawn();
                }
            }
        }
        _spawningCoroutine ??= StartCoroutine(SpawnRoutine());

    }

    /// <summary>Stops spawning and freezes all pooled objects.</summary>
    public void StopSpawning()
    {
        isSpawning = false;

        if (_spawningCoroutine != null)
        {
            StopCoroutine(_spawningCoroutine);
            _spawningCoroutine = null;
        }

        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null && obj.activeInHierarchy)
                    obj.GetComponent<ScrollingElement>().StopMoving();
            }
        }
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            if (patterns == null || patterns.Length == 0)
            {
                Debug.LogWarning("[SpawnObstacleV2] No patterns assigned.");
                yield return null;
                continue;
            }

            if (_fallingLines == null || _fallingLines.Length < 3)
            {
                Debug.LogWarning("[SpawnObstacleV2] _fallingLines needs at least 3 entries.");
                yield return null;
                continue;
            }

            ObstaclePattern pattern = patterns[GetNextPatternIndex()];
            if (pattern == null) continue;

            yield return StartCoroutine(SpawnPatternCoroutine(pattern));
        }
    }

    /// <summary>
    /// Emits each row sequentially, timed so consecutive rows are spaced by
    /// rowSpacing. After the last row, waits for it to exit the screen before
    /// returning so the next pattern starts seamlessly.
    /// </summary>
    private IEnumerator SpawnPatternCoroutine(ObstaclePattern pattern)
    {
        for (int rowIndex = 0; rowIndex < pattern.rows.Count; rowIndex++)
        {
            // Spawn this row (empty lanes are skipped but the wait still happens).
            SpawnRow(pattern.rows[rowIndex]);

            // Wait for the virtual distance of one rowSpacing to be travelled.
            // Speed is re-sampled every frame so acceleration is accounted for.
            yield return StartCoroutine(WaitForDistance(pattern.rowSpacing));
        }

        // Wait for the last row to travel from the spawn Y down to DespawnY,
        // plus the configured gap before the next pattern begins.
        float exitDistance = ((transform.position.y - DespawnY) + gapBetweenPatterns)*0;
        yield return StartCoroutine(WaitForDistance(exitDistance));
    }

    /// <summary>
    /// Waits until the accumulated virtual distance (speed × deltaTime)
    /// reaches <paramref name="distance"/>. Re-samples speed every frame.
    /// </summary>
    private IEnumerator WaitForDistance(float distance)
    {
        float travelled = 0f;
        while (travelled < distance)
        {
            float currentSpeed = baseObstacleSpeed + _generalSpeed;
            travelled += currentSpeed * Time.deltaTime;
            yield return null;
        }
    }

    private void SpawnRow(PatternRow row)
    {
        if (row == null) return;

        for (int lane = 0; lane < 3; lane++)
        {
            if (isSpawning)
            {
                GameObject prefab = row.lanes[lane];
                if (prefab == null) continue;

                GameObject obj = GetFromPool(prefab);

                Vector3 spawnPos = new Vector3(
                    _fallingLines[lane].transform.position.x,
                    transform.position.y,
                    transform.position.z
                );

                obj.transform.SetPositionAndRotation(spawnPos, prefab.transform.rotation);
                obj.SetActive(true);
                obj.GetComponent<ScrollingElement>().UpdateSpeed(_generalSpeed);
            }
        }
    }

    // ── Shuffle-bag ───────────────────────────────────────────────────────────

    private int GetNextPatternIndex()
    {
        if (_shuffleBag.Count == 0)
        {
            for (int i = 0; i < patterns.Length; i++)
                _shuffleBag.Add(i);

            for (int i = 0; i < _shuffleBag.Count; i++)
            {
                int r = Random.Range(i, _shuffleBag.Count);
                (_shuffleBag[i], _shuffleBag[r]) = (_shuffleBag[r], _shuffleBag[i]);
            }
        }

        int index = _shuffleBag[0];
        _shuffleBag.RemoveAt(0);
        return index;
    }

    // ── Object pool ───────────────────────────────────────────────────────────

    private GameObject GetFromPool(GameObject prefab)
    {
        string key = prefab.name;

        if (!_pool.ContainsKey(key))
            _pool[key] = new List<GameObject>();

        foreach (GameObject obj in _pool[key])
        {
            if (obj != null && !obj.activeInHierarchy)
                return obj;
        }

        GameObject newObj = Instantiate(prefab);
        _pool[key].Add(newObj);
        return newObj;
    }
}
