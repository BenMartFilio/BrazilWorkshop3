// ScreenShake.cs — évite les allocations Vector3 par frame
using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    private Vector3 _originalPos;
    private Vector3 _shakePos; // réutilisé, pas de new à chaque frame

    private void Awake() => _originalPos = transform.localPosition;

    public IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            _shakePos.x = _originalPos.x + Random.Range(-1f, 1f) * magnitude;
            _shakePos.y = _originalPos.y + Random.Range(-1f, 1f) * magnitude;
            _shakePos.z = _originalPos.z;
            transform.localPosition = _shakePos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _originalPos;
    }
}