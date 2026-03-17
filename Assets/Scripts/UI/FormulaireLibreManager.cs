using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Système de formulaires libres dans la PartieBasse.
    /// Les cartes ont une taille fixe, se superposent partiellement en tas
    /// (disposition spirale de Fibonacci) et dérivent par inertie quand relâchées.
    /// </summary>
    public class FormulaireLibreManager : MonoBehaviour
    {
        [Header("Zones UI")]
        [Tooltip("RectTransform de la zone basse où les cartes sont déposées (Canvas propre).")]
        [SerializeField] private RectTransform partieBasse;
        [SerializeField] private MainDuGardeUI mainDuGarde;

        [Header("Apparence")]
        [Tooltip("Taille fixe de chaque carte en pixels.")]
        [SerializeField] private Vector2 tailleFixeCarte = new Vector2(189f, 336f);

        [Tooltip("Facteur d'écartement entre les cartes dans le tas (pixels par racine d'index).")]
        [SerializeField] private float facteurEcartement = 28f;

        [Tooltip("Amplitude maximale de la rotation aléatoire initiale (degrés).")]
        [SerializeField] private float rotationMax = 14f;

        [Header("Données")]
        [SerializeField] private FormulaireInventaire inventaire;
        [SerializeField] private List<FormulaireData> formulairesData = new();

        // Angle d'or en radians — donne une spirale de Fibonacci uniforme
        private const float ANGLE_OR = 2.39996323f;

        private readonly List<FormulaireLibre> _cartes = new();
        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            mainDuGarde.OnFormulaireRemis += OnFormulaireRemisAuGarde;
        }

        private void Start()
        {
            inventaire.InitialiserInventaire();
            SpawnToutesLesCartes();
        }

        private void OnDestroy()
        {
            if (mainDuGarde != null)
                mainDuGarde.OnFormulaireRemis -= OnFormulaireRemisAuGarde;
        }

        // ── Spawn ──────────────────────────────────────────────────────────────

        private void SpawnToutesLesCartes()
        {
            List<FormulaireType> liste = BuildListeInterleaved();

            foreach (var type in liste)
                SpawnCarte(type);

            PositionnerEnTas();
        }

        private void SpawnCarte(FormulaireType type)
        {
            if (!_dataParType.TryGetValue(type, out var data) || data.prefab == null)
            {
                Debug.LogWarning($"[FormulaireLibreManager] Aucun prefab pour : {type}");
                return;
            }

            // Lire la texture directement depuis le prefab (sans l'instancier dans la scène)
            RawImage sourceImage = data.prefab.GetComponentInChildren<RawImage>(true);
            Texture2D texture    = sourceImage != null ? sourceImage.texture as Texture2D : null;

            if (texture == null)
            {
                Debug.LogError($"[FormulaireLibreManager] Aucune texture dans '{data.prefab.name}'.");
                return;
            }

            // Créer un GameObject UI propre directement dans la PartieBasse
            var go = new GameObject(data.prefab.name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(partieBasse, false);
            go.layer = LayerMask.NameToLayer("UI");

            var img       = go.GetComponent<RawImage>();
            img.texture       = texture;
            img.raycastTarget = true;

            var carte = go.AddComponent<FormulaireLibre>();
            carte.Initialiser(type, this, partieBasse, tailleFixeCarte);

            _cartes.Add(carte);
        }

        /// <summary>Repositionne toutes les cartes en tas spirale de Fibonacci.</summary>
        private void PositionnerEnTas()
        {
            int n = _cartes.Count;
            for (int i = 0; i < n; i++)
            {
                // Spirale de Fibonacci : rayon croissant + angle d'or → jamais deux cartes superposées
                float rayon = facteurEcartement * Mathf.Sqrt(i);
                float angle = i * ANGLE_OR;

                Vector2 pos = new Vector2(Mathf.Cos(angle) * rayon, Mathf.Sin(angle) * rayon);
                float rotation  = UnityEngine.Random.Range(-rotationMax, rotationMax);

                var rt = _cartes[i].GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.localEulerAngles = new Vector3(0f, 0f, rotation);
            }
        }

        /// <summary>
        /// Construit la liste de types interleaved (types les plus fréquents distribués en premier)
        /// pour maximiser la variété visible dans le tas.
        /// </summary>
        private List<FormulaireType> BuildListeInterleaved()
        {
            var comptes = new Dictionary<FormulaireType, int>();
            foreach (FormulaireType type in Enum.GetValues(typeof(FormulaireType)))
                comptes[type] = inventaire.ObtenirQuantité(type);

            var résultat = new List<FormulaireType>();
            bool anyLeft = true;

            while (anyLeft)
            {
                anyLeft = false;
                var triés = comptes.OrderByDescending(kv => kv.Value).Select(kv => kv.Key);
                foreach (var type in triés)
                {
                    if (comptes[type] <= 0) continue;
                    résultat.Add(type);
                    comptes[type]--;
                    anyLeft = true;
                }
            }

            return résultat;
        }

        // ── Logique de jeu ─────────────────────────────────────────────────────

        private void OnFormulaireRemisAuGarde(FormulaireType type)
        {
            inventaire.Retirer(type, 1);
        }

        /// <summary>Soumet une carte à la main du garde, la retire de la liste et la détruit.</summary>
        public void EnvoyerAMainDuGarde(FormulaireLibre carte)
        {
            _cartes.Remove(carte);
            mainDuGarde.RecevoirFormulaire(carte);
        }

        // ── Helpers de détection ───────────────────────────────────────────────

        /// <summary>Retourne true si la position écran est dans la zone de la main du garde.</summary>
        public bool EstSurMainDuGarde(Vector2 screenPos, Camera cam)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                mainDuGarde.RectTransform, screenPos, cam);
        }
    }
}
