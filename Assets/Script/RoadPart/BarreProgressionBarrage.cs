using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pilote un Slider Unity (vertical, BottomToTop) représentant la progression
/// vers le prochain barrage.
///
/// Timing : duréeTotale = (DuréeSignal × SeuilBarrage) + DuréeGapAvantBarrage + délaiArrivéeBarrage
///   → la roue atteint 1 exactement quand le barrage arrive visuellement à l'écran.
///
/// Mort / Revive : Geler() fige _tempsEcoulé. Dégeler() recalcule _tempsEcoulé
///   depuis _valeurAffichée pour repartir du bon point même si la durée totale
///   a légèrement dérivé.
/// </summary>
public class BarreProgressionBarrage : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private SpawnObstacleV2 _spawner;
    [SerializeField] private Slider          _slider;
    [SerializeField] private RectTransform   _handleRoue;

    [Header("Rotation handle")]
    [Tooltip("Degrés par seconde de rotation de la roue (vitesse de base).")]
    [SerializeField] private float vitesseRotation = 120f;

    [Header("Correction timing")]
    [Tooltip("Durée en secondes entre l'arrivée du barrage à l'écran et le moment où il a spawné. " +
             "Ajustez jusqu'à ce que la roue atteigne le haut exactement quand le barrage est visible.")]
    [SerializeField] private float délaiArrivéeBarrage = 2f;

    // ── État interne ──────────────────────────────────────────────────────────
    private float _valeurAffichée = 0f;
    private float _tempsEcoulé   = 0f;
    private float _duréeTotale   = 0f;
    private bool  _gelé          = false;   // bloqué à 1 quand barrage est en attente
    private bool  _pausé         = false;   // mort du joueur

    private void Start()
    {
        if (_slider == null) return;

        _slider.direction    = Slider.Direction.BottomToTop;
        _slider.minValue     = 0f;
        _slider.maxValue     = 1f;
        _slider.wholeNumbers = false;
        _slider.interactable = false;
        _slider.value        = 0f;

        InitialiserCycle();
    }

    private void Update()
    {
        if (_spawner == null || _slider == null) return;

        if (_pausé || _gelé)
        {
            TournerRoue(_valeurAffichée);
            return;
        }

        if (_duréeTotale <= 0f)
            InitialiserCycle();

        _tempsEcoulé    += Time.deltaTime;
        float cible      = Mathf.Clamp01(_tempsEcoulé / _duréeTotale);
        _valeurAffichée  = Mathf.Max(_valeurAffichée, cible);
        _slider.value    = _valeurAffichée;

        // Gel dès que le barrage est en attente
        if (_spawner.BarrageEnAttente)
        {
            _valeurAffichée = 1f;
            _slider.value   = 1f;
            _gelé           = true;
        }

        TournerRoue(_valeurAffichée);
    }

    /// <summary>
    /// Réinitialise la barre à 0 pour un nouveau cycle (retour de barrage réussi).
    /// </summary>
    public void RéinitialiserPourNouveauCycle()
    {
        _valeurAffichée = 0f;
        _tempsEcoulé    = 0f;
        _gelé           = false;
        _pausé          = false;

        if (_slider != null)
            _slider.value = 0f;

        InitialiserCycle();
    }

    /// <summary>Stoppe la progression (mort du joueur).</summary>
    public void Geler() => _pausé = true;

    /// <summary>
    /// Reprend la progression (revive).
    /// Recalcule _tempsEcoulé depuis _valeurAffichée pour que la distance
    /// restante corresponde exactement à ce qu'il restait avant la mort.
    /// </summary>
    public void Dégeler()
    {
        // On repositionne le curseur de temps au bon endroit dans la durée totale
        // en partant de la valeur affichée figée, peu importe le temps réel écoulé.
        if (_duréeTotale > 0f)
            _tempsEcoulé = _valeurAffichée * _duréeTotale;

        _pausé = false;
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    /// <summary>
    /// Durée totale du cycle = signaux × durée/signal + gap avant barrage + délai visuel.
    /// </summary>
    private void InitialiserCycle()
    {
        if (_spawner == null) return;

        float duréeSignal    = Mathf.Max(0.1f, _spawner.DuréeSignal);
        int   seuil          = Mathf.Max(1, _spawner.SeuilBarrage);
        float duréeGap       = _spawner.DuréeGapAvantBarrage;

        _duréeTotale = duréeSignal * seuil + duréeGap + délaiArrivéeBarrage;
    }

    private void TournerRoue(float progression)
    {
        if (_handleRoue == null) return;

        float facteur = 0.5f + progression * 0.5f;
        _handleRoue.Rotate(Vector3.forward, -vitesseRotation * facteur * Time.deltaTime, Space.Self);
    }
}
