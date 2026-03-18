using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Barre de patience du garde affichée dans la PartieHaute.
    /// - Se vide progressivement avec le temps.
    /// - Bord droit animé comme un signal électrique (shader custom).
    /// - Subit une perte animée de POINTS_PENALITE points sur erreur du joueur.
    ///
    /// Structure créée automatiquement :
    ///   BarrePatience (RectTransform)
    ///   ├── Remplissage (Image avec shader Barrage/UI/BarrePatience — wave + glow)
    ///   └── Flash       (Image standard — flash rouge lors d'une pénalité)
    /// </summary>
    public class BarrePatience : MonoBehaviour
    {
        // ── Paramètres de jeu ────────────────────────────────────────────────
        private const float PATIENCE_MAX    = 100f;
        private const float POINTS_PENALITE = 10f;  // points perdus par erreur
        private const float VITESSE_VIDAGE  = 3f;   // points/s de décroissance naturelle

        // ── Animation de perte ────────────────────────────────────────────────
        private const float FLASH_DUREE        = 0.45f; // s — durée totale du flash alpha
        private const float SHAKE_AMPLITUDE    = 5f;    // px
        private const float SHAKE_DUREE        = 0.32f; // s

        // ── IDs des propriétés shader ─────────────────────────────────────────
        private static readonly int ID_Fill          = Shader.PropertyToID("_FillAmount");
        private static readonly int ID_SeuilMoyenne  = Shader.PropertyToID("_SeuilMoyenne");
        private static readonly int ID_SeuilBasse    = Shader.PropertyToID("_SeuilBasse");
        private static readonly int ID_ColorHaute    = Shader.PropertyToID("_ColorHaute");
        private static readonly int ID_ColorMoyenne  = Shader.PropertyToID("_ColorMoyenne");
        private static readonly int ID_ColorBasse    = Shader.PropertyToID("_ColorBasse");

        [Header("Références (laissez vide pour génération automatique)")]
        [SerializeField] private Image imageRemplissage;
        [SerializeField] private Image imageFlash;

        [Header("Couleurs")]
        [SerializeField] private Color couleurHaute   = new Color(0.18f, 0.80f, 0.18f);
        [SerializeField] private Color couleurMoyenne = new Color(0.95f, 0.70f, 0.10f);
        [SerializeField] private Color couleurBasse   = new Color(0.90f, 0.15f, 0.10f);
        [SerializeField] private Color couleurFlash   = new Color(1f, 0.15f, 0.10f, 0.75f);

        [Header("Seuils de couleur (0–1)")]
        [SerializeField] private float seuilMoyenne = 0.50f;
        [SerializeField] private float seuilBasse   = 0.25f;

        private float         _patience;
        private RectTransform _rt;
        private Vector2       _positionBase;
        private Material      _matRemplissage;
        private bool          _enPenalite;

        /// <summary>Valeur de patience normalisée entre 0 et 1.</summary>
        public float PatienceNormalisée => _patience / PATIENCE_MAX;

        /// <summary>True quand la patience atteint 0.</summary>
        public bool EstEpuisée => _patience <= 0f;

        private void Awake()
        {
            _rt       = GetComponent<RectTransform>();
            _patience = PATIENCE_MAX;

            CreerImagesSiAbsentes();
        }

        private void Start()
        {
            _positionBase = _rt.anchoredPosition;
            EnvoyerFillAuShader(1f);
        }

        private void Update()
        {
            if (EstEpuisée) return;

            _patience = Mathf.Max(0f, _patience - VITESSE_VIDAGE * Time.deltaTime);

            if (!_enPenalite)
                EnvoyerFillAuShader(PatienceNormalisée);
        }

        private void OnDestroy()
        {
            if (_matRemplissage != null)
                Destroy(_matRemplissage);
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Applique une pénalité de POINTS_PENALITE avec animation de flash et secousse.
        /// À connecter sur MainDuGardeUI.OnFormulaireIncorrect.
        /// </summary>
        public void AppliquerPénalité()
        {
            if (EstEpuisée) return;

            _patience = Mathf.Max(0f, _patience - POINTS_PENALITE);

            StartCoroutine(AnimerPerte());
            StartCoroutine(AnimerSecousse());
        }

        // ── Animations ────────────────────────────────────────────────────────

        private IEnumerator AnimerPerte()
        {
            _enPenalite = true;

            // Saut immédiat de la barre au nouveau niveau
            EnvoyerFillAuShader(PatienceNormalisée);

            // Flash rouge : apparition instantanée, disparition progressive
            if (imageFlash != null)
            {
                imageFlash.color   = couleurFlash;
                imageFlash.enabled = true;

                float t = 0f;
                while (t < FLASH_DUREE)
                {
                    t += Time.deltaTime;
                    float alpha       = Mathf.Lerp(couleurFlash.a, 0f, Mathf.SmoothStep(0f, 1f, t / FLASH_DUREE));
                    imageFlash.color  = new Color(couleurFlash.r, couleurFlash.g, couleurFlash.b, alpha);
                    yield return null;
                }

                imageFlash.enabled = false;
            }

            _enPenalite = false;
        }

        private IEnumerator AnimerSecousse()
        {
            float t = 0f;
            while (t < SHAKE_DUREE)
            {
                t += Time.deltaTime;
                float décroiss           = 1f - (t / SHAKE_DUREE);
                float offsetX            = Mathf.Sin(t * 65f) * SHAKE_AMPLITUDE * décroiss;
                _rt.anchoredPosition     = _positionBase + new Vector2(offsetX, 0f);
                yield return null;
            }
            _rt.anchoredPosition = _positionBase;
        }

        // ── Shader ────────────────────────────────────────────────────────────

        private void EnvoyerFillAuShader(float fill)
        {
            if (_matRemplissage == null) return;
            _matRemplissage.SetFloat(ID_Fill, fill);
        }

        // ── Génération automatique ────────────────────────────────────────────

        private void CreerImagesSiAbsentes()
        {
            if (imageRemplissage == null)
            {
                imageRemplissage = CreerImageEnfant("Remplissage", Color.white, useShader: true);

                // Charger le shader et créer une instance de material
                Shader shader = Shader.Find("Barrage/UI/BarrePatience");
                if (shader != null)
                {
                    _matRemplissage = new Material(shader);
                    _matRemplissage.SetFloat(ID_SeuilMoyenne, seuilMoyenne);
                    _matRemplissage.SetFloat(ID_SeuilBasse,   seuilBasse);
                    _matRemplissage.SetColor(ID_ColorHaute,   couleurHaute);
                    _matRemplissage.SetColor(ID_ColorMoyenne, couleurMoyenne);
                    _matRemplissage.SetColor(ID_ColorBasse,   couleurBasse);
                    _matRemplissage.SetFloat(ID_Fill, 1f);
                    imageRemplissage.material = _matRemplissage;
                }
                else
                {
                    Debug.LogWarning("[BarrePatience] Shader 'Barrage/UI/BarrePatience' introuvable — fallback Image.Filled.");
                    imageRemplissage.type       = Image.Type.Filled;
                    imageRemplissage.fillMethod = Image.FillMethod.Horizontal;
                    imageRemplissage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    imageRemplissage.fillAmount = 1f;
                    imageRemplissage.color      = couleurHaute;
                }
            }

            if (imageFlash == null)
            {
                imageFlash         = CreerImageEnfant("Flash", couleurFlash, useShader: false);
                imageFlash.enabled = false;
            }
        }

        private Image CreerImageEnfant(string nomGo, Color couleur, bool useShader)
        {
            var go = new GameObject(nomGo, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.layer = LayerMask.NameToLayer("UI");

            var rt        = go.GetComponent<RectTransform>();
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            var img             = go.GetComponent<Image>();
            img.color           = useShader ? Color.white : couleur;
            img.raycastTarget   = false;
            return img;
        }
    }
}
