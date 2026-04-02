using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Joue un effet visuel d'échec sur un bouton UI : tremblement rotatif gauche-droite
/// et teinte rouge temporaire. À brancher sur l'événement onPurchaseFailed du CoinPurchaseButton.
/// </summary>
public class BoutonEchecEffect : MonoBehaviour
{
    // ── Tremblement ──────────────────────────────────────────────────────────

    private const float DUREE_DEFAUT = 0.45f; // secondes
    private const float ANGLE_DEFAUT = 6f;    // degrés
    private const float FREQUENCE_DEFAUT = 28f;   // oscillations/s

    [Header("Tremblement")]
    [Tooltip("Durée totale de l'animation de tremblement.")]
    [SerializeField] private float durée = DUREE_DEFAUT;

    [Tooltip("Angle maximal de rotation de chaque côté (degrés).")]
    [SerializeField] private float angleMax = ANGLE_DEFAUT;

    [Tooltip("Vitesse des oscillations gauche-droite.")]
    [SerializeField] private float fréquence = FREQUENCE_DEFAUT;

    // ── Teinte rouge ─────────────────────────────────────────────────────────

    [Header("Teinte")]
    [Tooltip("Graphic à teinter (Image du bouton). Laissez vide pour chercher automatiquement.")]
    [SerializeField] private Graphic graphic;

    [Tooltip("Couleur de la teinte d'échec.")]
    [SerializeField] private Color couleurEchec = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("Durée du fondu de retour à la couleur d'origine.")]
    [SerializeField] private float duréeFonduRetour = 0.3f;

    // ── État interne ─────────────────────────────────────────────────────────

    private Coroutine _coroutineTremblement;
    private Coroutine _coroutineTeinte;
    private float _angleBase;
    private Color _couleurBase;

    private void Awake()
    {
        if (graphic == null)
            graphic = GetComponentInChildren<Graphic>();

        if (graphic != null)
            _couleurBase = graphic.color;
    }

    // ── API publique ─────────────────────────────────────────────────────────

    /// <summary>
    /// Déclenche l'effet d'échec. Brancher sur l'événement onPurchaseFailed du CoinPurchaseButton.
    /// </summary>
    public void JouerEffetEchec()
    {
        if (_coroutineTremblement != null)
        {
            StopCoroutine(_coroutineTremblement);
            transform.localEulerAngles = new Vector3(0f, 0f, _angleBase);
        }

        if (_coroutineTeinte != null)
            StopCoroutine(_coroutineTeinte);

        _coroutineTremblement = StartCoroutine(CoroutineTremblement());
        _coroutineTeinte = StartCoroutine(CoroutineTeinte());
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator CoroutineTremblement()
    {
        _angleBase = transform.localEulerAngles.z;

        float t = 0f;
        while (t < durée)
        {
            t += Time.deltaTime;

            // Amortissement exponentiel : l'effet s'atténue vers la fin
            float amortissement = 1f - Mathf.SmoothStep(0f, 1f, t / durée);

            // Oscillation sinusoïdale gauche-droite
            float angle = Mathf.Sin(t * fréquence) * angleMax * amortissement;

            transform.localEulerAngles = new Vector3(0f, 0f, _angleBase + angle);

            yield return null;
        }

        transform.localEulerAngles = new Vector3(0f, 0f, _angleBase);
        _coroutineTremblement = null;
    }

    private IEnumerator CoroutineTeinte()
    {
        if (graphic == null) yield break;

        // Passage instantané à la couleur d'échec
        graphic.color = couleurEchec;

        // Fondu progressif de retour à la couleur d'origine
        float t = 0f;
        while (t < duréeFonduRetour)
        {
            t += Time.deltaTime;
            graphic.color = Color.Lerp(couleurEchec, _couleurBase, t / duréeFonduRetour);
            yield return null;
        }

        graphic.color = _couleurBase;
        _coroutineTeinte = null;
    }
}
