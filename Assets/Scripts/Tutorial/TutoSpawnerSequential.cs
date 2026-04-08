using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop-in replacement for SpawnObstacleV2 on the Spawner GameObject in MapTuto.
/// Plays an explicit ordered list of ObstaclePattern assets, then fires OnSequenceComplete.
/// The last pattern (PaternTuto3) loops until the player successfully collects a document,
/// which is detected via Aspiration.OnDocumentCollected.
/// </summary>
public class TutoSpawnerSequential : MonoBehaviour
{
    [Header("Tutorial Pattern Queue")]
    [Tooltip("Patterns played in order, top to bottom.")]
    public ObstaclePattern[] tutorialPatterns;

    [Header("Colored Car Settings")]
    [Tooltip("Index of the last pattern in tutorialPatterns that is the looping car pattern (0-based).")]
    public int loopingPatternIndex = 4;

    [Tooltip("Skin index to force on all ChangeSkin cars in the looping pattern. " +
             "GoodNormalVoitureObstacle sprites: 0=ITA, 1=BSFI, 2=DCR, 3=MCS. " +
             "Set to -1 to keep random.")]
    public int forcedCarSkinIndex = 0;

    [Header("Speed Reference")]
    public float baseObstacleSpeed = 5f;
    public float gapBetweenPatterns = 2f;

    [Header("References")]
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GameObject[] _fallingLines;    // left, center, right
    [SerializeField] private GoundMouvement[] _grounds;

    // ── Pool ──────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, List<GameObject>> _pool = new();

    // ── Speed ─────────────────────────────────────────────────────────────────
    private float _generalSpeed = 0f;

    // ── Coroutine handle ──────────────────────────────────────────────────────
    private Coroutine _spawningCoroutine;

    // ── Pattern index ─────────────────────────────────────────────────────────
    private int _currentPatternIndex = 0;
    private bool _currentPatternEventFired = false;

    // ── Document collection tracking ──────────────────────────────────────────
    private bool _documentCollectedThisLoop = false;

    // ── Pause guard ───────────────────────────────────────────────────────────
    private bool _speedUpdatePaused = false;

    // ── Public state ─────────────────────────────────────────────────────────

    /// <summary>True while the spawn coroutine is running.</summary>
    public bool isSpawning { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Invoked when the last pattern in the queue has fully finished
    /// AND the player has successfully collected a document.</summary>
    public event System.Action OnSequenceComplete;

    /// <summary>Invoked just before pattern at index i begins spawning its first row.</summary>
    public event System.Action<int> OnPatternAboutToStart;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (_timeManager != null)
            _timeManager.OnTimePassed += OnTimePassed;

        Aspiration.OnDocumentCollected += OnDocumentCollectedDuringLoop;
    }

    private void OnDisable()
    {
        if (_timeManager != null)
            _timeManager.OnTimePassed -= OnTimePassed;

        Aspiration.OnDocumentCollected -= OnDocumentCollectedDuringLoop;
    }

    private void OnDocumentCollectedDuringLoop(Barrage.Formulaires.FormulaireType _)
    {
        _documentCollectedThisLoop = true;
    }

