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

    private Vector3 originalScale;
    private Coroutine currentAnimation;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartAnimation(originalScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (enableVibration)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(BounceEffect());
    }

    void StartAnimation(Vector3 target)
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(ScaleTo(target));
    }

    IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                target,
                Time.deltaTime * animationSpeed
            );
            yield return null;
        }

        transform.localScale = target;
    }

    IEnumerator BounceEffect()
    {
        yield return StartCoroutine(ScaleTo(originalScale * bounceScale));
        yield return StartCoroutine(ScaleTo(originalScale));
    }
}
