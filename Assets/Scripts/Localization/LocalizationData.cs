using System;
using System.Collections.Generic;
using UnityEngine;

namespace Drakensland.Localization
{
    /// <summary>
    /// ScriptableObject holding all translations for a single locale.
    /// Create one asset per language via the Assets/Create menu.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationData_XX", menuName = "Drakensland/Localization/Localization Data")]
    public class LocalizationData : ScriptableObject
    {
        [Tooltip("BCP-47 language tag, e.g. 'fr', 'en', 'es', 'de'.")]
        public string languageCode;

        [Tooltip("Human-readable name shown in the locale selector.")]
        public string displayName;

        [Serializable]
        public struct Entry
        {
            public string key;
            [TextArea(1, 4)]
            public string value;
        }

        [SerializeField] private List<Entry> _entries = new();

        // Cache du dictionnaire (thread-safe via lock)
        private Dictionary<string, string> _lookup;
        private readonly object _lockObject = new object();

        // ── Properties ─────────────────────────────────────────────────────────

        /// <summary>Number of translation entries in this locale.</summary>
        public int EntryCount => _entries?.Count ?? 0;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the translated string for a given key.
        /// Falls back to the key itself if not found.
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            BuildLookupIfNeeded();

            lock (_lockObject)
            {
                return _lookup.TryGetValue(key, out string value) ? value : key;
            }
        }

        /// <summary>Returns true if the given key exists in this locale.</summary>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            BuildLookupIfNeeded();

            lock (_lockObject)
            {
                return _lookup.ContainsKey(key);
            }
        }

        /// <summary>Forces a rebuild of the internal lookup (useful after runtime edits).</summary>
        public void InvalidateCache()
        {
            lock (_lockObject)
            {
                _lookup = null;
            }
        }

        /// <summary>Returns all keys in this locale (for debugging/editor tools).</summary>
        public IEnumerable<string> GetAllKeys()
        {
            BuildLookupIfNeeded();

            lock (_lockObject)
            {
                return _lookup.Keys;
            }
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void BuildLookupIfNeeded()
        {
            lock (_lockObject)
            {
                if (_lookup != null) return;

                _lookup = new Dictionary<string, string>(_entries?.Count ?? 0);

                if (_entries == null || _entries.Count == 0)
                {
                    Debug.LogWarning($"[LocalizationData] '{name}' has no entries!", this);
                    return;
                }

                int duplicateCount = 0;
                foreach (Entry entry in _entries)
                {
                    if (string.IsNullOrEmpty(entry.key))
                    {
                        Debug.LogWarning($"[LocalizationData] Empty key found in '{name}'", this);
                        continue;
                    }

                    if (_lookup.ContainsKey(entry.key))
                    {
                        Debug.LogWarning($"[LocalizationData] Duplicate key '{entry.key}' in '{name}'. Using first occurrence.", this);
                        duplicateCount++;
                        continue;
                    }

                    _lookup[entry.key] = entry.value ?? string.Empty;
                }

                if (duplicateCount > 0)
                {
                    Debug.LogWarning($"[LocalizationData] '{name}' contains {duplicateCount} duplicate key(s).", this);
                }

                Debug.Log($"[LocalizationData] Built lookup for '{languageCode}' with {_lookup.Count} entries.");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InvalidateCache();

            // Validation du language code
            if (string.IsNullOrEmpty(languageCode))
            {
                Debug.LogWarning($"[LocalizationData] Language code is empty on '{name}'", this);
            }
        }

        [ContextMenu("Rebuild Lookup")]
        private void RebuildLookup()
        {
            InvalidateCache();
            BuildLookupIfNeeded();
        }

        [ContextMenu("Log All Keys")]
        private void LogAllKeys()
        {
            BuildLookupIfNeeded();
            Debug.Log($"[LocalizationData] '{languageCode}' contains {EntryCount} keys:\n" + string.Join(", ", GetAllKeys()));
        }

        [ContextMenu("Find Duplicate Keys")]
        private void FindDuplicates()
        {
            HashSet<string> seen = new HashSet<string>();
            List<string> duplicates = new List<string>();

            foreach (Entry entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;

                if (!seen.Add(entry.key))
                {
                    duplicates.Add(entry.key);
                }
            }

            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"[LocalizationData] Found {duplicates.Count} duplicate key(s): {string.Join(", ", duplicates)}", this);
            }
            else
            {
                Debug.Log($"[LocalizationData] No duplicates found in '{name}'.");
            }
        }
#endif
    }
}