    private void OnTimePassed()
    {
        _generalSpeed = Mathf.Clamp(_generalSpeed + 0.5f, 0f, 30f);

        // Always accumulate speed, but only push it to live objects when not paused.
        if (_speedUpdatePaused) return;

        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null && obj.activeInHierarchy && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                    scrolling.UpdateSpeed(_generalSpeed);
            }
        }

        if (_grounds != null)
        {
            for (int i = 0; i < _grounds.Length; i++)
            {
                if (_grounds[i] != null)
                    _grounds[i].UpdateSpeed(_generalSpeed);
            }
        }
    }

    // ── Public control ────────────────────────────────────────────────────────

    /// <summary>Starts playing the pattern queue from the current index.</summary>
    public void StartSpawning()
    {
        if (isSpawning) return;

        isSpawning = true;
        _speedUpdatePaused = false;

        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null && obj.activeInHierarchy && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                {
                    scrolling.StartMoving();
                    scrolling.UpdateSpeed(_generalSpeed);
                }
            }
        }

        if (_spawningCoroutine == null)
            _spawningCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>Stops the coroutine and freezes all pooled objects.</summary>
    public void StopSpawning()
    {
        isSpawning = false;
        _speedUpdatePaused = true;

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

    /// <summary>Updates the cached speed and propagates it to all live objects and grounds.</summary>
    public void UpdateAllSpeeds(float speed)
    {
        _generalSpeed = speed;
        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj != null && obj.activeInHierarchy && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                    scrolling.UpdateSpeed(_generalSpeed);
            }
        }

        if (_grounds != null)
        {
            for (int i = 0; i < _grounds.Length; i++)
            {
                if (_grounds[i] != null)
                    _grounds[i].UpdateSpeed(_generalSpeed);
            }
        }
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private IEnumerator SpawnRoutine()
    {
        if (tutorialPatterns == null || tutorialPatterns.Length == 0)
        {
            Debug.LogWarning("[TutoSpawnerSequential] tutorialPatterns is null or empty. Nothing to spawn.");
            _spawningCoroutine = null;
            yield break;
        }

        while (_currentPatternIndex < tutorialPatterns.Length && isSpawning)
        {
            ObstaclePattern pattern = tutorialPatterns[_currentPatternIndex];

            if (pattern == null)
            {
                Debug.LogWarning($"[TutoSpawnerSequential] Pattern at index {_currentPatternIndex} is null. Skipping.");
                _currentPatternIndex++;
                _currentPatternEventFired = false;
                continue;
            }

            bool isLoopingPattern = (_currentPatternIndex == loopingPatternIndex);

            // Fire the event only once per pattern entry, even if the coroutine is resumed.
            if (!_currentPatternEventFired)
            {
                _currentPatternEventFired = true;

                // Reset document flag at the start of each fresh pass through the looping pattern.
                if (isLoopingPattern)
                    _documentCollectedThisLoop = false;

                OnPatternAboutToStart?.Invoke(_currentPatternIndex);

                // Yield one frame so TutorialManager has time to react.
                yield return null;

                if (!isSpawning)
                {
                    _spawningCoroutine = null;
                    yield break;
                }
            }

            yield return StartCoroutine(SpawnPatternCoroutine(pattern));

            if (!isSpawning)
            {
                _spawningCoroutine = null;
                yield break;
            }

            // If this is the looping pattern and the player missed the car, replay it.
            if (isLoopingPattern && !_documentCollectedThisLoop)
            {
                // Reset so OnPatternAboutToStart does NOT re-fire (silent re-loop).
                _currentPatternEventFired = true;
                _documentCollectedThisLoop = false;
                // Do not increment — stay on this index and loop.
                continue;
            }

            _currentPatternIndex++;
            _currentPatternEventFired = false;
        }

        _spawningCoroutine = null;

        if (_currentPatternIndex >= tutorialPatterns.Length)
            OnSequenceComplete?.Invoke();
    }

    /// <summary>Emits each row in the pattern sequentially, waiting for distance between rows.</summary>
    private IEnumerator SpawnPatternCoroutine(ObstaclePattern pattern)
    {
        bool isLoopingPattern = (_currentPatternIndex == loopingPatternIndex);

        for (int rowIndex = 0; rowIndex < pattern.rows.Count; rowIndex++)
        {
            if (!isSpawning) yield break;

            SpawnRow(pattern.rows[rowIndex], isLoopingPattern);
            yield return StartCoroutine(WaitForDistance(pattern.rowSpacing));

            if (!isSpawning) yield break;
        }

        yield return StartCoroutine(WaitForDistance(gapBetweenPatterns));
    }

    /// <summary>Waits until the virtual distance accumulated reaches the target. Aborts if paused.</summary>
    private IEnumerator WaitForDistance(float distance)
    {
        float travelled = 0f;
        while (travelled < distance)
        {
            if (!isSpawning) yield break;

            float currentSpeed = (_grounds != null && _grounds.Length > 0 && _grounds[0] != null)
                ? _grounds[0].speed
                : baseObstacleSpeed + _generalSpeed;

            travelled += currentSpeed * Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private void SpawnRow(PatternRow row, bool forceCarSkin)
    {
        if (row == null) return;
        if (_fallingLines == null || _fallingLines.Length < 3)
        {
            Debug.LogWarning("[TutoSpawnerSequential] _fallingLines needs at least 3 entries.");
            return;
        }

        for (int lane = 0; lane < 3; lane++)
        {
            if (!isSpawning) break;

            GameObject prefab = row.lanes[lane];
            if (prefab == null) continue;

            GameObject obj = GetFromPool(prefab);

            Vector3 spawnPos = new Vector3(
                _fallingLines[lane].transform.position.x,
                transform.position.y,
                transform.position.z
            );

            obj.transform.SetPositionAndRotation(spawnPos, prefab.transform.rotation);

            // ChangeSkin lives on a child ("Sprite"), not the root — use GetComponentInChildren.
            // Set forcedSkinIndex before SetActive so OnEnable → ChangerSkin picks the right value.
            // Then call ChangerSkin() explicitly after SetActive to cover the case where the
            // prefab is active by default (Instantiate fires OnEnable immediately, before we
            // can set anything, and a subsequent SetActive(true) on an active object is a no-op).
            ChangeSkin skin = obj.GetComponentInChildren<ChangeSkin>();
            if (skin != null)
            {
                skin.forcedSkinIndex = (forceCarSkin && forcedCarSkinIndex >= 0)
                    ? forcedCarSkinIndex
                    : -1;
            }

            obj.SetActive(true);

            if (skin != null)
                skin.ChangerSkin();

            if (obj.TryGetComponent<ScrollingElement>(out var scrolling))
                scrolling.UpdateSpeed(_generalSpeed);
        }
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
