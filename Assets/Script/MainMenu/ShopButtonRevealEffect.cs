using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ShopButtonRevealEffect : MonoBehaviour
{
    private const float VERTICAL_OFFSET = 35f; // pixels de décalage initial (vient d'en haut)

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPosition;
    private Coroutine _coroutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>Remet le bouton invisible avant le reveal. Doit être appelé après ForceUpdateCanvases.</summary>
    public void Init()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _originalAnchoredPosition = _rectTransform.anchoredPosition;
        _canvasGroup.alpha = 0f;
        _rectTransform.anchoredPosition = _originalAnchoredPosition + Vector2.up * VERTICAL_OFFSET;
    }

    /// <summary>Lance l'animation de reveal avec un délai et une durée donnés.</summary>
    public void PlayReveal(float delay, float duration)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(RevealCoroutine(delay, duration));
    }

    private IEnumerator RevealCoroutine(float delay, float duration)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float elapsed = 0f;
        Vector2 startPos = _originalAnchoredPosition + Vector2.up * VERTICAL_OFFSET;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Alpha monte vite → bord supérieur net
            _canvasGroup.alpha = EaseOutQuart(t);

            // Position descend plus lentement → bord inférieur traînant/diffus
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalAnchoredPosition, EaseOutCubic(t));

            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
        _coroutine = null;
    }

    // Monte très vite puis s'aplatit → front supérieur net
    private static float EaseOutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);

    // Monte régulièrement → déplacement qui traîne légèrement en bas
    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
