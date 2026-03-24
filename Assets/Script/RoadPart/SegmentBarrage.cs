using System.Collections;
using UnityEngine;

/// <summary>
/// À placer sur le prefab du segment de barrage (ligne de la grille ObstaclePattern).
/// Quand le joueur entre dans le trigger :
///   1. Ralentit progressivement le sol et les obstacles jusqu'à zéro.
///   2. Sauvegarde l'état complet dans DonnéesSession via SessionManager.
///   3. Charge la scène Barrage.
/// </summary>
[RequireComponent(typeof(Collider2D))]
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

    // Empêche un double déclenchement.
    private bool _déclenché = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        // Résolution automatique des dépendances de scène au moment du spawn.
        _joueur       = FindFirstObjectByType<PlayerMovement>();
        _scoreManager = FindFirstObjectByType<ScoreManager>();
        _spawner      = FindFirstObjectByType<SpawnObstacleV2>();
        _sols         = FindObjectsByType<GoundMouvement>(FindObjectsSortMode.None);

        if (_joueur == null)       Debug.LogError("[SegmentBarrage] PlayerMovement introuvable.");
        if (_scoreManager == null) Debug.LogError("[SegmentBarrage] ScoreManager introuvable.");
        if (_spawner == null)      Debug.LogError("[SegmentBarrage] SpawnObstacleV2 introuvable.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_déclenché) return;
        if (!other.TryGetComponent<PlayerMovement>(out _)) return;

        _déclenché = true;
        StartCoroutine(SéquenceBarrage());
    }

    private IEnumerator SéquenceBarrage()
    {
        // Stopper le spawn de nouveaux patterns pendant le freinage.
        _spawner?.StopSpawning();

        // Capturer la vitesse AVANT le freinage pour la restaurer au retour dans MapRoad.
        float vitesseSolAvantFreinage = _sols != null && _sols.Length > 0 ? _sols[0].speed : 0f;

        // Ralentir progressivement le sol vers 0.
        float temps = 0f;
        while (temps < duréeRalentissement)
        {
            temps += Time.deltaTime;
            float t = Mathf.SmoothStep(1f, 0f, temps / duréeRalentissement);

            if (_sols != null)
                foreach (var sol in _sols)
                    sol.speed = vitesseSolAvantFreinage * t;

            yield return null;
        }

        // Arrêt complet du sol.
        if (_sols != null)
            foreach (var sol in _sols)
                sol.StopMove();

        // Sauvegarder et transitionner vers Barrage.
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
