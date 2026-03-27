using System.Collections;
using UnityEngine;

/// <summary>
/// Déclenche la transition vers la scène Barrage dès que le joueur entre
/// dans les bounds world-space du BoxCollider2D du prefab.
/// Détection via Update() + Bounds.Contains — aucune dépendance à la physique 2D.
/// </summary>
[RequireComponent(typeof(ScrollingElement))]
[RequireComponent(typeof(BoxCollider2D))]
public class SegmentBarrage : MonoBehaviour
{
    private PlayerMovement   _joueur;
    private ScoreManager     _scoreManager;
    private SpawnObstacleV2  _spawner;
    private GoundMouvement[] _sols;
    private BoxCollider2D    _collider;

    private static bool _barrageEnCours = false;


    private void Awake()
    {
        _collider           = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
    }

    private void OnEnable()
    {
        _barrageEnCours = false;

        _joueur       = FindFirstObjectByType<PlayerMovement>();
        _scoreManager = FindFirstObjectByType<ScoreManager>();
        _spawner      = FindFirstObjectByType<SpawnObstacleV2>();
        _sols         = FindObjectsByType<GoundMouvement>(FindObjectsSortMode.None);

        Debug.Log($"[SegmentBarrage] Activé. Joueur={_joueur != null} " +
                  $"| SessionManager={SessionManager.Instance != null}");

        if (_joueur == null)       Debug.LogError("[SegmentBarrage] PlayerMovement introuvable.");
        if (_scoreManager == null) Debug.LogError("[SegmentBarrage] ScoreManager introuvable.");
        if (_spawner == null)      Debug.LogError("[SegmentBarrage] SpawnObstacleV2 introuvable.");
    }

    private void OnDisable() => _barrageEnCours = false;

    private void Update()
    {
        if (_barrageEnCours || _joueur == null || _collider == null) return;

        // Vérification directe bounds world-space — fiable avec transform.Translate.
        if (!_collider.bounds.Contains(_joueur.transform.position)) return;

        _barrageEnCours = true;
        Debug.Log("[SegmentBarrage] Joueur détecté dans la box → chargement scène Barrage.");
        DeclencherTransition();
    }

    /// <summary>Arrête le spawn et charge immédiatement la scène Barrage.</summary>
    private void DeclencherTransition()
    {
        float vitesseInitiale = _sols != null && _sols.Length > 0 ? _sols[0].speed : 0f;

        _spawner?.StopSpawning();

        StartCoroutine(StopBeforeBarrage(vitesseInitiale));

        /*
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.AllerAuBarrage(
                _joueur, _scoreManager, _spawner, _sols,
                vitesseSolAvantRalentissement: vitesseInitiale);
        }
        else
        {
            Debug.LogError("[SegmentBarrage] SessionManager introuvable — transition annulée.");
        }
        */
    }

    IEnumerator StopBeforeBarrage(float vitesseInitiale)
    {
        float duration = 2f;
        _joueur.MoveUp(duration);
        foreach (GoundMouvement moves in _sols)
        {
            moves.Ralentissement(duration, GetComponent<ScrollingElement>());
        }

        yield return new WaitForSeconds(duration+0.2f);

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.AllerAuBarrage(
                _joueur, _scoreManager, _spawner, _sols,
                vitesseSolAvantRalentissement: vitesseInitiale);
        }
        else
        {
            Debug.LogError("[SegmentBarrage] SessionManager introuvable — transition annulée.");
        }
    }
}
