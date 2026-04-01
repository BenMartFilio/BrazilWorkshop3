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
        // ── Paramètres de jeu ────────────────────────────────────────────────
        public const float PATIENCE_MAX     = 160f;
        private const float POINTS_PENALITE = 10f;  // points perdus par erreur
        private const float VITESSE_VIDAGE  = 3f;   // points/s de décroissance naturelle

        // ── Animation de perte ────────────────────────────────────────────────
        private const float FLASH_DUREE     = 0.45f; // s — durée totale du flash alpha
        private const float SHAKE_AMPLITUDE = 5f;    // px
        private const float SHAKE_DUREE     = 0.32f; // s

        // ── Tremblement passif continu (selon la couleur de la barre) ─────────
        private const float TREMBLE_VERT_AMPLITUDE   = 0.8f;  // px — discret
        private const float TREMBLE_VERT_FREQUENCE   = 8f;    // Hz
        private const float TREMBLE_ORANGE_AMPLITUDE = 3.5f;  // px — nerveux
        private const float TREMBLE_ORANGE_FREQUENCE = 22f;   // Hz
        private const float TREMBLE_ROUGE_AMPLITUDE  = 7f;    // px — très nerveux
        private const float TREMBLE_ROUGE_FREQUENCE  = 45f;   // Hz

        // ── IDs des propriétés shader ─────────────────────────────────────────
        private static readonly int ID_Fill         = Shader.PropertyToID("_FillAmount");
        private static readonly int ID_SeuilMoyenne = Shader.PropertyToID("_SeuilMoyenne");
        private static readonly int ID_SeuilBasse   = Shader.PropertyToID("_SeuilBasse");
        private static readonly int ID_ColorHaute   = Shader.PropertyToID("_ColorHaute");
        private static readonly int ID_ColorMoyenne = Shader.PropertyToID("_ColorMoyenne");
        private static readonly int ID_ColorBasse   = Shader.PropertyToID("_ColorBasse");

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
        [SerializeField] private float seuilMoyenne = 0.50f;
        [SerializeField] private float seuilBasse   = 0.25f;

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
        public float PatienceNormalisée => _patience / PATIENCE_MAX;

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
            _patience = PATIENCE_MAX;

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

            if (_visualisateur != null)
                _visualisateur.MettreAJour(1f, seuilMoyenne, seuilBasse, couleurHaute, couleurMoyenne, couleurBasse);
            else
                EnvoyerFillAuShader(1f);
        }

        private void Update()
        {
            if (_gelée) return;

            if (EstEpuisée)
            {
                if (!_épuiséeDéclenché)
                {
                    _épuiséeDéclenché = true;
                    OnPatienceEpuisée?.Invoke();
                }
                return;
            }

            _patience = Mathf.Max(0f, _patience - VITESSE_VIDAGE * Time.deltaTime);

            if (!_enPenalite)
                NotifierVisualisateur(PatienceNormalisée);

            AppliquerTremblement();
        }

        /// <summary>Applique un tremblement passif continu selon le niveau de patience.</summary>
        private void AppliquerTremblement()
        {
            if (_enPenalite) return;

            float amplitude;
            float frequence;
            float normalized = PatienceNormalisée;

            if (normalized > seuilMoyenne)
            {
                amplitude = TREMBLE_VERT_AMPLITUDE;
                frequence = TREMBLE_VERT_FREQUENCE;
            }
            else if (normalized > seuilBasse)
            {
                amplitude = TREMBLE_ORANGE_AMPLITUDE;
                frequence = TREMBLE_ORANGE_FREQUENCE;
            }
            else
            {
                amplitude = TREMBLE_ROUGE_AMPLITUDE;
                frequence = TREMBLE_ROUGE_FREQUENCE;
            }

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

            _patience = Mathf.Max(0f, _patience - POINTS_PENALITE);

            StartCoroutine(AnimerPerte());
            StartCoroutine(AnimerSecousse());
        }

        /// <summary>
        /// Ajoute directement un nombre de points à la patience, dans la limite de PATIENCE_MAX.
        /// Utilisé par l'effet Liasse de Billets.
        /// </summary>
        public void AjouterPatience(float points)
        {
            _patience = Mathf.Min(PATIENCE_MAX, _patience + points);
        }

        /// <summary>
        /// Gèle la barre : le décompte s'arrête, les pénalités sont ignorées
        /// et OnPatienceEpuisée ne peut plus se déclencher.
        /// Appeler dès que le barrage est validé par le joueur.
        /// </summary>
        public void Geler()
        {
            _gelée = true;
            // Stopper le tremblement passif en remettant la position à la base
            _rt.anchoredPosition = _positionBase;
            Debug.Log("[BarrePatience] Barre gelée — game over désactivé.");
        }

        /// <summary>
        /// Dégèle la barre si elle avait été gelée (pour une future réinitialisation de scène).
        /// </summary>
        public void Dégeler()
        {
            _gelée = false;
            Debug.Log("[BarrePatience] Barre dégelée.");
        }

        // ── Animations ────────────────────────────────────────────────────────

        private IEnumerator AnimerPerte()
        {
            _enPenalite = true;

            // Saut immédiat de la barre au nouveau niveau
            NotifierVisualisateur(PatienceNormalisée);

            // Flash rouge sur le visualisateur externe
            _visualisateur?.AnimerFlash(couleurFlash);

            // Flash rouge interne (Image shader) : apparition instantanée, disparition progressive
            if (_visualisateur == null && imageFlash != null)
            {
                imageFlash.color   = couleurFlash;
                imageFlash.enabled = true;

                float t = 0f;
                while (t < FLASH_DUREE)
                {
                    t += Time.deltaTime;
                    float alpha      = Mathf.Lerp(couleurFlash.a, 0f, Mathf.SmoothStep(0f, 1f, t / FLASH_DUREE));
                    imageFlash.color = new Color(couleurFlash.r, couleurFlash.g, couleurFlash.b, alpha);
                    yield return null;
                }

                imageFlash.enabled = false;
            }
            else
            {
                yield return new WaitForSeconds(FLASH_DUREE);
            }

            _enPenalite = false;
        }

        private IEnumerator AnimerSecousse()
        {
            float t = 0f;
            while (t < SHAKE_DUREE)
            {
                t += Time.deltaTime;
                float décroiss       = 1f - (t / SHAKE_DUREE);
                float offsetX        = Mathf.Sin(t * 65f) * SHAKE_AMPLITUDE * décroiss;
                _rt.anchoredPosition = _positionBase + new Vector2(offsetX, 0f);
                yield return null;
            }
            // Ne pas fixer à _positionBase : le tremblement passif reprend dans Update
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
