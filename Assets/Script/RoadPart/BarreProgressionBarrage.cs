using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barre de progression affichant la distance avant le prochain barrage.
/// Le marqueur (roue 10×10px) monte du bas du rail vers le haut au rythme
/// du décompte de signaux TimeManager géré par SpawnObstacleV2.
/// La roue est pivotée à 90° Z pour être verticale et tourne en continu.
/// </summary>
public class BarreProgressionBarrage : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private SpawnObstacleV2 _spawner;
    [SerializeField] private RectTransform   _marqueurRoue;
    [SerializeField] private RectTransform   _railRect;

    [Header("Rotation roue")]
    [Tooltip("Degrés par seconde de rotation de la roue.")]
    [SerializeField] private float vitesseRotation = 180f;

    // ── Taille fixe du marqueur ───────────────────────────────────────────────
    private const float TAILLE_MARQUEUR = 10f;

    // ── Bornes de déplacement (calculées depuis le rail) ─────────────────────
    private float _yMin;
    private float _yMax;

    private void Start()
    {
        // Forcer la taille de la roue à 10×10
        _marqueurRoue.sizeDelta = new Vector2(TAILLE_MARQUEUR, TAILLE_MARQUEUR);

        // Rotation initiale 90° Z : Roue.png est horizontale, on la redresse
        _marqueurRoue.localEulerAngles = new Vector3(0f, 0f, 90f);

        RecalculerBornes();
    }

    private void Update()
    {
        if (_spawner == null || _marqueurRoue == null) return;

        RecalculerBornes();

        // Positionner la roue selon la progression (0 = bas, 1 = haut)
        float progression              = _spawner.Progression;
        float yPos                     = Mathf.Lerp(_yMin, _yMax, progression);
        _marqueurRoue.anchoredPosition = new Vector2(0f, yPos);

        // Rotation continue simulant le roulement
        _marqueurRoue.Rotate(Vector3.forward, -vitesseRotation * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Calcule _yMin/_yMax depuis les bords réels du rail.
    /// La roue (pivot centré) reste toujours dans les limites du rail.
    /// </summary>
    private void RecalculerBornes()
    {
        float moitié      = TAILLE_MARQUEUR * 0.5f;
        float h           = _railRect.rect.height;
        float railYCentre = _railRect.anchoredPosition.y;

        _yMin = railYCentre - h * 0.5f + moitié;
        _yMax = railYCentre + h * 0.5f - moitié;
    }
}
