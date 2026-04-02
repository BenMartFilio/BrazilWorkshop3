using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the SliderBarrage fill value from 0 to <see cref="targetValue"/> over
/// <see cref="animationDuration"/> seconds, giving the illusion that the barrage
/// is approaching during the PanelSliderInfo tutorial step.
///
/// Only intended for MapTuto. Call <see cref="StartAnimation"/> from TutorialManager
/// when PanelSliderInfo is shown.
/// </summary>
public class TutoSliderAnimator : MonoBehaviour
{
    [Header("Slider Reference")]
    [SerializeField] private Slider _slider;

    [Header("Animation Settings")]
    [Tooltip("Slider value to animate toward (0–1). PaternTuto3 illusion targets ~0.75.")]
    [SerializeField] [Range(0f, 1f)] private float targetValue = 0.75f;

    [Tooltip("Duration in seconds for the slider to reach the target value.")]
    [SerializeField] private float animationDuration = 3f;

    [Tooltip("Optional handle RectTransform to spin during animation.")]
    [SerializeField] private RectTransform handleRoue;

    [Tooltip("Degrees per second for the handle wheel rotation.")]
    [SerializeField] private float vitesseRotation = 180f;

    private Coroutine _animationCoroutine;

    /// <summary>
    /// Resets the slider to 0 and starts animating it toward <see cref="targetValue"/>.
    /// Safe to call multiple times — cancels any in-progress animation first.
    /// </summary>
    public void StartAnimation()
    {
        if (_slider == null)
        {
            Debug.LogError("[TutoSliderAnimator] _slider is not assigned.");
            return;
        }

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _slider.value = 0f;
        _animationCoroutine = StartCoroutine(AnimateRoutine());
    }

    /// <summary>Stops the animation and holds the slider at its current value.</summary>
    public void StopAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    private IEnumerator AnimateRoutine()
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            _slider.value = Mathf.Lerp(0f, targetValue, t);

            SpinHandle(_slider.value);

            yield return null;
        }

        _slider.value = targetValue;
        SpinHandle(targetValue);
        _animationCoroutine = null;
    }

    private void SpinHandle(float progression)
    {
        if (handleRoue == null) return;

        float factor = 0.5f + progression * 0.5f;
        handleRoue.Rotate(Vector3.forward, -vitesseRotation * factor * Time.deltaTime, Space.Self);
    }
}
