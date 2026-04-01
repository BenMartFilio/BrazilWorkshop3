using System.Collections;
using UnityEngine;

/// <summary>
/// Déclenche la transition vers la scène Barrage dès que le joueur entre
/// dans les bounds world-space du BoxCollider2D du prefab.
///
/// Stratégie de position :
///   Le segment descend normalement avec les autres obstacles.
///   Lorsqu'il approche de la position Y du joueur, il se gèle
///   (StopMoving) pour attendre le contact — il ne passera jamais
///   sous le seuil de despawn (-20) et ne sera pas recyclé par le pool.
///   Dès que le joueur entre dans la box, la transition démarre.
///
/// Robustesse :
///   - _barrageEnCours est d'instance (pas static) — isolé par prefab.
///   - StopMoving() appelé avant StopSpawning() pour que le spawner
///     ne remette pas le segment en mouvement lors de son itération.
/// </summary>
[RequireComponent(typeof(ScrollingElement))]
[RequireComponent(typeof(BoxCollider2D))]
public class SegmentBarrage : MonoBehaviour
{
    [Tooltip("Position Y à laquelle le segment se fige pour attendre le joueur. " +
             "Doit être au-dessus du seuil de despawn (-20) et au niveau du joueur (~0).")]
    [SerializeField] private float positionYGel = -2f;

    private PlayerMovement   _joueur;
    private ScoreManager     _scoreManager;
    private SpawnObstacleV2  _spawner;
    private EndManager       _end;
    private GoundMouvement[] _sols;
    private BoxCollider2D    _collider;
    private ScrollingElement _scrolling;

    private bool _barrageEnCours = false;
    private bool _gelé           = false;

    private void Awake()
    {
        _collider           = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _scrolling          = GetComponent<ScrollingElement>();

        // Cache scene references once — these objects persist for the session lifetime.
        _joueur       = FindFirstObjectByType<PlayerMovement>();
        _scoreManager = FindFirstObjectByType<ScoreManager>();
        _spawner      = FindFirstObjectByType<SpawnObstacleV2>();
        _end          = FindFirstObjectByType<EndManager>();
        _sols         = FindObjectsByType<GoundMouvement>(FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        _barrageEnCours = false;
        _gelé           = false;

        // Safety re-fetch if a reference was lost (e.g., scene reload edge case).
        if (_joueur == null)       _joueur       = FindFirstObjectByType<PlayerMovement>();
        if (_scoreManager == null) _scoreManager = FindFirstObjectByType<ScoreManager>();
        if (_spawner == null)      _spawner      = FindFirstObjectByType<SpawnObstacleV2>();
        if (_end == null)          _end          = FindFirstObjectByType<EndManager>();
        if (_sols == null || _sols.Length == 0)
            _sols = FindObjectsByType<GoundMouvement>(FindObjectsSortMode.None);

        Debug.Log($"[SegmentBarrage] Activé. Joueur={_joueur != null} | SessionManager={SessionManager.Instance != null}");
        if (_joueur == null)       Debug.LogError("[SegmentBarrage] PlayerMovement introuvable.");
        if (_scoreManager == null) Debug.LogError("[SegmentBarrage] ScoreManager introuvable.");
        if (_spawner == null)      Debug.LogError("[SegmentBarrage] SpawnObstacleV2 introuvable.");
    }

    private void Update()
    {
        // ── Gel préventif avant le seuil de despawn ───────────────────────────
        // Le segment se fige dès qu'il atteint positionYGel pour attendre le joueur.
        // Cela empêche ScrollingElement de le faire passer sous y=-20 et de le désactiver.
        if (!_gelé && transform.position.y <= positionYGel)
        {
            _gelé = true;
            _scrolling?.StopMoving();
            Debug.Log("[SegmentBarrage] Gelé en position Y d'attente.");
        }

        // ── Détection du joueur ───────────────────────────────────────────────
        if (_barrageEnCours || _joueur == null || _collider == null) return;
        if (!_collider.bounds.Contains(_joueur.transform.position)) return;

        _barrageEnCours = true;
        Debug.Log("[SegmentBarrage] Joueur détecté dans la box → transition barrage.");
        DeclencherTransition();
    }

    /// <summary>
    /// Arrête le spawn global puis lance la séquence d'entrée au barrage.
    /// Le StopMoving est déjà fait par le gel préventif ou est rappelé ici en sécurité.
    /// </summary>
    private void DeclencherTransition()
    {
        // Capturer la vitesse AVANT tout ralentissement.
        float vitesseInitiale = _sols != null && _sols.Length > 0 ? _sols[0].speed : 0f;

        // S'assurer que le scrolling est bien stoppé (cas où le joueur entre
        // exactement au moment du spawn, avant que le gel préventif s'active).
        _scrolling?.StopMoving();

        // Arrêter le spawn.
        _spawner?.StopSpawning();

        StartCoroutine(StopBeforeBarrage(vitesseInitiale));
    }

    private IEnumerator StopBeforeBarrage(float vitesseInitiale)
    {
        const float durée = 2f;

        _joueur.MoveUp(durée);
        if (_sols != null)
            foreach (GoundMouvement sol in _sols)
                sol.Ralentissement(durée, _scrolling);

        yield return new WaitForSeconds(durée + 0.2f);

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.AllerAuBarrage(
                _joueur, _scoreManager, _spawner, _end, _sols,
                vitesseSolAvantRalentissement: vitesseInitiale);
        }
        else
        {
            Debug.LogError("[SegmentBarrage] SessionManager introuvable — transition annulée.");
        }
    }
}
