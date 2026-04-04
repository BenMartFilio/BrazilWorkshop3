// PopupDeath.cs — capture la scale dans Awake
using System.Collections;
using UnityEngine;

public class PopupDeath : MonoBehaviour
{
    private Vector3 _originalScale;

    private void Awake()
    {
        // Capturé une seule fois à l'initialisation — stable
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(LerpScale(0.1f));
    }

    private IEnumerator LerpScale(float time)
    {
        Vector3 fromScale = _originalScale * 0.85f;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(fromScale, _originalScale,
                EaseOutCubic(elapsed / time));
            yield return null;
        }
        transform.localScale = _originalScale;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}