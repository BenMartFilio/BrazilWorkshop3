using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Vignette d'assombrissement pour la séquence de game over.
    /// Génère une texture gradient radiale : transparent au centre, sombre aux bords.
    /// Appeler AnimerApparition / AnimerDisparition pour contrôler la visibilité.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class VignetteGameOver : MonoBehaviour
    {
        private const float SQRT2 = 1.41421356f;

        // ── Apparence ─────────────────────────────────────────────────────────
        [Header("Apparence")]
        [Tooltip("Couleur d'assombrissement (généralement noir).")]
        [SerializeField] private Color couleurVignette = Color.black;

        [Tooltip("Opacité au centre de l'écran (0 = transparent, 1 = opaque).")]
        [SerializeField, Range(0f, 1f)] private float intensitéCentre = 0.05f;

        [Tooltip("Opacité aux bords et aux coins de l'écran (0 = transparent, 1 = opaque).")]
        [SerializeField, Range(0f, 1f)] private float intensitéBords = 0.85f;

        [Tooltip("Courbe de falloff : X = distance normalisée du centre [0..1], Y = facteur d'intensité [0..1].")]
        [SerializeField] private AnimationCurve courbeFalloff = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── Qualité ───────────────────────────────────────────────────────────
        [Header("Qualité")]
        [Tooltip("Résolution de la texture de gradient en pixels (64–512). Plus élevé = plus lisse.")]
        [SerializeField] private int résolutionTexture = 256;

        // ── Animation ─────────────────────────────────────────────────────────
        [Header("Animation")]
        [Tooltip("Opacité initiale atteinte lors de l'apparition (début du game over).")]
        [SerializeField, Range(0f, 1f)] private float alphaInitial = 0.3f;

        [Tooltip("Durée de l'animation d'apparition initiale en secondes.")]
        [SerializeField] private float duréeApparition = 1.2f;

        [Tooltip("Durée de l'animation de disparition en secondes.")]
        [SerializeField] private float duréeDisparition = 0.6f;

        // ── Debug ─────────────────────────────────────────────────────────────
        [Header("Debug")]
        [Tooltip("Si vrai, affiche la vignette à pleine opacité dès le démarrage pour valider l'affichage.")]
        [SerializeField] private bool afficherAuDémarrage = false;

        // ── État interne ──────────────────────────────────────────────────────
        private RawImage  _rawImage;
        private Texture2D _texture;
        private Coroutine _coroutineEnCours;

        /// <summary>Alpha de départ atteint lors de l'apparition initiale.</summary>
        public float AlphaInitial => alphaInitial;

        // ── Cycle de vie ──────────────────────────────────────────────────────

        private void Awake()
        {
            // S'assurer que le GameObject est sur le layer UI pour le rendu Canvas
            gameObject.layer = LayerMask.NameToLayer("UI");

            _rawImage = GetComponent<RawImage>();
            _rawImage.raycastTarget = false;

            GenérerTexture();

            // Commencer invisible sauf si le mode debug est actif
            Color c = couleurVignette;
            c.a = afficherAuDémarrage ? 1f : 0f;
            _rawImage.color = c;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (_rawImage == null) _rawImage = GetComponent<RawImage>();
            GenérerTexture();
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Anime la vignette jusqu'à alphaInitial — à appeler en début de séquence game over.
        /// </summary>
        public IEnumerator AnimerApparition()
        {
            yield return FaderVers(alphaInitial, duréeApparition);
        }

        /// <summary>
        /// Anime la vignette vers un alpha cible en une durée donnée, sans bloquer l'appelant.
        /// Interrompt toute animation précédente.
        /// </summary>
        public void AnimerVersAlpha(float alphaTarget, float durée)
        {
            if (_coroutineEnCours != null)
                StopCoroutine(_coroutineEnCours);
            _coroutineEnCours = StartCoroutine(FadeCoroutine(Mathf.Clamp01(alphaTarget), durée));
        }

        /// <summary>Anime la disparition de la vignette.</summary>
        public IEnumerator AnimerDisparition()
        {
            yield return FaderVers(0f, duréeDisparition);
        }

        // ── Génération de la texture ──────────────────────────────────────────

        /// <summary>Génère ou régénère la texture de gradient radial.</summary>
        private void GenérerTexture()
        {
            if (_texture != null)
                Destroy(_texture);

            int res = Mathf.Max(64, résolutionTexture);
            _texture = new Texture2D(res, res, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode    = TextureWrapMode.Clamp,
                filterMode  = FilterMode.Bilinear
            };

            var pixels   = new Color[res * res];
            float invHalf = 1f / (res * 0.5f);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    // Espace normalisé [-1, 1] sur chaque axe
                    float nx = (x - res * 0.5f) * invHalf;
                    float ny = (y - res * 0.5f) * invHalf;

                    // Distance du centre normalisée par sqrt(2) :
                    // 0 = centre, 1 = coin — les bords latéraux sont à ~0.7
                    float dist  = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny) / SQRT2);
                    float t     = courbeFalloff.Evaluate(dist);
                    float alpha = Mathf.Lerp(intensitéCentre, intensitéBords, t);

                    // RGB blanc pour que la RawImage.color contrôle la teinte
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            _texture.SetPixels(pixels);
            _texture.Apply(updateMipmaps: false);

            if (_rawImage != null)
                _rawImage.texture = _texture;
        }

        // ── Animation interne ─────────────────────────────────────────────────

        private IEnumerator FaderVers(float alphaTarget, float durée)
        {
            if (_coroutineEnCours != null)
                StopCoroutine(_coroutineEnCours);
            _coroutineEnCours = StartCoroutine(FadeCoroutine(alphaTarget, durée));
            yield return _coroutineEnCours;
        }

        private IEnumerator FadeCoroutine(float alphaTarget, float durée)
        {
            float alphaDepart = _rawImage.color.a;
            float t = 0f;

            while (t < durée)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / durée));
                Color c = couleurVignette;
                c.a = Mathf.Lerp(alphaDepart, alphaTarget, p);
                _rawImage.color = c;
                yield return null;
            }

            Color cFinal = couleurVignette;
            cFinal.a = alphaTarget;
            _rawImage.color = cFinal;
            _coroutineEnCours = null;
        }
    }
}
