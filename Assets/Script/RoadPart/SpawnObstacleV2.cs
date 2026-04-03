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
    [Tooltip("Pattern spécial déclenché après un nombre de patterns normaux calculé par FormulaireSpawnBudget.")]
    [SerializeField] private ObstaclePattern patternBarrage;

    [Tooltip("Nombre minimum de patterns normaux à spawner avant le barrage (fallback si aucune session valide).")]
    [SerializeField] private int patternsMinFallback = 5;

    [Tooltip("Nombre maximum de patterns normaux à spawner avant le barrage (fallback si aucune session valide).")]
    [SerializeField] private int patternsMaxFallback = 7;

    [Tooltip("Distance vide (unités monde) imposée avant d'émettre le pattern Barrage (laisse la route dégagée).")]
    [SerializeField] private float gapAvantBarrage = 10f;

    [Tooltip("Distance vide (unités monde) imposée après le pattern Barrage (avant que les obstacles normaux reprennent).")]
    [SerializeField] private float gapAprèsBarrage = 10f;

    [Tooltip("Décalage X du root SegmentBarrage par rapport au centre de la route. " +
             "Compense l'offset interne du prefab (BarriereInspection localX=-23.9 × scale0.1 = -2.39) " +
             "pour que la barrière tombe au centre et le bureau à droite.")]
    [SerializeField] private float barrageOffsetX = 2.39f;

    [Header("Budget formulaires")]
    [Tooltip("Budget ScriptableObject partagé avec la scène Barrage. " +
             "Détermine quels types de véhicules forcer et combien de patterns spawner.")]
    [SerializeField] private FormulaireSpawnBudget spawnBudget;

    [Header("Speed Reference")]
    [Tooltip("Must match the baseSpeed value on the obstacles' ScrollingElement.")]
    public float baseObstacleSpeed = 5f;

    [Tooltip("Extra distance (world units) added between the end of one pattern and the start of the next.")]
    public float gapBetweenPatterns = 2f;

    // ── Evenement de spawn (Radar a Obstacles) ────────────────────────────────

    /// <summary>
    /// Declenche a chaque fois qu'un obstacle est spawne dans la scene.
    /// La position transmise est celle du spawn (haut de l'ecran).
    /// Le GameObject transmis permet au Radar de filtrer les pieces.
    /// Utilise par EffetsObjetsSpeciaux.SignalerNouvelObstacle pour l'effet Radar.
    /// </summary>
    public event System.Action<Vector3, GameObject> OnObstacleSpawne;

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

    // ── Compteur Barrage (basé sur patterns, pas sur signaux) ─────────────────
    private int  _patternsSpawnés    = 0;
    private int  _patternsNécessaires = 5;
    private bool _barrageEnAttente   = false;

    // Conservé uniquement pour la compatibilité session (signauxEcoules / prochainBarrageA).
    private int _signauxEcoules   = 0;
    private int _prochainBarrageA = 0;

    /// <summary>
    /// Bloque les mises à jour de vitesse sur les sols et les objets du pool
    /// pendant la séquence de freinage du barrage, pour que la décélération
    /// contrôlée par SegmentBarrage ne soit pas écrasée par OnTimePassed.
    /// </summary>
    private bool _miseAJourVitessePausée = false;

    /// <summary>
    /// Bloque l'incrément de _signauxEcoules pendant la mort du joueur.
    /// Empêche le compteur barrage d'avancer pendant le revive menu.
    /// </summary>
    private bool _compteurBarragePausé = false;

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
        // Le tirage initial est fait ici uniquement si aucune session n'est restaurée.
        // Si une session valide existe, RestaurerProgressionDepuisSession() sera appelé
        // depuis MapRoadSessionBridge.Start() et écrasera ce tirage.
        TirerProchainSeuilBarrage();
        StartSpawning();
    }

    // ── Speed scaling ─────────────────────────────────────────────────────────

    private void OnTimePassed()
    {
        _generalSpeed = Mathf.Clamp(_generalSpeed + 0.3f, 0, 30);

        if (!_miseAJourVitessePausée)
        {
            foreach (List<GameObject> bucket in _pool.Values)
            {
                foreach (GameObject obj in bucket)
                {
                    if (obj != null && obj.TryGetComponent<ScrollingElement>(out var scrolling))
                        scrolling.UpdateSpeed(_generalSpeed);
                }
            }

            for (int i = 0; i < _grounds.Length; i++)
                _grounds[i].UpdateSpeed(_generalSpeed);
        }

        // Le trigger barrage est basé sur le compteur de patterns (IncrémenterCompteurPatterns),
        // pas sur les signaux. On conserve _signauxEcoules uniquement pour la compatibilité session.
        if (!_compteurBarragePausé)
            _signauxEcoules++;
    }

    // ── Compteur patterns (trigger barrage) ───────────────────────────────────

    /// <summary>
    /// Appelé par SpawnPatternCoroutine après chaque pattern normal spawné.
    /// Déclenche le barrage quand le nombre de patterns cible est atteint.
    /// </summary>
    private void IncrémenterCompteurPatterns()
    {
        if (_barrageEnAttente || _compteurBarragePausé) return;

        _patternsSpawnés++;
        Debug.Log($"[SpawnObstacleV2] Pattern #{_patternsSpawnés}/{_patternsNécessaires} spawné.");

        if (_patternsSpawnés >= _patternsNécessaires && patternBarrage != null)
        {
            _barrageEnAttente = true;
            Debug.Log("[SpawnObstacleV2] Seuil de patterns atteint — barrage en attente.");
        }
    }

    // ── Public control ────────────────────────────────────────────────────────

    /// <summary>Gèle le compteur de patterns barrage (mort du joueur).</summary>
    public void PauserCompteurBarrage() => _compteurBarragePausé = true;

    /// <summary>Reprend le compteur de patterns barrage (revive).</summary>
    public void ReprendreCompteurBarrage() => _compteurBarragePausé = false;

    /// <summary>
    /// Force le recalcul des vitesses de tous les objets actifs du pool et des sols
    /// en tenant compte du FacteurVitesseGlobal courant.
    /// A appeler depuis EffetsObjetsSpeciaux apres avoir change ScrollingElement.FacteurVitesseGlobal.
    /// </summary>
    public void RefreshVitesses()
    {
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
            foreach (GoundMouvement g in _grounds)
                if (g != null) g.UpdateSpeed(_generalSpeed);
        }
    }

    /// <summary>Starts the spawn coroutine.</summary>
    public void StartSpawning()
    {
        isSpawning = true;
        _miseAJourVitessePausée = false;

        foreach (List<GameObject> bucket in _pool.Values)
        {
            foreach (GameObject obj in bucket)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                if (!obj.TryGetComponent<ScrollingElement>(out var scrolling)) continue;

                // SegmentBarrage instances are mid-sequence (frozen, awaiting the player).
                // They must not be despawned on revive — SegmentBarrage manages its own lifecycle.
                if (obj.TryGetComponent<SegmentBarrage>(out _)) continue;

                scrolling.StartMoving();
                scrolling.UpdateSpeed(_generalSpeed);
                scrolling.Dispawn();
            }
        }
        _spawningCoroutine ??= StartCoroutine(SpawnRoutine());
    }

    /// <summary>Stops spawning, freeze tous les objets du pool et bloque les
    /// mises à jour de vitesse pour laisser SegmentBarrage gérer la décélération.</summary>
    public void StopSpawning()
    {
        isSpawning = false;
        _miseAJourVitessePausée = true;

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
            if (_barrageEnAttente)
            {
                Debug.Log("[SpawnObstacleV2] Entrée bloc barrage — attente gap avant...");

                // Route vide AVANT le barrage.
                yield return StartCoroutine(WaitForDistance(gapAvantBarrage));

                // NE PAS remettre _signauxEcoules à 0 ici — BarreProgressionBarrage gère son propre cycle.
                // NE PAS appeler TirerProchainSeuilBarrage ici — sera fait à la réinitialisation.
                _barrageEnAttente = false;

                Debug.Log("[SpawnObstacleV2] Spawn du pattern barrage...");

                // Spawn centré + gap vide APRÈS le barrage.
                if (patternBarrage != null)
                    yield return StartCoroutine(SpawnPatternCoroutineBarrage(patternBarrage));

                // Arrêter tout nouveau spawn : le barrage est en route vers le joueur.
                isSpawning = false;
                _spawningCoroutine = null;
                yield break;
            }

            if (patterns == null || patterns.Length == 0)
            {
                Debug.LogWarning("[SpawnObstacleV2] No patterns assigned.");
                yield return null;
                continue;
            }

            ObstaclePattern prochain = patterns[GetNextPatternIndex()];
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
            // Interrompre immédiatement si le barrage est en attente — aucune
            // row supplémentaire ne doit apparaître au-dessus du barrage.
            if (_barrageEnAttente) yield break;

            SpawnRow(pattern.rows[rowIndex]);
            yield return StartCoroutine(WaitForDistance(pattern.rowSpacing));
        }

        // Attendre le gap configuré avant de lancer le pattern suivant.
        yield return StartCoroutine(WaitForDistance(gapBetweenPatterns));

        // Incrémenter le compteur après que le pattern est entièrement sorti.
        IncrémenterCompteurPatterns();
    }

    /// <summary>
    /// Coroutine de spawn dédiée au pattern Barrage : place chaque row centrée
    /// sur la route (position X du spawner) puis attend un gap vide après le
    /// dernier élément avant que les patterns normaux reprennent.
    /// </summary>
    private IEnumerator SpawnPatternCoroutineBarrage(ObstaclePattern pattern)
    {
        for (int rowIndex = 0; rowIndex < pattern.rows.Count; rowIndex++)
        {
            SpawnBarrageRow(pattern.rows[rowIndex]);
            yield return StartCoroutine(WaitForDistance(pattern.rowSpacing));
        }

        // Gap vide après le barrage — laisse la route dégagée à la reprise.
        yield return StartCoroutine(WaitForDistance(gapAprèsBarrage));
    }

    /// <summary>
    /// Variante de SpawnRow pour le barrage : prend le premier prefab non-null
    /// de la row et le place centré sur la route (position X du spawner),
    /// indépendamment du système de lanes.
    /// </summary>
    private void SpawnBarrageRow(PatternRow row)
    {
        if (row == null) return;

        GameObject prefab = null;
        foreach (var lane in row.lanes)
        {
            if (lane != null) { prefab = lane; break; }
        }
        if (prefab == null)
        {
            Debug.LogWarning("[SpawnObstacleV2] SpawnBarrageRow : aucun prefab trouvé dans la row.");
            return;
        }

        GameObject obj = GetFromPool(prefab);

        float centreX = (_fallingLines != null && _fallingLines.Length > 1
            ? _fallingLines[1].transform.position.x
            : transform.position.x) + barrageOffsetX;

        Vector3 spawnPos = new Vector3(centreX, transform.position.y, transform.position.z);

        obj.transform.SetPositionAndRotation(spawnPos, prefab.transform.rotation);
        obj.SetActive(true);

        // UpdateSpeed appelé après SetActive : sur un objet nouvellement instancié,
        // ScrollingElement.Start() n'a pas encore tourné, donc UpdateSpeed sera lu
        // correctement par Update(). Sur un objet recyclé, Start() ne re-tournera pas.
        if (obj.TryGetComponent<ScrollingElement>(out var scrolling))
            scrolling.UpdateSpeed(_generalSpeed);

        OnObstacleSpawne?.Invoke(spawnPos, obj);

        Debug.Log($"[SpawnObstacleV2] SegmentBarrage spawné à {spawnPos} | vitesse générale={_generalSpeed}");
    }

    /// <summary>
    /// Waits until the accumulated virtual distance (speed × fixedDeltaTime)
    /// reaches <paramref name="distance"/>. Reads the live ground speed each
    /// physics tick to stay in sync with the actual scroll rate.
    /// </summary>
    private IEnumerator WaitForDistance(float distance)
    {
        float travelled = 0f;
        while (travelled < distance)
        {
            float currentSpeed = (_grounds != null && _grounds.Length > 0)
                ? _grounds[0].speed
                : baseObstacleSpeed + _generalSpeed;
            travelled += currentSpeed * Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
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

                // Si ce véhicule est un Good (porte un ChangeSkin), forcer le skin via le budget.
                var changeSkin = obj.GetComponentInChildren<ChangeSkin>();
                if (changeSkin != null && spawnBudget != null && !spawnBudget.BudgetÉpuisé)
                {
                    var type = spawnBudget.ConsumeNext();
                    if (type.HasValue)
                    {
                        changeSkin.forcedSkinIndex = -1; // reset avant forçage
                        changeSkin.ForceSkinParType(type.Value);
                        Debug.Log($"[SpawnObstacleV2] Good vehicle forcé → {type.Value} (budget restant : {spawnBudget.VéhiculesRestants})");
                    }
                }

                OnObstacleSpawne?.Invoke(spawnPos, obj);
            }
        }
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sauvegarde la progression barrage pour le prochain retour sur MapRoad.
    /// Le compteur de signaux est intentionnellement remis à 0 et un nouveau seuil
    /// est tiré ici : au retour de barrage, le joueur repart toujours d'un cycle neuf
    /// (barrageSignauxMin–barrageSignauxMax signaux avant le prochain barrage), et non pas
    /// du seuil déjà atteint qui déclencherait un barrage immédiat dès le premier signal.
    /// </summary>
    public void SauvegarderDansSession(DonnéesSession donnees)
    {
        donnees.vitesseGénérale = _generalSpeed;

        // Réinitialiser le compteur de patterns pour le prochain cycle MapRoad.
        // patternsNécessaires sera recalculé à la fin du prochain barrage par FormulaireSpawnBudget.
        donnees.signauxEcoules   = 0;
        donnees.prochainBarrageA = 0;

        Debug.Log($"[SpawnObstacleV2] Session sauvegardée — patterns={_patternsSpawnés}/{_patternsNécessaires}");
    }

    /// <summary>Restaure la vitesse générale depuis les données de session.</summary>
    public void RestaurerDepuisSession(float vitesse)
    {
        _generalSpeed = vitesse;
    }

    /// <summary>
    /// Restaure la progression barrage depuis les données de session.
    /// Lit <see cref="DonnéesSession.patternsNécessaires"/> calculé par FormulaireSpawnBudget
    /// à la fin du dernier barrage.
    /// </summary>
    public void RestaurerProgressionDepuisSession(DonnéesSession donnees)
    {
        _patternsSpawnés     = 0;
        _patternsNécessaires = donnees.patternsNécessaires > 0
            ? donnees.patternsNécessaires
            : UnityEngine.Random.Range(patternsMinFallback, patternsMaxFallback + 1);
        _barrageEnAttente    = false;
        _compteurBarragePausé = false;

        Debug.Log($"[SpawnObstacleV2] Progression restaurée : 0/{_patternsNécessaires} patterns avant barrage.");
    }

    /// <summary>
    /// Réinitialise complètement le compteur pour une nouvelle partie.
    /// </summary>
    public void RéinitialiserProgressionBarrage()
    {
        _patternsSpawnés  = 0;
        _barrageEnAttente = false;
        TirerProchainSeuilBarrage();
    }

    // ── Progression barrage (0 → 1) ───────────────────────────────────────────

    /// <summary>
    /// Progression discrète (0 → 1) basée sur le nombre de patterns spawned.
    /// Retourne 1 dès que le seuil est atteint.
    /// </summary>
    public float Progression =>
        (_patternsNécessaires > 0
            ? Mathf.Clamp01((float)_patternsSpawnés / _patternsNécessaires)
            : 0f);

    /// <summary>Durée en secondes d'un signal TimeManager — utilisée pour lisser la progression.</summary>
    public float DuréeSignal => _timeManager != null ? _timeManager._timeStepDuration : 1.5f;

    /// <summary>Vrai si le barrage est en attente de spawn (progression = 1 verrouillée).</summary>
    public bool BarrageEnAttente => _barrageEnAttente;

    /// <summary>Seuil total de patterns pour ce cycle.</summary>
    public int SeuilBarrage => _patternsNécessaires;

    /// <summary>
    /// Durée en secondes du gap vide avant le barrage, convertie depuis la distance monde.
    /// </summary>
    public float DuréeGapAvantBarrage =>
        (baseObstacleSpeed > 0f) ? gapAvantBarrage / baseObstacleSpeed : 0f;

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

    /// <summary>
    /// Tirage de secours utilisé uniquement quand aucune session valide n'est disponible
    /// (nouvelle partie, premier barrage, game over).
    /// </summary>
    private void TirerProchainSeuilBarrage()
    {
        _patternsSpawnés     = 0;
        _patternsNécessaires = UnityEngine.Random.Range(patternsMinFallback, patternsMaxFallback + 1);
        Debug.Log($"[SpawnObstacleV2] Fallback — seuil tiré : {_patternsNécessaires} patterns.");
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

