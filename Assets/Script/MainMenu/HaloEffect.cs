using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fait tourner des Images UI pour simuler un halo animé autour d'un objet obtenu.
/// Attach sur le GameObject parent du halo, activer/désactiver le GO pour lancer/stopper.
/// </summary>
public class HaloEffect : MonoBehaviour
{
    [Header("Anneaux")]
    [SerializeField] private RectTransform outerRing;
    [Tooltip("Degrés par seconde, positif = sens horaire.")]
    [SerializeField] private float outerRotationSpeed = 60f;

    [SerializeField] private RectTransform innerRing;
    [SerializeField] private float innerRotationSpeed = -90f;

    [Header("Pulse de scale")]
    [SerializeField] private float pulseAmplitude = 0.08f;
    [Tooltip("Cycles par seconde.")]
    [SerializeField] private float pulseFrequency = 1.2f;

    private Vector3 _baseScale;

    private void OnEnable()
    {
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        if (outerRing != null)
            outerRing.Rotate(0f, 0f, -outerRotationSpeed * delta);

        if (innerRing != null)
            innerRing.Rotate(0f, 0f, -innerRotationSpeed * delta);

        // Pulse
        float pulse = 1f + pulseAmplitude * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f);
        transform.localScale = _baseScale * pulse;
    }

    private void OnDisable()
    {
        // Remet le scale à l'état initial quand on désactive
        transform.localScale = _baseScale;
    }
}
