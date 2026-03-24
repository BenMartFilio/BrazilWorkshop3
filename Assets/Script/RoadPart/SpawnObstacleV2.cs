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
/// Le pattern Barrage est exclu du shuffle-bag normal et se déclenche
/// automatiquement après un nombre aléatoire de signaux TimeManager (entre
/// barrageSingauxMin et barrageSignauxMax).
/// </summary>
public class SpawnObstacleV2 : MonoBehaviour
{
    [Header("Patterns normaux")]
    [Tooltip("List of ObstaclePattern assets to pick from (ne pas inclure le pattern Barrage).")]
    public ObstaclePattern[] patterns;

    [Header("Pattern Barrage")]
    [Tooltip("Pattern spécial déclenché après un nombre de signaux TimeManager.")]
    [SerializeField] private ObstaclePattern patternBarrage;

    [Tooltip("Nombre minimum de signaux TimeManager avant l'apparition du Barrage.")]
    [SerializeField] private int barrageSignauxMin = 5;

    [Tooltip("Nombre maximum de signaux TimeManager avant l'apparition du Barrage.")]
    [SerializeField] private int barrageSignauxMax = 7;

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

    // ── Compteur Barrage ──────────────────────────────────────────────────────
    private int _signauxEcoules = 0;
    private int _prochainBarrageA = 0;
    private bool _barrageEnAttente = false;

    /// <summary>Y threshold below which ScrollingElement.Update despawns objects.</summary>
    private const float DespawnY = -20f;

    // ─────────────────────────────────────────────────────────────────────────

    [SerializeField] private GoundMouvement[] _grounds;


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
        TirerProchainSeuilBarrage();
        StartSpawning();
    }

    // ── Speed scaling ─────────────────────────────────────────────────────────

    private void OnTimePassed()
    {
        _generalSpeed = Mathf.Clamp(_generalSpeed + 1, 0, 30);

        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                    scrolling.UpdateSpeed(_generalSpeed);
            }
            for (int i = 0; i < _grounds.Length; i++)
            {
                _grounds[i].UpdateSpeed(_generalSpeed);
            }
        }

        // Compter les signaux et marquer le barrage comme attendu au bon moment.
        if (!_barrageEnAttente && patternBarrage != null)
        {
            _signauxEcoules++;
            if (_signauxEcoules >= _prochainBarrageA)
                _barrageEnAttente = true;
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
                if (obj != null && obj.activeInHierarchy && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                {
                    scrolling.StartMoving();
                    scrolling.UpdateSpeed(_generalSpeed);
                    scrolling.Dispawn();
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
                if (obj != null && obj.activeInHierarchy && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                    scrolling.StopMoving();
            }
        }
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            if (_fallingLines == null || _fallingLines.Length < 3)
            {
                Debug.LogWarning("[SpawnObstacleV2] _fallingLines needs at least 3 entries.");
                yield return null;
                continue;
            }

            // Si le barrage est en attente, on le spawn à la place du prochain pattern normal.
            ObstaclePattern prochain;
            if (_barrageEnAttente)
            {
                prochain = patternBarrage;
                _barrageEnAttente = false;
                _signauxEcoules = 0;
                TirerProchainSeuilBarrage();
            }
            else
            {
                if (patterns == null || patterns.Length == 0)
                {
                    Debug.LogWarning("[SpawnObstacleV2] No patterns assigned.");
                    yield return null;
                    continue;
                }
                prochain = patterns[GetNextPatternIndex()];
            }

            if (prochain == null) continue;

            yield return StartCoroutine(SpawnPatternCoroutine(prochain));
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
            SpawnRow(pattern.rows[rowIndex]);
            yield return StartCoroutine(WaitForDistance(pattern.rowSpacing));
        }

        float exitDistance = ((transform.position.y - DespawnY) + gapBetweenPatterns) * 0;
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

                if (obj.TryGetComponent<ScrollingElement>(out var scrolling))
                    scrolling.UpdateSpeed(_generalSpeed);
            }
        }
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>Sauvegarde la vitesse générale dans les données de session.</summary>
    public void SauvegarderDansSession(DonnéesSession donnees)
    {
        donnees.vitesseGénérale = _generalSpeed;
    }

    /// <summary>Restaure la vitesse générale depuis les données de session.</summary>
    public void RestaurerDepuisSession(float vitesse)
    {
        _generalSpeed = vitesse;
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

    // ── Barrage ───────────────────────────────────────────────────────────────

    private void TirerProchainSeuilBarrage()
    {
        _prochainBarrageA = Random.Range(barrageSignauxMin, barrageSignauxMax + 1);
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

