using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Gère l'apparition et l'action du bouton de retour vers MapRoad.
    /// Le bouton est masqué par défaut ; <see cref="AfficherApresDelai"/> l'anime
    /// en fondu après un délai une fois la prochaine demande de formulaire affichée.
    /// Cliquer sur le bouton appelle <see cref="SessionManager.RetournerAMapRoad"/>.
    /// </summary>
    public class BoutonRetourMapRoad : MonoBehaviour
    {
        [Header("Composants")]
        [Tooltip("Le bouton UI à afficher. Si non assigné, le composant Button sur ce GameObject est utilisé.")]
        [SerializeField] private Button bouton;

        [Tooltip("CanvasGroup utilisé pour le fondu d'apparition. Si non assigné, un CanvasGroup est ajouté automatiquement.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [Tooltip("Durée du fondu d'apparition du bouton (secondes).")]
        [SerializeField, Min(0f)] private float duréeFondu = 0.4f;

        [SerializeField] private AudioEventDispatcher audioEventDispatcher;
        [SerializeField] private AudioType _ClickSound;    

        private Coroutine _coroutine;

        private void Awake()
        {
            if (bouton == null)
                bouton = GetComponent<Button>();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Masqué et non interactable au démarrage.
            MasquerImmédiatement();

            bouton.onClick.AddListener(OnClic);
        }

        private void OnDestroy()
        {
            bouton.onClick.RemoveListener(OnClic);
        }

        /// <summary>
        /// Lance le chrono puis fait apparaître le bouton en fondu.
        /// Peut être appelé plusieurs fois — annule le chrono précédent.
        /// </summary>
        /// <param name="délai">Délai avant apparition (secondes).</param>
        public void AfficherApresDelai(float délai)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(AttendreEtAfficher(délai));
        }

        /// <summary>Masque et désactive le bouton immédiatement sans animation.</summary>
        public void Masquer()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            MasquerImmédiatement();
        }

        // ── Privé ────────────────────────────────────────────────────────────────

        private IEnumerator AttendreEtAfficher(float délai)
        {
            yield return new WaitForSeconds(délai);

            // Fondu 0 → 1.
            float t = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;

            while (t < duréeFondu)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / duréeFondu);
                yield return null;
            }

            canvasGroup.alpha          = 1f;
            canvasGroup.interactable   = true;
            canvasGroup.blocksRaycasts = true;
            _coroutine = null;
        }

        private void MasquerImmédiatement()
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnClic()
        {
            if(audioEventDispatcher != null) audioEventDispatcher.PlayAudio(_ClickSound);
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.RetournerAMapRoad();
            }
            else
            {
                Debug.LogError("[BoutonRetourMapRoad] SessionManager introuvable — retour MapRoad annulé.");
            }
        }
    }
}
