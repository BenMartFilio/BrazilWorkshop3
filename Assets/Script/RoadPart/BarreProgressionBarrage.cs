using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pilote un Slider Unity (vertical, BottomToTop) représentant la progression
/// vers le prochain barrage.
///
/// Stratégie de lissage :
///   - On connaît la durée totale du cycle = SeuilBarrage × DuréeSignal.
///   - On mesure le temps écoulé depuis le début du cycle.
///   - La valeur affichée = Lerp(0, 1, tempsEcoulé / duréeTotale).
///   - C'est un mouvement strictement continu, sans saut ni palier.
///
/// Gel : dès que BarrageEnAttente est vrai ET que la valeur affichée
///   a atteint 1, elle y reste jusqu'à RéinitialiserPourNouveauCycle().
///
/// La roue ne peut jamais reculer — _valeurAffichée est toujours ≥ à la frame précédente.
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
    [Tooltip("Durée en secondes entre le spawn du barrage et son arrivée visuelle à l'écran. " +
             "Ajoutée à la durée totale du cycle pour que la roue atteigne le haut " +
             "exactement quand le barrage devient visible.")]
    [SerializeField] private float délaiArrivéeBarrage = 2f;

    // ── État interne ──────────────────────────────────────────────────────────
    private float _valeurAffichée = 0f;     // valeur courante [0-1], ne recule jamais
    private float _tempsEcoulé   = 0f;      // temps écoulé dans le cycle courant
    private float _duréeTotale   = 0f;      // duréeSignal × seuilBarrage
    private bool  _gelé          = false;   // true quand la barre est bloquée à 1
    private bool  _pausé         = false;   // true quand la mort stoppe la progression

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

        // Mort ou barre gelée en haut : juste tourner la roue
        if (_pausé || _gelé)
        {
            TournerRoue(_valeurAffichée);
            return;
        }

        // Recalculer la durée totale si le seuil a changé (1er frame après Start)
        if (_duréeTotale <= 0f)
            InitialiserCycle();

        // Avancer le temps
        _tempsEcoulé += Time.deltaTime;

        // Valeur cible basée sur le temps réel écoulé vs durée totale attendue
        float cible = (_duréeTotale > 0f)
            ? Mathf.Clamp01(_tempsEcoulé / _duréeTotale)
            : 0f;

        // La valeur ne peut jamais reculer
        _valeurAffichée = Mathf.Max(_valeurAffichée, cible);
        _slider.value   = _valeurAffichée;

        // Gel automatique dès que le barrage est en attente ET qu'on est au maximum
        if (_spawner.BarrageEnAttente)
        {
            _valeurAffichée = 1f;
            _slider.value   = 1f;
            _gelé           = true;
        }

        TournerRoue(_valeurAffichée);
    }

    /// <summary>
    /// Réinitialise la barre à 0 pour un nouveau cycle barrage.
    /// À appeler depuis MapRoadSessionBridge après un retour de barrage réussi.
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

    /// <summary>Stoppe la progression de la barre (mort du joueur).</summary>
    public void Geler() => _pausé = true;

    /// <summary>Reprend la progression de la barre (revive).</summary>
    public void Dégeler() => _pausé = false;

    // ── Utilitaires ───────────────────────────────────────────────────────────

    /// <summary>Calcule la durée totale du cycle courant depuis le spawner.</summary>
    private void InitialiserCycle()
    {
        if (_spawner == null) return;

        float duréeSignal = Mathf.Max(0.1f, _spawner.DuréeSignal);
        int   seuil       = Mathf.Max(1, _spawner.SeuilBarrage);

        // On ajoute le délai visuel d'arrivée du barrage : la roue doit atteindre
        // le haut de la barre exactement quand le barrage devient visible, pas quand il spawne.
        _duréeTotale = duréeSignal * seuil + délaiArrivéeBarrage;
    }

    /// <summary>Tourne la roue proportionnellement à la progression.</summary>
    private void TournerRoue(float progression)
    {
        if (_handleRoue == null) return;

        float facteur = 0.5f + progression * 0.5f;
        _handleRoue.Rotate(Vector3.forward, -vitesseRotation * facteur * Time.deltaTime, Space.Self);
    }
}
