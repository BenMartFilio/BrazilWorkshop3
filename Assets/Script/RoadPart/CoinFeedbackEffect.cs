using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CoinFeedbackEffect : MonoBehaviour
{
    [Header("Taille")]
    [SerializeField] private float worldScale = 0.05f;

    [Header("Scale Punch")]
    [SerializeField] private float peakScale = 1.4f;
    [SerializeField] private float overshoot = 0.15f;
    [SerializeField] private float growDuration = 0.10f;
    [SerializeField] private float holdDuration = 0.04f;
    [SerializeField] private float shrinkDuration = 0.22f;

    [Header("Alpha — discret comme Subway Surfers")]
    [Tooltip("Alpha max de l'effet (0.4 = très discret, 0.7 = visible)")]
    [SerializeField] private float maxAlpha = 0.45f;

    [Header("Rotation aléatoire")]
    [Tooltip("Amplitude max de la rotation en degrés")]
    [SerializeField] private float maxRotation = 45f;

    [Header("Dérive")]
    [SerializeField] private float driftUp = 0.06f;

    [Header("Fade")]
    [SerializeField] private float fadeStartRatio = 0.45f;

    // ── runtime ──────────────────────────────────────────────
    private SpriteRenderer sr;
    private Vector3 startPos;
    private Vector3 baseScale;
    private float elapsed;
    private float totalDuration;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private static float lastAngle = 0f; // mémorise le dernier angle utilisé

    private void OnEnable()
    {
        elapsed = 0f;
        totalDuration = growDuration + holdDuration + shrinkDuration;
        startPos = transform.position;
        baseScale = Vector3.one * worldScale;
        transform.localScale = Vector3.zero;

        // Rotation vraiment variée, en évitant de retomber sur un angle trop proche
        float angle;
        int attempts = 0;
        do
        {
            // Plage de 90° seulement car symétrie X et Y → 0-90° couvre tous les cas visuels
            // On multiplie par 4 quadrants choisis aléatoirement pour garder un vrai aléatoire
            float baseAngle = Random.Range(0f, 90f);
            int quadrant = Random.Range(0, 4);
            angle = baseAngle + quadrant * 90f;
            attempts++;
        }
        while (Mathf.Abs(Mathf.DeltaAngle(angle, lastAngle)) < 25f && attempts < 10);

        lastAngle = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        ApplyScale();
        ApplyDrift();
        ApplyFade();

        if (elapsed >= totalDuration)
            Destroy(gameObject);
    }

    private void ApplyScale()
    {
        float s;

        if (elapsed < growDuration)
        {
            float t = elapsed / growDuration;
            s = Mathf.Lerp(0f, peakScale + overshoot, EaseOutBack(t));
        }
        else if (elapsed < growDuration + holdDuration)
        {
            s = peakScale;
        }
        else
        {
            float t = (elapsed - growDuration - holdDuration) / shrinkDuration;
            s = Mathf.Lerp(peakScale, 0f, EaseInQuad(t));
        }

        transform.localScale = baseScale * s;
    }

    private void ApplyDrift()
    {
        float progress = Mathf.Clamp01(elapsed / totalDuration);
        float y = driftUp * EaseOutQuad(progress);
        transform.position = startPos + new Vector3(0f, y, 0f);
    }

    private void ApplyFade()
    {
        float fadeStart = totalDuration * fadeStartRatio;

        // Fade in rapide au début
        float alphaIn = Mathf.Clamp01(elapsed / growDuration);

        // Fade out sur la fin
        float alphaOut = 1f;
        if (elapsed > fadeStart)
        {
            float t = Mathf.Clamp01((elapsed - fadeStart) / (totalDuration - fadeStart));
            alphaOut = 1f - EaseInQuad(t);
        }

        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, maxAlpha * alphaIn * alphaOut);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseInQuad(float t) => t * t;
    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}