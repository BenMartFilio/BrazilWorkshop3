using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Affiche un slot d'icône de formulaire dans la prochaine demande de barrage.
    /// Utilise une RawImage pour afficher la texture extraite du prefab du formulaire,
    /// et un TextMeshProUGUI pour afficher le nombre requis (ex : ×2).
    /// L'apparition se fait par un scale-in animé depuis zéro.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class IconFormulaireUI : MonoBehaviour
    {
        [Tooltip("Texte affichant le nombre de formulaires requis de ce type (ex : ×2).")]
        [SerializeField] private TextMeshProUGUI texteQuantité;

        [Tooltip("Durée de l'animation d'apparition en secondes.")]
        [SerializeField] private float duréeApparition = 0.25f;

        private RawImage _rawImage;
        private Coroutine _animation;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            Masquer();
        }

        /// <summary>
        /// Affiche ce slot avec la texture du prefab formulaire et la quantité requise.
        /// Déclenche l'animation d'apparition (scale-in).
        /// </summary>
        public void Afficher(Texture texture, int quantité)
        {
            gameObject.SetActive(true);
            _rawImage.texture = texture;
            texteQuantité.text = quantité > 1 ? $"×{quantité}" : string.Empty;

            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimerApparition());
        }

        /// <summary>Masque et réinitialise ce slot.</summary>
        public void Masquer()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            gameObject.SetActive(false);
            transform.localScale = Vector3.zero;
        }

        private IEnumerator AnimerApparition()
        {
            transform.localScale = Vector3.zero;
            float t = 0f;

            while (t < duréeApparition)
            {
                t += Time.deltaTime;
                float ratio = Mathf.SmoothStep(0f, 1f, t / duréeApparition);
                transform.localScale = Vector3.one * ratio;
                yield return null;
            }

            transform.localScale = Vector3.one;
            _animation = null;
        }
    }
}
