using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère l'écran de chargement uniquement au premier lancement du jeu.
/// Le panel reste caché lors des retours sur la scène MainMenu en cours de session.
/// Le slider progresse en déplaçant l'ancre droite du RectTransform du fill (gauche → droite).
/// Les deux écrous tournent autour de l'eye central avec une vitesse oscillante.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("Références UI")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private RectTransform _sliderFillRect;
    [SerializeField] private RectTransform _rotationPivot;

    [Header("Paramètres du chargement")]
    [SerializeField] private float _loadingDuration = 3.5f;
    [SerializeField] private AnimationCurve _progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Paramètres de rotation")]
    [SerializeField] private float _rotationSpeedMin = 60f;
    [SerializeField] private float _rotationSpeedMax = 420f;
    [SerializeField] private float _rotationOscillationSpeed = 1.2f;

    // Persistant entre les chargements de scène
    private static bool _hasShownLoadingScreen = false;

    private float _currentAngle = 0f;
    private bool _isLoading = false;

    // Cache pour éviter les allocations répétées
    private Vector2 _anchorCache = Vector2.one;
    private Quaternion _rotationCache = Quaternion.identity;

    // Constantes précalculées
    private const float HALF = 0.5f;
    private const float FULL_ROTATION = 360f;
    private static readonly Vector2 ANCHOR_START = new Vector2(0f, 1f);
    private static readonly Vector2 ANCHOR_COMPLETE = Vector2.one;

    private void Awake()
    {
        // Validation des références
        ValidateReferences();

        // Initialisation des caches
        _anchorCache = ANCHOR_START;
    }

    private void Start()
    {
        if (_hasShownLoadingScreen)
        {
            HideLoadingScreen();
            return;
        }

        ShowLoadingScreen();
    }

    private void Update()
    {
        if (_isLoading)
        {
            AnimateRotation();
        }
    }

    private void OnDestroy()
    {
    }

    /// <summary>
    /// Vérifie que toutes les références sont assignées
    /// </summary>
    private void ValidateReferences()
    {
        if (_loadingPanel == null)
            Debug.LogError($"[{nameof(LoadingScreen)}] _loadingPanel n'est pas assigné!", this);

        if (_sliderFillRect == null)
            Debug.LogError($"[{nameof(LoadingScreen)}] _sliderFillRect n'est pas assigné!", this);

        if (_rotationPivot == null)
            Debug.LogError($"[{nameof(LoadingScreen)}] _rotationPivot n'est pas assigné!", this);
    }

    /// <summary>
    /// Affiche et initialise l'écran de chargement
    /// </summary>
    private void ShowLoadingScreen()
    {
        _loadingPanel.SetActive(true);
        _sliderFillRect.anchorMax = ANCHOR_START;
        _currentAngle = 0f;
        _isLoading = true;
        StartCoroutine(RunLoadingSequence());
    }

    /// <summary>
    /// Cache immédiatement l'écran de chargement
    /// </summary>
    private void HideLoadingScreen()
    {
        _loadingPanel.SetActive(false);
        _isLoading = false;
    }

    /// <summary>
    /// Coroutine principale du chargement
    /// </summary>
    private IEnumerator RunLoadingSequence()
    {
        float elapsed = 0f;

        while (elapsed < _loadingDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / _loadingDuration;

            // Application de la courbe d'animation pour un chargement plus naturel
            float progress = _progressCurve.Evaluate(normalizedTime);

            // Mise à jour du fill avec cache réutilisé
            _anchorCache.x = progress;
            _sliderFillRect.anchorMax = _anchorCache;

            yield return null;
        }

        // Assure que le slider est à 100%
        _sliderFillRect.anchorMax = ANCHOR_COMPLETE;

        // Petit délai avant de masquer
        yield return new WaitForSeconds(0.3f);

        _isLoading = false;
        _hasShownLoadingScreen = true;
        _loadingPanel.SetActive(false);
    }

    /// <summary>
    /// Anime la rotation oscillante des écrous autour de l'eye
    /// </summary>
    private void AnimateRotation()
    {
        // Calcul optimisé de la vitesse oscillante
        float t = (Mathf.Sin(Time.time * _rotationOscillationSpeed) + 1f) * HALF;
        float speed = Mathf.Lerp(_rotationSpeedMin, _rotationSpeedMax, t);

        // Mise à jour de l'angle avec modulo optimisé
        _currentAngle = (_currentAngle + speed * Time.deltaTime) % FULL_ROTATION;

        // Application de la rotation avec cache
        _rotationCache = Quaternion.Euler(0f, 0f, _currentAngle);
        _rotationPivot.localRotation = _rotationCache;
    }

    /// <summary>
    /// Permet de réinitialiser manuellement le flag (utile pour tests ou cas spéciaux)
    /// </summary>
    public static void ResetLoadingFlag()
    {
        _hasShownLoadingScreen = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Méthode helper pour forcer l'affichage dans l'éditeur
    /// </summary>
    [ContextMenu("Force Show Loading Screen")]
    private void ForceShowInEditor()
    {
        ResetLoadingFlag();
        Start();
    }
#endif
}