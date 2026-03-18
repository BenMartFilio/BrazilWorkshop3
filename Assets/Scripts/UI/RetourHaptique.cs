using System.Collections;
using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Utilitaire de retour haptique pour Android et iOS.
    /// Utilise Handheld.Vibrate() (court, unique) ou un pattern personnalisé
    /// via une coroutine pour simuler des impulsions multiples sur Android.
    ///
    /// Appelez les méthodes statiques depuis n'importe quelle classe.
    /// Les méthodes à coroutine nécessitent un MonoBehaviour hôte.
    /// </summary>
    public class RetourHaptique : MonoBehaviour
    {
        // ── Singleton léger ──────────────────────────────────────────────────
        private static RetourHaptique _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Vibration courte et unique — erreur légère.
        /// Compatible Android et iOS.
        /// </summary>
        public static void VibrerCourt()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Double impulsion — erreur marquée (ex. mauvais formulaire remis au garde).
        /// Simule deux courtes vibrations espacées.
        /// </summary>
        public static void VibrerDoubleImpulsion()
        {
            if (_instance == null)
            {
                Debug.LogWarning("[RetourHaptique] Aucune instance active — impossible de lancer la coroutine.");
                VibrerCourt();
                return;
            }

            _instance.StartCoroutine(_instance.PatternDoubleImpulsion());
        }

        // ── Patterns ─────────────────────────────────────────────────────────

        private IEnumerator PatternDoubleImpulsion()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            yield return new WaitForSecondsRealtime(0.12f);
            Handheld.Vibrate();
#else
            yield return null;
#endif
        }
    }
}
