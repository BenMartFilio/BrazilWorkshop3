using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Point d'entrée unique pour le déclenchement du Game Over.
    /// Deux modes sélectionnables depuis l'Inspector :
    ///
    ///   ☑ <b>Utiliser Animation Complète</b> (case cochée)
    ///     → Délègue à <see cref="AnimationGameOver.Déclencher"/> :
    ///       cartes qui glissent, tampons GAME / OVER, effets visuels complets.
    ///
    ///   ☐ <b>Illustration simple</b> (case décochée)
    ///     → Affiche <see cref="textureGameOver"/> par-dessus tout le reste
    ///       dans un Canvas ScreenSpace-Overlay avec sortingOrder élevé,
    ///       avec fondu d'entrée configurable.
    ///
    /// IMPORTANT : une fois ce composant en place, laissez le champ
    /// "Barre Patience" de l'<see cref="AnimationGameOver"/> à <c>null</c>
    /// pour éviter un double-déclenchement. Ce composant gère lui-même
    /// la subscription à <see cref="BarrePatience.OnPatienceEpuisée"/>.
    /// </summary>
    public class GestionnaireGameOver : MonoBehaviour
    {
        // ── Mode ──────────────────────────────────────────────────────────────
        [Header("Mode")]
        [Tooltip("☑  Coché   → Animation complète (cartes + tampons GAME OVER).\n" +
                 "☐  Décoché → Illustration simple (texture plein écran par-dessus tout).")]
        [SerializeField] private bool utiliserAnimationComplète = true;

        // ── Références communes ───────────────────────────────────────────────
        [Header("Références communes")]
        [Tooltip("BarrePatience dont OnPatienceEpuisée déclenche le game over.")]
        [SerializeField] private BarrePatience barrePatience;

        // ── Mode animation complète ───────────────────────────────────────────
        [Header("Animation complète (si case cochée)")]
        [Tooltip("Composant AnimationGameOver à déléguer.\n" +
                 "Laissez son propre champ 'Barre Patience' à null.")]
        [SerializeField] private AnimationGameOver animationGameOver;

        // ── Mode illustration simple ──────────────────────────────────────────
        [Header("Illustration simple (si case décochée)")]
        [Tooltip("Texture affichée en plein écran par-dessus tout le reste.")]
        [SerializeField] private Texture2D textureGameOver;

        [Tooltip("Durée du fondu d'entrée en secondes (0 = instantané).")]
        [SerializeField, Min(0f)] private float duréeFondu = 0.8f;

        [Tooltip("Ordre de rendu du Canvas overlay. " +
                 "Doit être supérieur à celui de tous les Canvas déjà présents dans la scène.")]
        [SerializeField] private int sortingOrderOverlay = 200;

        [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
        [SerializeField] private AudioType _gameOverSound;

        [SerializeField] private AudioSource _tictac;


        // ── État interne ──────────────────────────────────────────────────────
        private Canvas   _overlayCanvas;
        private RawImage _overlayImage;
        private bool     _déclenché;

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Déclenche le game over manuellement (par code).
        /// Équivalent à ce que fait <see cref="BarrePatience.OnPatienceEpuisée"/>.
        /// </summary>
        public void Déclencher()
        {
            if (_déclenché) return;
            _déclenché = true;
            _tictac.Stop();

            SoundManager.Instance.PlayMusicWithLowPass(null);
            if (_audioEventDispatcher != null) _audioEventDispatcher.PlayAudio(_gameOverSound);

            if (utiliserAnimationComplète)
            {
                if (animationGameOver != null)
                    animationGameOver.Déclencher();
                else
                    Debug.LogError("[GestionnaireGameOver] 'Animation Game Over' n'est pas assigné " +
                                   "alors que le mode Animation Complète est activé.");
            }
            else
            {
                if (textureGameOver == null)
                {
                    Debug.LogError("[GestionnaireGameOver] 'Texture Game Over' n'est pas assignée " +
                                   "alors que le mode Illustration Simple est activé.");
                    return;
                }
                StartCoroutine(AfficherIllustration());
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (barrePatience != null)
                barrePatience.OnPatienceEpuisée += Déclencher;
        }

        private void OnDisable()
        {
            if (barrePatience != null)
                barrePatience.OnPatienceEpuisée -= Déclencher;
        }

        // ── Affichage de l'illustration ───────────────────────────────────────

        private IEnumerator AfficherIllustration()
        {
            AssurerOverlay();
            _overlayCanvas.gameObject.SetActive(true);

            if (duréeFondu <= 0f)
            {
                _overlayImage.color = Color.white;
                yield break;
            }

            float t = 0f;
            _overlayImage.color = new Color(1f, 1f, 1f, 0f);

            while (t < duréeFondu)
            {
                t += Time.deltaTime;
                _overlayImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / duréeFondu));
                yield return null;
            }

            _overlayImage.color = Color.white;
        }

        /// <summary>
        /// Crée le Canvas overlay la première fois qu'il est nécessaire.
        /// Le Canvas est placé à la racine de la scène pour garantir que le mode
        /// ScreenSpace-Overlay fonctionne correctement (pas de Canvas parent qui l'écraserait).
        /// </summary>
        private void AssurerOverlay()
        {
            if (_overlayCanvas != null) return;

            // ── Canvas plein écran au-dessus de tout ──────────────────────────
            var go = new GameObject("Overlay_GameOver");
            go.transform.SetParent(null, false);   // racine de la scène

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrderOverlay;

            // CanvasScaler optionnel — aucun scaling nécessaire en mode Overlay
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // ── Image plein écran étirée ──────────────────────────────────────
            var imgGO = new GameObject("Image_GameOver", typeof(RectTransform));
            imgGO.transform.SetParent(go.transform, false);

            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);

            var raw = imgGO.AddComponent<RawImage>();
            raw.texture = textureGameOver;
            raw.color   = new Color(1f, 1f, 1f, 0f); // transparent au départ

            // Conserver le ratio de la texture (EnvelopeParent = couvre tout l'écran)
            if (textureGameOver != null)
            {
                var fitter = imgGO.AddComponent<AspectRatioFitter>();
                fitter.aspectMode  = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)textureGameOver.width / textureGameOver.height;
            }

            _overlayCanvas = canvas;
            _overlayImage  = raw;

            // Caché jusqu'au déclenchement
            go.SetActive(false);
        }
    }
}
