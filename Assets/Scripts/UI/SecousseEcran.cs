using System.Collections;
using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Secoue le Transform cible (Canvas racine ou tout autre objet) via bruit de Perlin.
    /// Fonctionne avec un Canvas en mode Screen Space - Overlay où secouer la caméra
    /// n'aurait aucun effet sur l'UI.
    /// </summary>
    public class SecousseEcran : MonoBehaviour
    {
        // ── Paramètres par défaut ────────────────────────────────────────────
        private const float DUREE_DEFAUT     = 0.40f;  // s
        private const float INTENSITE_DEFAUT = 18f;    // pixels en UV-space Canvas
        private const float FREQUENCE        = 22f;    // vitesse du bruit de Perlin

        [Tooltip("Transform à secouer (Canvas racine de préférence). Laissez vide pour utiliser ce GameObject.")]
        [SerializeField] private Transform cible;

        [Tooltip("Durée de la secousse en secondes.")]
        [SerializeField] private float durée = DUREE_DEFAUT;

        [Tooltip("Intensité maximale du déplacement en pixels.")]
        [SerializeField] private float intensité = INTENSITE_DEFAUT;

        private Vector3    _positionBase;
        private Coroutine  _coroutine;

        private void Awake()
        {
            if (cible == null)
                cible = transform;
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Déclenche une secousse avec les paramètres configurés dans l'Inspector.
        /// Si une secousse est déjà en cours, elle est réinitialisée.
        /// </summary>
        public void Secouer()
            => Secouer(durée, intensité);

        /// <summary>
        /// Déclenche une secousse avec des paramètres personnalisés.
        /// </summary>
        public void Secouer(float duréeS, float intensitéPx)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                cible.localPosition = _positionBase;
            }

            _coroutine = StartCoroutine(CoroutineSecousse(duréeS, intensitéPx));
        }

        // ── Coroutine ─────────────────────────────────────────────────────────

        private IEnumerator CoroutineSecousse(float duréeS, float intensitéPx)
        {
            _positionBase = cible.localPosition;

            // Normaliser l'intensité par rapport à la hauteur de référence 1920px
            // pour que l'effet soit identique sur tous les formats d'écran.
            float scale     = Screen.height / 1920f;
            float intensité = intensitéPx * scale;

            // Offset aléatoire dans l'espace Perlin pour éviter les répétitions
            float offsetX = Random.Range(0f, 100f);
            float offsetY = Random.Range(0f, 100f);

            float t = 0f;
            while (t < duréeS)
            {
                t += Time.deltaTime;

                // Décroissance exponentielle → secousse qui s'amortit naturellement
                float décroiss = 1f - Mathf.SmoothStep(0f, 1f, t / duréeS);

                // Bruit de Perlin recentré sur [-1, 1]
                float dx = (Mathf.PerlinNoise(offsetX + t * FREQUENCE, 0f) - 0.5f) * 2f;
                float dy = (Mathf.PerlinNoise(0f, offsetY + t * FREQUENCE) - 0.5f) * 2f;

                cible.localPosition = _positionBase + new Vector3(dx, dy, 0f) * intensité * décroiss;

                yield return null;
            }

            cible.localPosition = _positionBase;
            _coroutine = null;
        }
    }
}
