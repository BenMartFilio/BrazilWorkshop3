// SegmentBarrage.cs — supprime Find dans OnEnable, arrête Update après déclenchement
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ScrollingElement))]
[RequireComponent(typeof(BoxCollider2D))]
public class SegmentBarrage : MonoBehaviour
{
    [SerializeField] private float positionYGel = -2f;

    private PlayerMovement _joueur;
    private ScoreManager _scoreManager;
    private SpawnObstacleV2 _spawner;
    private EndManager _end;
    private GoundMouvement[] _sols;
    private BoxCollider2D _collider;
    private ScrollingElement _scrolling;

    private bool _barrageEnCours = false;
    private bool _gelé = false;

    // Références injectées depuis l'extérieur (par le spawner ou la scène)
    // pour éviter tout Find au runtime
    public void Init(PlayerMovement joueur, ScoreManager score,
                     SpawnObstacleV2 spawner, EndManager end,
                     GoundMouvement[] sols)
    {
        _joueur = joueur;
        _scoreManager = score;
        _spawner = spawner;
        _end = end;
        _sols = sols;
    }

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _scrolling = GetComponent<ScrollingElement>();
    }

    private void OnEnable()
    {
        _barrageEnCours = false;
        _gelé = false;

        // Fallback uniquement si Init() n'a pas été appelé
        // (ex : premier spawn avant que le spawner soit prêt)
        if (_joueur == null)
        {
            _joueur = FindFirstObjectByType<PlayerMovement>();
            _scoreManager = FindFirstObjectByType<ScoreManager>();
            _spawner = FindFirstObjectByType<SpawnObstacleV2>();
            _end = FindFirstObjectByType<EndManager>();
            _sols = FindObjectsByType<GoundMouvement>(FindObjectsSortMode.None);
            Debug.LogWarning("[SegmentBarrage] Init() non appelé — fallback Find utilisé.");
        }
    }

    private void Update()
    {
        // Court-circuit immédiat si déjà déclenché — plus rien à faire
        if (_barrageEnCours) return;

        if (!_gelé && transform.position.y <= positionYGel)
        {
            _gelé = true;
            _scrolling?.StopMoving();
        }

        if (_joueur == null || _collider == null) return;
        if (!_collider.bounds.Contains(_joueur.transform.position)) return;

        _barrageEnCours = true;
        DeclencherTransition();
    }

    private void DeclencherTransition()
    {
        float vitesseInitiale = _sols != null && _sols.Length > 0 ? _sols[0].speed : 0f;
        _scrolling?.StopMoving();
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