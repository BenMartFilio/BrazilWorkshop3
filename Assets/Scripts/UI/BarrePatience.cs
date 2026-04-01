using System;
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
    /// - Peut être gelée via Geler() : le décompte s'arrête et OnPatienceEpuisée ne se déclenche plus.
    ///
    /// Visualisation déléguée à un IVisualisateurPatience optionnel (ex. BarrePatienceSlider).
    /// Si aucun visualisateur n'est assigné, le système shader/Image interne est utilisé.
    ///
    /// Structure créée automatiquement (sans visualisateur externe) :
    ///   BarrePatience (RectTransform)
    ///   ├── Remplissage (Image avec shader Barrage/UI/BarrePatience — wave + glow)
    ///   └── Flash       (Image standard — flash rouge lors d'une pénalité)
    /// </summary>
    public class BarrePatience : MonoBehaviour
    {
        // ── IDs des propriétés shader ─────────────────────────────────────────
        private static readonly int ID_Fill         = Shader.PropertyToID("_FillAmount");
        private static readonly int ID_SeuilMoyenne = Shader.PropertyToID("_SeuilMoyenne");
        private static readonly int ID_SeuilBasse   = Shader.PropertyToID("_SeuilBasse");
        private static readonly int ID_ColorHaute   = Shader.PropertyToID("_ColorHaute");
        private static readonly int ID_ColorMoyenne = Shader.PropertyToID("_ColorMoyenne");
        private static readonly int ID_ColorBasse   = Shader.PropertyToID("_ColorBasse");

        [Header("Patience")]
        [Tooltip("Valeur de départ à chaque barrage (points).")]
        [SerializeField, Min(1f)] private float patienceDepart = 160f;

        [Tooltip("Décroissance naturelle en points/seconde.")]
        [SerializeField, Min(0f)] private float vitesseVidage = 3f;

        [Tooltip("Points perdus sur formulaire incorrect.")]
        [SerializeField, Min(0f)] private float pointsPenalite = 10f;

        [Header("Visualisateur externe (optionnel)")]
        [Tooltip("Assignez ici un BarrePatienceSlider pour utiliser le visuel Slider à la place du shader interne.")]
        [SerializeField] private MonoBehaviour visualisateurExterne;

        [Header("Références internes (laissez vide pour génération automatique)")]
        [SerializeField] private Image imageRemplissage;
        [SerializeField] private Image imageFlash;

        [Header("Couleurs")]
        [SerializeField] private Color couleurHaute   = new Color(0.18f, 0.80f, 0.18f);
        [SerializeField] private Color couleurMoyenne = new Color(0.95f, 0.70f, 0.10f);
        [SerializeField] private Color couleurBasse   = new Color(0.90f, 0.15f, 0.10f);
        [SerializeField] private Color couleurFlash   = new Color(1f, 0.15f, 0.10f, 0.75f);

        [Header("Seuils de couleur (0–1)")]
        [SerializeField, Range(0f, 1f)] private float seuilMoyenne = 0.50f;
        [SerializeField, Range(0f, 1f)] private float seuilBasse   = 0.25f;

        [Header("Animation de pénalité")]
        [SerializeField, Min(0f)] private float flashDurée     = 0.45f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 5f;
        [SerializeField, Min(0f)] private float shakeDurée     = 0.32f;

        [Header("Tremblement passif")]
        [SerializeField, Min(0f)] private float trembleVertAmplitude   = 0.8f;
        [SerializeField, Min(0f)] private float trembleVertFrequence   = 8f;
        [SerializeField, Min(0f)] private float trembleOrangeAmplitude = 3.5f;
        [SerializeField, Min(0f)] private float trembleOrangeFrequence = 22f;
        [SerializeField, Min(0f)] private float trembleRougeAmplitude  = 7f;
        [SerializeField, Min(0f)] private float trembleRougeFrequence  = 45f;

        private float         _patience;
        private RectTransform _rt;
        private Vector2       _positionBase;
        private Material      _matRemplissage;
        private bool          _enPenalite;
        private bool          _épuiséeDéclenché;
        private float         _trembleTemps;
        private bool          _gelée;

        private IVisualisateurPatience _visualisateur;

        /// <summary>Valeur de patience normalisée entre 0 et 1.</summary>
        public float PatienceNormalisée => _patience / patienceDepart;

        /// <summary>Seuil en dessous duquel la barre passe en orange (0–1).</summary>
        public float SeuilMoyenne => seuilMoyenne;

        /// <summary>Seuil en dessous duquel la barre passe en rouge (0–1).</summary>
        public float SeuilBasse => seuilBasse;

        /// <summary>True quand la patience atteint 0.</summary>
        public bool EstEpuisée => _patience <= 0f;

        /// <summary>True quand la barre est gelée (décompte et game over suspendus).</summary>
        public bool EstGelée => _gelée;

        /// <summary>Déclenché une seule fois quand la patience atteint 0.</summary>
        public event Action OnPatienceEpuisée;

        private void Awake()
        {
            _rt       = GetComponent<RectTransform>();
            _patience = patienceDepart;

            // Résoudre le visualisateur externe en IVisualisateurPatience
            if (visualisateurExterne != null && visualisateurExterne is IVisualisateurPatience v)
                _visualisateur = v;

            // Le système shader interne n'est créé que s'il n'y a pas de visualisateur externe
            if (_visualisateur == null)
                CreerImagesSiAbsentes();
        }

        private void Start()
        {
            _positionBase = _rt.anchoredPosition;
            NotifierVisualisateur(1f);
        }

        private void Update()
        {
            if (_gelée) return;

            if (EstEpuisée)
            {
                if (!_épuiséeDéclenché)
                {
                    _épuiséeDéclenché = true;
                    Debug.Log("[BarrePatience] Patience épuisée → game over.");
                    OnPatienceEpuisée?.Invoke();
                }
                return;
            }

            _patience = Mathf.Max(0f, _patience - vitesseVidage * Time.deltaTime);

            if (!_enPenalite)
                NotifierVisualisateur(PatienceNormalisée);

            AppliquerTremblement();
        }

        /// <summary>Tremblement passif : intensité croissante selon la couleur de la barre.</summary>
        private void AppliquerTremblement()
        {
            if (_enPenalite) return;

            float n         = PatienceNormalisée;
            float amplitude = n > seuilMoyenne ? trembleVertAmplitude
                            : n > seuilBasse   ? trembleOrangeAmplitude
                                               : trembleRougeAmplitude;
            float frequence = n > seuilMoyenne ? trembleVertFrequence
                            : n > seuilBasse   ? trembleOrangeFrequence
                                               : trembleRougeFrequence;

            _trembleTemps       += Time.deltaTime;
            float offsetX        = Mathf.Sin(_trembleTemps * frequence * Mathf.PI * 2f) * amplitude;
            _rt.anchoredPosition = _positionBase + new Vector2(offsetX, 0f);
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
        /// Ignoré si la barre est gelée.
        /// </summary>
        public void AppliquerPénalité()
        {
            if (EstEpuisée || _gelée) return;

            _patience = Mathf.Max(0f, _patience - pointsPenalite);

            StartCoroutine(AnimerPerte());
            StartCoroutine(AnimerSecousse());
        }

        /// <summary>
        /// Ajoute directement un nombre de points à la patience, dans la limite de patienceDepart.
        /// Utilisé par l'effet Liasse de Billets.
        /// </summary>
        public void AjouterPatience(float points)
        {
            _patience = Mathf.Min(patienceDepart, _patience + points);
        }

        /// <summary>
        /// Gèle la barre : le décompte s'arrête, les pénalités sont ignorées
        /// et OnPatienceEpuisée ne peut plus se déclencher.
        /// Appeler dès que le barrage est validé.
        /// </summary>
        public void Geler()
        {
            _gelée = true;
            _rt.anchoredPosition = _positionBase;
            Debug.Log("[BarrePatience] Gelée — game over suspendu.");
        }

        /// <summary>
        /// Remet la patience à patienceDepart et dégèle la barre.
        /// À appeler au chargement de chaque barrage.
        /// </summary>
        public void Réinitialiser()
        {
            _patience            = patienceDepart;
            _gelée               = false;
            _épuiséeDéclenché    = false;
            _enPenalite          = false;
            _trembleTemps        = 0f;
            _rt.anchoredPosition = _positionBase;
            NotifierVisualisateur(1f);
            Debug.Log("[BarrePatience] Réinitialisée.");
        }

        /// <summary>Dégèle uniquement, sans remettre la patience à son maximum.</summary>
        public void Dégeler()
        {
            _gelée = false;
            Debug.Log("[BarrePatience] Dégelée.");
        }

        // ── Animations ────────────────────────────────────────────────────────

        private IEnumerator AnimerPerte()
        {
            _enPenalite = true;

            NotifierVisualisateur(PatienceNormalisée);
            _visualisateur?.AnimerFlash(couleurFlash);

            if (_visualisateur == null && imageFlash != null)
            {
                imageFlash.color   = couleurFlash;
                imageFlash.enabled = true;

                float t = 0f;
                while (t < flashDurée)
                {
                    t += Time.deltaTime;
                    float alpha      = Mathf.Lerp(couleurFlash.a, 0f, Mathf.SmoothStep(0f, 1f, t / flashDurée));
                    imageFlash.color = new Color(couleurFlash.r, couleurFlash.g, couleurFlash.b, alpha);
                    yield return null;
                }

                imageFlash.enabled = false;
            }
            else
            {
                yield return new WaitForSeconds(flashDurée);
            }

            _enPenalite = false;
        }

        private IEnumerator AnimerSecousse()
        {
            float t = 0f;
            while (t < shakeDurée)
            {
                t += Time.deltaTime;
                float décroiss       = 1f - (t / shakeDurée);
                float offsetX        = Mathf.Sin(t * 65f) * shakeAmplitude * décroiss;
                _rt.anchoredPosition = _positionBase + new Vector2(offsetX, 0f);
                yield return null;
            }
        }

        // ── Shader interne ────────────────────────────────────────────────────

        private void NotifierVisualisateur(float fill)
        {
            if (_visualisateur != null)
                _visualisateur.MettreAJour(fill, seuilMoyenne, seuilBasse, couleurHaute, couleurMoyenne, couleurBasse);
            else
                EnvoyerFillAuShader(fill);
        }

        private void EnvoyerFillAuShader(float fill)
        {
            if (_matRemplissage == null) return;
            _matRemplissage.SetFloat(ID_Fill, fill);
        }

        // ── Génération automatique (système shader interne) ───────────────────

        private void CreerImagesSiAbsentes()
        {
            if (imageRemplissage == null)
            {
                imageRemplissage = CreerImageEnfant("Remplissage", Color.white, useShader: true);

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

            var rt       = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img           = go.GetComponent<Image>();
            img.color         = useShader ? Color.white : couleur;
            img.raycastTarget = false;
            return img;
        }
    }
}
