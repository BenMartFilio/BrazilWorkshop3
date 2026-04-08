using TMPro;
using UnityEngine;

namespace Drakensland.Localization
{
    /// <summary>
    /// Attach to any GameObject with a <see cref="TextMeshProUGUI"/> or <see cref="TextMeshPro"/>
    /// to automatically display and refresh a localized string.
    /// The text is refreshed whenever <see cref="LocalizationManager.OnLocaleChanged"/> fires.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("The localization key to look up. See LocalizationKeys for all available keys.")]
        [SerializeField] private string _key;

        [Tooltip("Optional format arguments to inject with string.Format. Leave empty for plain keys.")]
        [SerializeField] private string[] _formatArgs;

        private TMP_Text _text;
        private bool _isSubscribed = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();

            if (_text == null)
            {
                Debug.LogError($"[LocalizedText] No TMP_Text component found on {gameObject.name}!", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Refresh initial au Start pour s'assurer que le LocalizationManager est initialisé
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Changes the key at runtime and refreshes the displayed text.
        /// </summary>
        public void SetKey(string key)
        {
            if (_key == key) return; // Évite un refresh inutile

            _key = key;
            Refresh();
        }

        /// <summary>
        /// Updates the format arguments and refreshes the displayed text.
        /// </summary>
        public void SetFormatArgs(params string[] args)
        {
            _formatArgs = args;
            Refresh();
        }

        /// <summary>
        /// Sets both key and format args, then refreshes once.
        /// </summary>
        public void SetKeyAndArgs(string key, params string[] args)
        {
            _key = key;
            _formatArgs = args;
            Refresh();
        }

        /// <summary>
        /// Manually forces a refresh of the localized text.
        /// </summary>
        public void ForceRefresh()
        {
            Refresh();
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void Subscribe()
        {
            if (_isSubscribed) return;

            LocalizationManager.OnLocaleChanged += Refresh;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;

            LocalizationManager.OnLocaleChanged -= Refresh;
            _isSubscribed = false;
        }

        private void Refresh()
        {
            // Vérifications de sécurité
            if (_text == null) return;
            if (string.IsNullOrEmpty(_key))
            {
                _text.text = string.Empty;
                return;
            }

            // Vérifier que le manager est disponible et initialisé
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning($"[LocalizedText] LocalizationManager instance not found. Key='{_key}' on {gameObject.name}", this);
                _text.text = _key; // Afficher la clé en fallback
                return;
            }

            if (!LocalizationManager.Instance.IsInitialized)
            {
                Debug.LogWarning($"[LocalizedText] LocalizationManager not initialized yet. Key='{_key}' on {gameObject.name}", this);
                _text.text = _key;
                return;
            }

            // Récupération de la traduction
            string translated = LocalizationManager.Instance.Get(_key);

            // Application du formatage si nécessaire
            if (_formatArgs != null && _formatArgs.Length > 0)
            {
                try
                {
                    translated = string.Format(translated, _formatArgs);
                }
                catch (System.FormatException ex)
                {
                    Debug.LogError($"[LocalizedText] Format error for key '{_key}' on {gameObject.name}: {ex.Message}", this);
                    // En cas d'erreur, afficher la traduction brute
                }
            }

            _text.text = translated;
        }

#if UNITY_EDITOR
        [ContextMenu("Preview Localized Text")]
        private void PreviewText()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            Refresh();
        }

        private void OnValidate()
        {
            // Auto-refresh dans l'éditeur quand on change la clé
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (Application.isPlaying && enabled)
            {
                Refresh();
            }
        }
#endif
    }
}