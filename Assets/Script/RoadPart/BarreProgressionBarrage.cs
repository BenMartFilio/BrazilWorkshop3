using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pilote un Slider Unity (vertical, BottomToTop) représentant la progression
/// vers le prochain barrage.
///
/// Phase normale : la barre avance à vitesse autonome constante et est attirée
/// vers SpawnObstacleV2.Progression quand elle est en retard.
///
/// Phase barrage : dès que BarrageEnAttente est vrai, la barre décélère via
/// SmoothStep pour atteindre 1.0 exactement après délaiArrivéeBarrage secondes,
/// synchronisé avec l'arrivée visuelle du barrage à l'écran.
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

    [Header("Phase normale — progression autonome")]
    [Tooltip("Vitesse à laquelle la barre progresse seule, indépendamment des patterns (unités/seconde, 0-1). " +
             "Calibrez pour que la barre soit entre 0.5 et 0.9 au moment où le barrage spawne.")]
    [SerializeField] private float vitesseAutonome = 0.008f;

    [Tooltip("Force d'attraction vers la progression réelle du joueur quand la barre est en retard. " +
             "0 = ignore complètement le spawner, valeurs hautes = rattrapage plus agressif.")]
    [SerializeField, Min(0f)] private float forceAttraction = 2f;

    [Header("Phase barrage — arrivée synchronisée")]
    [Tooltip("Durée en secondes depuis BarrageEnAttente jusqu'à l'arrivée visuelle du barrage. " +
             "La barre décélère (SmoothStep) pour atteindre exactement 1.0 à ce moment. " +
             "Ajustez jusqu'à ce que la barre remplisse pile quand le barrage est à l'écran.")]
    [SerializeField] private float délaiArrivéeBarrage = 3f;

    [Tooltip("Seuil (0-1) à partir duquel la barre se gèle définitivement à 1.")]
    [SerializeField] private float seuilGel = 0.999f;

    // ── État interne ──────────────────────────────────────────────────────────
    private float _valeurAffichée        = 0f;
    private bool  _gelé                  = false;
    private bool  _pausé                 = false;

    // Phase barrage
    private bool  _phaseBarrage          = false;
    private float _valeurAuDéclenchement = 0f;
    private float _tempsPhaseBarrage     = 0f;

    private void Start()
    {
        if (_slider == null) return;

        _slider.direction    = Slider.Direction.BottomToTop;
        _slider.minValue     = 0f;
        _slider.maxValue     = 1f;
        _slider.wholeNumbers = false;
        _slider.interactable = false;
        _slider.value        = 0f;
    }

    private void Update()
    {
        if (_spawner == null || _slider == null) return;

        if (_pausé || _gelé)
        {
            TournerRoue(_valeurAffichée);
            return;
        }

        // ── Phase barrage : SmoothStep vers 1.0 ─────────────────────────────
        if (_spawner.BarrageEnAttente)
        {
            if (!_phaseBarrage)
            {
                _phaseBarrage          = true;
                _valeurAuDéclenchement = _valeurAffichée;
                _tempsPhaseBarrage     = 0f;
            }

            _tempsPhaseBarrage += Time.deltaTime;
            float t = Mathf.Clamp01(_tempsPhaseBarrage / délaiArrivéeBarrage);
            _valeurAffichée = Mathf.Lerp(_valeurAuDéclenchement, 1f, Mathf.SmoothStep(0f, 1f, t));
            _slider.value   = _valeurAffichée;

            if (_valeurAffichée >= seuilGel)
            {
                _valeurAffichée = 1f;
                _slider.value   = 1f;
                _gelé           = true;
            }

            TournerRoue(_valeurAffichée);
            return;
        }

        // ── Phase normale ─────────────────────────────────────────────────────

        // 1. Avance autonome constante
        _valeurAffichée += vitesseAutonome * Time.deltaTime;

        // 2. Attraction vers la progression réelle quand on est en retard
        float progressionRéelle = _spawner.Progression;
        if (_valeurAffichée < progressionRéelle)
            _valeurAffichée = Mathf.Lerp(_valeurAffichée, progressionRéelle, Time.deltaTime * forceAttraction);

        _valeurAffichée = Mathf.Clamp01(_valeurAffichée);
        _slider.value   = _valeurAffichée;
        TournerRoue(_valeurAffichée);
    }

    /// <summary>
    /// Réinitialise la barre à 0 pour un nouveau cycle (retour de barrage réussi).
    /// </summary>
    public void RéinitialiserPourNouveauCycle()
    {
        _valeurAffichée        = 0f;
        _phaseBarrage          = false;
        _valeurAuDéclenchement = 0f;
        _tempsPhaseBarrage     = 0f;
        _gelé                  = false;
        _pausé                 = false;

        if (_slider != null)
            _slider.value = 0f;
    }

    /// <summary>Stoppe la progression (mort du joueur).</summary>
    public void Geler() => _pausé = true;

    /// <summary>Reprend la progression (revive).</summary>
    public void Dégeler()
    {
        // Si le barrage était déjà en route avant la mort, le SmoothStep
        // repart depuis _valeurAffichée figée sans sauter.
        _pausé = false;
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private void TournerRoue(float progression)
    {
        if (_handleRoue == null) return;

        float facteur = 0.5f + progression * 0.5f;
        _handleRoue.Rotate(Vector3.forward, -vitesseRotation * facteur * Time.deltaTime, Space.Self);
    }
}
