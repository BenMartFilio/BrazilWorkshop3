using System;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

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
    private SpriteRenderer _sr;
    private Vector3 _startPos;
    private Vector3 _baseScale;
    private float _elapsed;
    private float _totalDuration;

    // Pool assigné par CoinsScript au moment du spawn
    private IObjectPool<CoinFeedbackEffect> _pool;

    private static float s_lastAngle = 0f;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _totalDuration = growDuration + holdDuration + shrinkDuration;
        _baseScale = Vector3.one * worldScale;
    }

    /// <summary>Référence au pool pour que l'objet puisse se recycler lui-même.</summary>
    public void SetPool(IObjectPool<CoinFeedbackEffect> pool) => _pool = pool;

    public void Play(Vector3 worldPosition, Vector3 scale)
    {
        transform.position = worldPosition;
        transform.localScale = scale;

        _elapsed = 0f;
        _startPos = worldPosition;

        float angle;
        int attempts = 0;
        do
        {
            angle = Random.Range(0f, 90f) + Random.Range(0, 4) * 90f;
            attempts++;
        }
        while (Mathf.Abs(Mathf.DeltaAngle(angle, s_lastAngle)) < 25f && attempts < 10);

        s_lastAngle = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.zero;

        Color c = _sr.color;
        _sr.color = new Color(c.r, c.g, c.b, 0f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        ApplyScale();
        ApplyDrift();
        ApplyFade();

        if (_elapsed >= _totalDuration)
            ReturnToPool();
    }

    /// <summary>Retourne l'objet au pool au lieu de le détruire.</summary>
    private void ReturnToPool()
    {
        if (_pool != null)
            _pool.Release(this);
        else
            gameObject.SetActive(false);
    }

    private void ApplyScale()
    {
        float s;

        if (_elapsed < growDuration)
        {
            float t = _elapsed / growDuration;
            s = Mathf.Lerp(0f, peakScale + overshoot, EaseOutBack(t));
        }
        else if (_elapsed < growDuration + holdDuration)
        {
            s = peakScale;
        }
        else
        {
            float t = (_elapsed - growDuration - holdDuration) / shrinkDuration;
            s = Mathf.Lerp(peakScale, 0f, EaseInQuad(t));
        }

        transform.localScale = _baseScale * s;
    }

    private void ApplyDrift()
    {
        float progress = Mathf.Clamp01(_elapsed / _totalDuration);
        float y = driftUp * EaseOutQuad(progress);
        transform.position = _startPos + new Vector3(0f, y, 0f);
    }

    private void ApplyFade()
    {
        float fadeStart = _totalDuration * fadeStartRatio;

        float alphaIn = Mathf.Clamp01(_elapsed / growDuration);

        float alphaOut = 1f;
        if (_elapsed > fadeStart)
        {
            float t = Mathf.Clamp01((_elapsed - fadeStart) / (_totalDuration - fadeStart));
            alphaOut = 1f - EaseInQuad(t);
        }

        Color c = _sr.color;
        _sr.color = new Color(c.r, c.g, c.b, maxAlpha * alphaIn * alphaOut);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseInQuad(float t) => t * t;
    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}