using System.Collections;
using UnityEngine;

/// <summary>
/// Déclenche la séquence de transition vers la scène Barrage quand le joueur
/// entre dans le trigger. Les dimensions (Transform.localScale) et le sorting
/// layer (SpriteRenderer) se configurent directement dans l'Inspector du prefab.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ScrollingElement))]
public class SegmentBarrage : MonoBehaviour
{
    [Header("Ralentissement")]
    [Tooltip("Durée en secondes pour ramener la vitesse à zéro.")]
    [SerializeField] private float duréeRalentissement = 1.5f;

    // Références résolues automatiquement à l'activation.
    private PlayerMovement   _joueur;
    private ScoreManager     _scoreManager;
    private SpawnObstacleV2  _spawner;
    private GoundMouvement[] _sols;

    // Empêche un double déclenchement entre plusieurs instances de la même vague.
    private static bool _barrageEnCours = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        _barrageEnCours = false;

        _joueur       = FindFirstObjectByType<PlayerMovement>();
        _scoreManager = FindFirstObjectByType<ScoreManager>();
        _spawner      = FindFirstObjectByType<SpawnObstacleV2>();
        _sols         = FindObjectsByType<GoundMouvement>(FindObjectsSortMode.None);

        if (_joueur == null)       Debug.LogError("[SegmentBarrage] PlayerMovement introuvable.");
        if (_scoreManager == null) Debug.LogError("[SegmentBarrage] ScoreManager introuvable.");
        if (_spawner == null)      Debug.LogError("[SegmentBarrage] SpawnObstacleV2 introuvable.");
    }

    private void OnDisable()
    {
        _barrageEnCours = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_barrageEnCours) return;
        if (!other.TryGetComponent<PlayerMovement>(out _)) return;

        _barrageEnCours = true;
        StartCoroutine(SéquenceBarrage());
    }

    private IEnumerator SéquenceBarrage()
    {
        _spawner?.StopSpawning();

        var scrolling = GetComponent<ScrollingElement>();
        float vitesseSolAvantFreinage = _sols != null && _sols.Length > 0 ? _sols[0].speed : 0f;
        float vitesseBarrageAvantFreinage = scrolling != null ? scrolling.baseSpeed + vitesseSolAvantFreinage : vitesseSolAvantFreinage;

        float temps = 0f;
        while (temps < duréeRalentissement)
        {
            temps += Time.deltaTime;
            float t = Mathf.SmoothStep(1f, 0f, temps / duréeRalentissement);

            // Ralentir le sol.
            if (_sols != null)
                foreach (var sol in _sols)
                    sol.speed = vitesseSolAvantFreinage * t;

            // Ralentir le barrage au même rythme pour qu'il s'arrête devant le joueur.
            if (scrolling != null)
                scrolling.UpdateSpeed(vitesseSolAvantFreinage * t - scrolling.baseSpeed);

            yield return null;
        }

        // Arrêt complet.
        if (_sols != null)
            foreach (var sol in _sols)
                sol.StopMove();

        scrolling?.StopMoving();

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.AllerAuBarrage(
                _joueur, _scoreManager, _spawner, _sols,
                vitesseSolAvantRalentissement: vitesseSolAvantFreinage);
        }
        else
        {
            Debug.LogError("[SegmentBarrage] SessionManager introuvable — transition annulée.");
        }
    }
}
