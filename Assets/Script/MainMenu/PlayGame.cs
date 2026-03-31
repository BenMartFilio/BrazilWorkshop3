using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayGame : MonoBehaviour
{
    [SerializeField] private GameObject serrure;
    [SerializeField] private RectTransform canvasContent; // child root du Canvas

    public Vector3 rotationAmount = new Vector3(0, 0, 120);
    public float duration = 1.5f;
    public Image overlay;
    public float flashDuration = 0.1f;
    public float fadeDownDuration = 0.3f;
    public float fadeUpDuration = 0.3f;
    public float zoomScale = 1.9f; // échelle cible du zoom

    public void PlayFlash()
    {
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        yield return StartCoroutine(FadeAndZoom(0f, 0.7f, 1f, 1.6f, flashDuration));
        yield return StartCoroutine(FadeAndZoom(0.7f, 0.3f, 1.6f, 1.4f, fadeDownDuration));
        yield return StartCoroutine(FadeAndZoom(0.3f, 1f, 1.4f, 1.9f, fadeUpDuration));
        ChangeLevel(1);
    }

    IEnumerator FadeAndZoom(float alphaFrom, float alphaTo, float scaleFrom, float scaleTo, float duration)
    {
        float elapsed = 0f;
        Color color = overlay.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            color.a = Mathf.Lerp(alphaFrom, alphaTo, t);
            overlay.color = color;
            canvasContent.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = alphaTo;
        overlay.color = color;
        canvasContent.localScale = Vector3.one * scaleTo;
    }

    public void LaunchGame()
    {
        StartCoroutine(RoutineRotation(rotationAmount, duration));
    }

    IEnumerator RoutineRotation(Vector3 rotation, float time)
    {
        Quaternion startRotation = serrure.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(rotation);
        float elapsed = 0f;
        while (elapsed < time)
        {
            serrure.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        serrure.transform.rotation = endRotation;
        PlayFlash();
    }

    public void ChangeLevel(int level)
    {
        SceneManager.LoadScene(level);
    }
}