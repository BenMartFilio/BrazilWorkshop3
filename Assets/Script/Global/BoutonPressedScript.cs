// BoutonPressedScript.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class BoutonPressedScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    public float pressedScale = 0.9f;
    public float bounceScale = 1.06f;
    public float animationSpeed = 12f;

    [Header("Mobile")]
    public bool enableVibration = true;

    [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
    [SerializeField] private AudioType _click;
    [SerializeField] private bool _muteSound = false;

    private Vector3 _originalScale;
    private Coroutine _currentAnimation;

    private void Start() => _originalScale = transform.localScale;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_audioEventDispatcher != null && !_muteSound)
            _audioEventDispatcher.PlayAudio(_click);

        StartAnim(_originalScale * pressedScale, animationSpeed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (enableVibration) Handheld.Vibrate();
#endif
        if (_currentAnimation != null) StopCoroutine(_currentAnimation);
        _currentAnimation = StartCoroutine(BounceEffect());
    }

    private void StartAnim(Vector3 target, float speed)
    {
        if (_currentAnimation != null) StopCoroutine(_currentAnimation);
        _currentAnimation = StartCoroutine(ScaleTo(target, speed));
    }

    // Lerp sur durée fixe au lieu de distance — converge toujours en exactement 1/speed secondes
    private IEnumerator ScaleTo(Vector3 target, float speed)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        float duration = 1f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.LerpUnclamped(start, target,
                EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        transform.localScale = target;
    }

    private IEnumerator BounceEffect()
    {
        yield return StartCoroutine(ScaleTo(_originalScale * bounceScale, animationSpeed));
        yield return StartCoroutine(ScaleTo(_originalScale, animationSpeed));
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}