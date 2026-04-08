using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Drakensland.Localization
{
    /// <summary>
    /// Singleton that manages the active locale and exposes translation lookups.
    /// On startup, detects the device locale and loads the best matching <see cref="LocalizationData"/>.
    /// Falls back to the default locale when no match is found.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        private static LocalizationManager _instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<LocalizationManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[LocalizationManager] No instance found in scene! Add LocalizationManager to your persistent scene.");
                    }
                }
                return _instance;
            }
        }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Locales")]
        [Tooltip("All supported LocalizationData assets. The first one is the default fallback.")]
        [SerializeField] private List<LocalizationData> _supportedLocales = new();

        [Header("Settings")]
        [Tooltip("PlayerPrefs key used to persist a manually chosen language.")]
        [SerializeField] private string _prefsKey = "SelectedLanguage";

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the active locale changes. Subscribe to refresh UI text.</summary>
        public static event Action OnLocaleChanged;

        // ── State ──────────────────────────────────────────────────────────────

        private LocalizationData _activeLocale;
        private LocalizationData _fallbackLocale;
        private bool _isInitialized = false;

        // ── Properties ─────────────────────────────────────────────────────────

        /// <summary>The currently active locale asset.</summary>
        public LocalizationData ActiveLocale => _activeLocale;

        /// <summary>All supported locale assets provided to this manager.</summary>
        public IReadOnlyList<LocalizationData> SupportedLocales => _supportedLocales;

        /// <summary>Returns true if the manager is ready to translate keys.</summary>
        public bool IsInitialized => _isInitialized;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // Singleton pattern avec destruction des duplicatas
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                OnLocaleChanged = null; // Nettoyage des événements
            }
        }

        // ── Initialization ─────────────────────────────────────────────────────

        private void Initialize()
        {
            if (_supportedLocales == null || _supportedLocales.Count == 0)
            {
                Debug.LogError("[LocalizationManager] No locales configured. Assign LocalizationData assets in the Inspector.");
                return;
            }

            // Validation des locales
            for (int i = 0; i < _supportedLocales.Count; i++)
            {
                if (_supportedLocales[i] == null)
                {
                    Debug.LogError($"[LocalizationManager] Locale at index {i} is null!");
                    return;
                }
            }

            _fallbackLocale = _supportedLocales[0];
            LoadInitialLocale();
            _isInitialized = true;

            Debug.Log($"[LocalizationManager] Initialized with {_supportedLocales.Count} locale(s). Active: '{_activeLocale.languageCode}'");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the translated string for <paramref name="key"/> in the active locale.
        /// Falls back to the default locale, then returns the key itself if nothing is found.
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[LocalizationManager] Attempted to get translation for null or empty key.");
                return string.Empty;
            }

            if (!_isInitialized)
            {
                Debug.LogWarning($"[LocalizationManager] Not initialized yet. Returning key: '{key}'");
                return key;
            }

            // Recherche dans la locale active
            if (_activeLocale != null && _activeLocale.HasKey(key))
                return _activeLocale.Get(key);

            // Fallback sur la locale par défaut
            if (_fallbackLocale != null && _fallbackLocale.HasKey(key))
            {
                Debug.LogWarning($"[LocalizationManager] Key '{key}' not found in active locale '{_activeLocale?.languageCode}', using fallback.");
                return _fallbackLocale.Get(key);
            }

            Debug.LogWarning($"[LocalizationManager] Missing translation key: '{key}' in all locales.");
            return key;
        }

        /// <summary>
        /// Switches the active locale to the one matching <paramref name="languageCode"/>.
        /// Persists the choice in PlayerPrefs and raises <see cref="OnLocaleChanged"/>.
        /// </summary>
        public void SetLocale(string languageCode)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[LocalizationManager] Cannot set locale before initialization.");
                return;
            }

            LocalizationData match = FindLocale(languageCode);
            if (match == null)
            {
                Debug.LogWarning($"[LocalizationManager] Locale '{languageCode}' not found. Using fallback.");
                match = _fallbackLocale;
            }

            ApplyLocale(match, persist: true);
        }

        /// <summary>Forces a reload from the device locale, ignoring any saved preference.</summary>
        public void ResetToDeviceLocale()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[LocalizationManager] Cannot reset locale before initialization.");
                return;
            }

            PlayerPrefs.DeleteKey(_prefsKey);
            PlayerPrefs.Save();
            LoadInitialLocale();
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void LoadInitialLocale()
        {
            // 1. Saved preference
            if (PlayerPrefs.HasKey(_prefsKey))
            {
                string savedCode = PlayerPrefs.GetString(_prefsKey);
                LocalizationData saved = FindLocale(savedCode);
                if (saved != null)
                {
                    ApplyLocale(saved, persist: false);
                    return;
                }
            }

            // 2. Device locale
            string deviceCode = GetDeviceLanguageCode();
            LocalizationData deviceMatch = FindLocale(deviceCode);
            if (deviceMatch != null)
            {
                ApplyLocale(deviceMatch, persist: false);
                return;
            }

            // 3. Fallback
            ApplyLocale(_fallbackLocale, persist: false);
        }

        private void ApplyLocale(LocalizationData locale, bool persist)
        {
            if (locale == null)
            {
                Debug.LogError("[LocalizationManager] Attempted to apply null locale.");
                return;
            }

            bool hasChanged = _activeLocale != locale;
            _activeLocale = locale;

            if (persist)
            {
                PlayerPrefs.SetString(_prefsKey, locale.languageCode);
                PlayerPrefs.Save();
            }

            Debug.Log($"[LocalizationManager] Locale set to '{locale.languageCode}' ({locale.displayName}).");

            // Ne déclencher l'événement que si la locale a réellement changé
            if (hasChanged)
            {
                OnLocaleChanged?.Invoke();
            }

            if (persist)
            {
                try
                {
                    PlayerPrefs.SetString(_prefsKey, locale.languageCode);
                    PlayerPrefs.Save();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LocalizationManager] Failed to save locale preference: {ex.Message}");
                }
            }
        }

        private LocalizationData FindLocale(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;

            // Exact match first (e.g. "fr-CA" == "fr-CA")
            foreach (LocalizationData locale in _supportedLocales)
            {
                if (string.Equals(locale.languageCode, code, StringComparison.OrdinalIgnoreCase))
                    return locale;
            }

            // Primary-language match (e.g. "fr-CA" matches "fr")
            string primary = code.Split('-')[0];
            foreach (LocalizationData locale in _supportedLocales)
            {
                string localePrimary = locale.languageCode.Split('-')[0];
                if (string.Equals(localePrimary, primary, StringComparison.OrdinalIgnoreCase))
                    return locale;
            }

            return null;
        }

        private static string GetDeviceLanguageCode()
        {
            try
            {
#if UNITY_ANDROID || UNITY_IOS
                // Sur mobile, préférer Application.systemLanguage car plus fiable
                SystemLanguage lang = Application.systemLanguage;

                // Mapping optimisé pour mobile
                string code = lang switch
                {
                    SystemLanguage.French => "fr",
                    SystemLanguage.English => "en",
                    SystemLanguage.Spanish => "es",
                    SystemLanguage.German => "de",
                    SystemLanguage.Italian => "it",
                    SystemLanguage.Portuguese => "pt",
                    SystemLanguage.Russian => "ru",
                    SystemLanguage.ChineseSimplified => "zh-CN",
                    SystemLanguage.ChineseTraditional => "zh-TW",
                    SystemLanguage.Japanese => "ja",
                    SystemLanguage.Korean => "ko",
                    SystemLanguage.Arabic => "ar",
                    SystemLanguage.Dutch => "nl",
                    SystemLanguage.Polish => "pl",
                    SystemLanguage.Turkish => "tr",
                    SystemLanguage.Swedish => "sv",
                    SystemLanguage.Danish => "da",
                    SystemLanguage.Norwegian => "no",
                    SystemLanguage.Finnish => "fi",
                    SystemLanguage.Greek => "el",
                    SystemLanguage.Hebrew => "he",
                    SystemLanguage.Thai => "th",
                    SystemLanguage.Vietnamese => "vi",
                    SystemLanguage.Indonesian => "id",
                    SystemLanguage.Ukrainian => "uk",
                    SystemLanguage.Czech => "cs",
                    SystemLanguage.Hungarian => "hu",
                    SystemLanguage.Romanian => "ro",
                    _ => "en" // Fallback to English
                };

                Debug.Log($"[LocalizationManager] Mobile language detected: {lang} → {code}");
                return code;

#else
        // Sur desktop/editor, utiliser CultureInfo
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (!string.IsNullOrEmpty(code))
        {
            Debug.Log($"[LocalizationManager] Desktop language detected: {code}");
            return code.ToLowerInvariant();
        }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationManager] Language detection failed: {ex.Message}");
            }

            // Ultimate fallback
            return "en";
        }

#if UNITY_EDITOR
        [ContextMenu("Log Current Locale")]
        private void LogCurrentLocale()
        {
            if (_activeLocale != null)
                Debug.Log($"Active Locale: {_activeLocale.languageCode} ({_activeLocale.displayName})");
            else
                Debug.Log("No active locale.");
        }

        [ContextMenu("Log Device Language")]
        private void LogDeviceLanguage()
        {
            Debug.Log($"Device Language Code: {GetDeviceLanguageCode()}");
        }
#endif
    }
}