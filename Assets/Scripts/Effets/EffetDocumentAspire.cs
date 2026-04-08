using System;
using System.Collections;
using Barrage.Formulaires;
using UnityEngine;

namespace Barrage.Effets
{
    /// <summary>
    /// Anime un document qui part de la voiture colorée (source ChangeSkin)
    /// et file rapidement vers le véhicule du joueur au moment de l'aspiration.
    /// Le sprite affiché correspond au formulaire réellement collecté.
    ///
    /// Placez ce composant sur le même GameObject qu'<see cref="Aspiration"/>
    /// et assignez les références depuis l'Inspector.
    /// </summary>
    public class EffetDocumentAspire : MonoBehaviour
    {
        // ── Références ─────────────────────────────────────────────────────────
        [Header("Références")]
        [Tooltip("Transform du véhicule du joueur (destination du document).")]
        [SerializeField] private Transform _joueur;

        [Tooltip("Prefab du document volant. Doit contenir un SpriteRenderer en racine.")]
        [SerializeField] private GameObject _prefabDocument;

        [Tooltip("Association FormulaireType → Sprite affiché pendant le vol.")]
        [SerializeField] private SpriteParType[] _spritesParType;

        // ── Paramètres de vol ──────────────────────────────────────────────────
        [Header("Paramètres de vol")]
        [Tooltip("Durée en secondes du trajet document → joueur.")]
        [SerializeField] private float _dureeVol = 0.22f;

        [Tooltip("Hauteur maximale de l'arc parabolique (en unités monde). 0 = trajet linéaire.")]
        [SerializeField] private float _hauteurArc = 1.2f;

        [Tooltip("Scale initial du sprite de document.")]
        [SerializeField] private float _scaleDepart = 0.6f;

        [Tooltip("Scale final du document en arrivant sur le joueur.")]
        [SerializeField] private float _scaleArrivee = 0.15f;

        [Tooltip("Rotation en degrés/s appliquée au document pendant le vol.")]
        [SerializeField] private float _vitesseRotation = 360f;

        [Tooltip("Sorting layer sur lequel rendre le document en vol.")]
        [SerializeField] private string _sortingLayer = "Player";

        [Tooltip("Sorting order du document en vol.")]
        [SerializeField] private int _sortingOrder = 20;

        // ── État ───────────────────────────────────────────────────────────────
        private Aspiration _aspiration;

        // ── Structures de données ──────────────────────────────────────────────

        [Serializable]
        public struct SpriteParType
        {
            public FormulaireType type;
            public Sprite sprite;
        }

        // ── Cycle de vie ────────────────────────────────────────────────────────

        private void Awake()
        {
            _aspiration = GetComponent<Aspiration>();
            if (_aspiration == null)
                Debug.LogError("[EffetDocumentAspire] Aucun composant Aspiration trouvé sur ce GameObject.", this);
        }

        private void OnEnable()
        {
            Aspiration.OnDocumentCollected += GérerCollecte;
        }

        private void OnDisable()
        {
            Aspiration.OnDocumentCollected -= GérerCollecte;
        }

        // ── Déclenchement ──────────────────────────────────────────────────────

        /// <summary>
        /// Appelé dès qu'un document est aspiré avec succès.
        /// Récupère la position de la voiture source et le sprite correspondant au type collecté.
        /// </summary>
        private void GérerCollecte(FormulaireType type)
        {
            if (_aspiration == null || _joueur == null || _prefabDocument == null) return;

            Transform source = _aspiration.ObtenirSourceActuelle();
            if (source == null) return;

            Sprite sprite = TrouverSprite(type);
            StartCoroutine(AnimerVol(source.position, sprite));
        }

        // ── Animation ──────────────────────────────────────────────────────────

        private IEnumerator AnimerVol(Vector3 posDepart, Sprite sprite)
        {
            GameObject instance = Instantiate(_prefabDocument, posDepart, Quaternion.identity);
            instance.transform.localScale = Vector3.one * _scaleDepart;

            SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (sprite != null)
                    sr.sprite = sprite;
                sr.color            = Color.white;
                sr.sortingLayerName = _sortingLayer;
                sr.sortingOrder     = _sortingOrder;
            }

            float elapsed = 0f;

            while (elapsed < _dureeVol)
            {
                if (instance == null) yield break;
                if (_joueur == null)  { Destroy(instance); yield break; }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _dureeVol);

                // Position : arc parabolique entre départ et joueur.
                Vector3 destination = _joueur.position;
                Vector3 posLerp     = Vector3.Lerp(posDepart, destination, t);
                float   arc         = _hauteurArc * 4f * t * (1f - t);
                instance.transform.position = new Vector3(posLerp.x, posLerp.y + arc, posDepart.z);

                // Scale : décroît progressivement.
                float scale = Mathf.Lerp(_scaleDepart, _scaleArrivee, t);
                instance.transform.localScale = Vector3.one * scale;

                // Rotation continue.
                instance.transform.Rotate(0f, 0f, _vitesseRotation * Time.deltaTime, Space.Self);

                yield return null;
            }

            if (instance != null)
                Destroy(instance);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne le sprite associé au FormulaireType donné, ou null si non configuré.
        /// </summary>
        private Sprite TrouverSprite(FormulaireType type)
        {
            if (_spritesParType == null) return null;

            foreach (SpriteParType entry in _spritesParType)
            {
                if (entry.type == type)
                    return entry.sprite;
            }

            Debug.LogWarning($"[EffetDocumentAspire] Aucun sprite configuré pour {type}.", this);
            return null;
        }
    }
}
