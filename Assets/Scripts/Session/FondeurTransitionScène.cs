using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Composant porté par le GameObject DontDestroyOnLoad du SessionManager.
/// Fournit un fondu au noir (fade-out) avant le changement de scène,
/// et un fondu depuis le noir (fade-in) après le chargement.
/// </summary>
public class FondeurTransitionScène : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static FondeurTransitionScène Instance { get; private set; }

    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Paramètres")]
    [Tooltip("Durée du fondu au noir avant le changement de scène (secondes).")]
    [SerializeField, Min(0f)] private float duréeFadeOut = 0.4f;

    [Tooltip("Durée du fondu depuis le noir après l'arrivée dans la nouvelle scène (secondes).")]
    [SerializeField, Min(0f)] private float duréeFadeIn = 0.5f;

    // ── État interne ──────────────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private Coroutine   _coroutine;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fait un fondu au noir puis appelle <paramref name="onNoir"/> quand l'écran est entièrement noir.
    /// </summary>
    public Coroutine FondreVersNoir(Action onNoir = null)
        => LancerCoroutine(AnimerVersNoir(duréeFadeOut, onNoir));

    /// <summary>
    /// Fait un fondu depuis le noir vers transparent.
    /// À appeler depuis la nouvelle scène après le chargement.
    /// </summary>
    public Coroutine FondreDepuisNoir()
        => LancerCoroutine(AnimerDepuisNoir(duréeFadeIn));

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _canvasGroup = CréerPanneauNoir();
        _canvasGroup.alpha          = 0f;
        _canvasGroup.interactable   = false;
        _canvasGroup.blocksRaycasts = false;
    }

    // ── Construction du panneau ───────────────────────────────────────────────

    /// <summary>
    /// Crée un Canvas Screen Space Overlay racine (non enfant du SessionManager)
    /// avec un panneau noir plein écran. Le CanvasGroup est sur le panneau enfant.
    /// </summary>
    private CanvasGroup CréerPanneauNoir()
    {
        // Canvas en objet racine indépendant — obligatoire pour que le RectTransform
        // enfant prenne bien la taille plein écran en ScreenSpaceOverlay.
        var canvasGO = new GameObject("FondeurCanvas");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panneau noir plein écran, enfant du Canvas.
        var panneauGO = new GameObject("PanneauNoir");
        panneauGO.transform.SetParent(canvasGO.transform, false);

        var img = panneauGO.AddComponent<Image>();
        img.color   = Color.black;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;

        return panneauGO.AddComponent<CanvasGroup>();
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private Coroutine LancerCoroutine(IEnumerator routine)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(routine);
        return _coroutine;
    }

    private IEnumerator AnimerVersNoir(float durée, Action onNoir)
    {
        _canvasGroup.blocksRaycasts = true;

        if (durée <= 0f)
        {
            _canvasGroup.alpha = 1f;
            onNoir?.Invoke();
            _coroutine = null;
            yield break;
        }

        float t = _canvasGroup.alpha; // reprend depuis l'alpha actuel si fondu interrompu
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / durée;
            _canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        onNoir?.Invoke();
        _coroutine = null;
    }

    private IEnumerator AnimerDepuisNoir(float durée)
    {
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (durée <= 0f)
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;
            _coroutine = null;
            yield break;
        }

        float t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime / durée;
            _canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        _canvasGroup.alpha          = 0f;
        _canvasGroup.blocksRaycasts = false;
        _coroutine = null;
    }
}
