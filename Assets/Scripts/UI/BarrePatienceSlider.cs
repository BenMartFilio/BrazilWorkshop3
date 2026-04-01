using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Visualisateur de la barre de patience basé sur UnityEngine.UI.Slider.
    /// Implémente IVisualisateurPatience — à assigner dans le champ
    /// "Visualisateur Externe" de BarrePatience.
    ///
    /// Fonctionnement :
    ///   - La valeur du Slider suit PatienceNormalisée en temps réel.
    ///   - La couleur de la zone de remplissage change selon les seuils
    ///     (vert → orange → rouge), identique à l'ancien système shader.
    ///   - Un flash sur l'image de remplissage signale chaque pénalité.
    ///   - Le tremblement passif est géré par BarrePatience (même RectTransform).
    ///
    /// Setup :
    ///   1. Créer un Slider Unity dans le Canvas de la scène Barrage.
    ///   2. Attacher ce composant sur le même GameObject que le Slider.
    ///   3. Assigner ce composant dans BarrePatience → Visualisateur Externe.
    ///   4. Optionnel : assigner imageFlash (Image enfant transparente par-dessus le fill).
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class BarrePatienceSlider : MonoBehaviour, IVisualisateurPatience
    {
        private const float FLASH_DUREE = 0.45f; // s — durée du flash de pénalité

        [Header("Références Slider")]
        [Tooltip("Image de remplissage du Slider dont la couleur change selon la patience. " +
                 "Laissez vide pour la détecter automatiquement depuis le Slider.")]
        [SerializeField] private Image imageFill;

        [Header("Flash de pénalité")]
        [Tooltip("Image transparente superposée au fill pour le flash rouge. " +
                 "Laissez vide pour créer automatiquement un enfant.")]
        [SerializeField] private Image imageFlash;

        private Slider    _slider;
        private Coroutine _flashCoroutine;

        private void Awake()
        {
            _slider = GetComponent<Slider>();

            // Le Slider ne doit pas être interactable : c'est un indicateur, pas un contrôle.
            _slider.interactable = false;
            _slider.minValue     = 0f;
            _slider.maxValue     = 1f;
            _slider.value        = 1f;

            // Auto-détection de l'image fill depuis la hiérarchie du Slider
            if (imageFill == null && _slider.fillRect != null)
                imageFill = _slider.fillRect.GetComponent<Image>();

            // Création automatique du flash si absent
            if (imageFlash == null)
                imageFlash = CreerImageFlash();
        }

        // ── IVisualisateurPatience ────────────────────────────────────────────

        /// <summary>Met à jour la valeur du Slider et la couleur du fill selon les seuils.</summary>
        public void MettreAJour(float fillNormalisé,
                                float seuilMoyenne,
                                float seuilBasse,
                                Color couleurHaute,
                                Color couleurMoyenne,
                                Color couleurBasse)
        {
            _slider.value = fillNormalisé;

            if (imageFill == null) return;

            Color cible;
            if (fillNormalisé > seuilMoyenne)
                cible = couleurHaute;
            else if (fillNormalisé > seuilBasse)
                cible = couleurMoyenne;
            else
                cible = couleurBasse;

            imageFill.color = cible;
        }

        /// <summary>Déclenche un flash rouge sur l'image superposée.</summary>
        public void AnimerFlash(Color couleurFlash)
        {
            if (imageFlash == null) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(FlashCoroutine(couleurFlash));
        }

        // ── Flash ─────────────────────────────────────────────────────────────

        private IEnumerator FlashCoroutine(Color couleurFlash)
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

            imageFlash.enabled  = false;
            _flashCoroutine     = null;
        }

        // ── Génération automatique ────────────────────────────────────────────

        private Image CreerImageFlash()
        {
            var go = new GameObject("FlashSlider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.layer = LayerMask.NameToLayer("UI");

            var rt       = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img           = go.GetComponent<Image>();
            img.color         = new Color(1f, 0f, 0f, 0f); // transparent par défaut
            img.raycastTarget = false;
            img.enabled       = false;
            return img;
        }
    }
}
