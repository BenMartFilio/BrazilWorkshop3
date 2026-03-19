using System;
using System.Collections;
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
        [Tooltip("RectTransform de la zone basse où les cartes vivent au repos.")]
        [SerializeField] private RectTransform partieBasse;
        [Tooltip("RectTransform de la couche de glissement (Canvas root) utilisée pendant le drag.")]
        [SerializeField] private RectTransform coucheGlissement;
        [SerializeField] private MainDuGardeUI mainDuGarde;
        [SerializeField] private BarrePatience barrePatience;

        [Header("Feedbacks")]
        [Tooltip("Composant de secousse de l'écran (placé sur le Canvas racine ou un parent).")]
        [SerializeField] private SecousseEcran secousseEcran;
        [Tooltip("Composant de retour haptique (peut être sur n'importe quel GameObject actif).")]
        [SerializeField] private RetourHaptique retourHaptique;

        [Tooltip("Durée de la secousse des formulaires (doit correspondre à celle de SecousseEcran).")]
        [SerializeField] private float duréeSecousseCartes = 0.40f;
        [Tooltip("Intensité de la secousse des formulaires en pixels.")]
        [SerializeField] private float intensitéSecousseCartes = 6f;

        [Header("Apparence")]
        [Tooltip("Taille fixe de chaque carte en pixels.")]
        [SerializeField] private Vector2 tailleFixeCarte = new Vector2(189f, 336f);

        [Tooltip("Amplitude maximale de la rotation aléatoire initiale (degrés).")]
        [SerializeField] private float rotationMax = 14f;

        [Tooltip("Marge en pixels par rapport aux bords de la partieBasse pour le spawn des cartes.")]
        [SerializeField] private float margeSpawnBords = 20f;

        [Tooltip("Vitesse horizontale initiale maximale aléatoire (px/s) pour disperser les cartes au spawn.")]
        [SerializeField] private float vitesseSpawnMax = 80f;

        [Header("Données")]
        [SerializeField] private FormulaireInventaire inventaire;
        [SerializeField] private List<FormulaireData> formulairesData = new();

        // ── Collision inter-cartes ────────────────────────────────────────────
        // Zone de collision = centre de la carte, avec 30 % de marge sur chaque bord.
        // → collisionHalfSize = tailleFixeCarte * 0.5 * (1 - 0.30) = tailleFixeCarte * 0.35
        private const float MARGE_COLLISION      = 0.30f; // fraction de marge côté carte
        private const float RESTITUTION          = 0.5f;  // fraction de l'overlap corrigée par itération
        private const int   ITERATIONS_COLLISION = 3;     // passes par LateUpdate

        private readonly List<FormulaireLibre> _cartes = new();
        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            mainDuGarde.OnFormulaireRemis    += OnFormulaireRemisAuGarde;
            mainDuGarde.OnFormulaireIncorrect += OnFormulaireIncorrect;
            mainDuGarde.OnBarrageValidé       += GriserToutesLesCartes;
        }

        private void Start()
        {
            inventaire.InitialiserInventaire();
            StartCoroutine(SpawnApresLayout());
        }

        /// <summary>
        /// Attend que le layout Canvas soit calculé avant de spawner et positionner les cartes,
        /// afin que partieBasse.rect retourne des dimensions réelles.
        /// </summary>
        private IEnumerator SpawnApresLayout()
        {
            // Attendre deux frames : le premier EndOfFrame initialise le Canvas,
            // le second garantit que les Canvas imbriqués (PartieBasse est un Canvas enfant)
            // ont bien propagé leurs dimensions.
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            SpawnToutesLesCartes();
        }

        private void OnDestroy()
        {
            if (mainDuGarde != null)
            {
                mainDuGarde.OnFormulaireRemis    -= OnFormulaireRemisAuGarde;
                mainDuGarde.OnFormulaireIncorrect -= OnFormulaireIncorrect;
                mainDuGarde.OnBarrageValidé       -= GriserToutesLesCartes;
            }
        }

        // ── Spawn ──────────────────────────────────────────────────────────────

        private void SpawnToutesLesCartes()
        {
            List<FormulaireType> liste = BuildListeInterleaved();

            foreach (var type in liste)
                SpawnCarte(type);

            PositionnerAléatoirement();
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
            carte.Initialiser(type, this, partieBasse, coucheGlissement, tailleFixeCarte);

            _cartes.Add(carte);
        }

        /// <summary>
        /// Positionne chaque carte à une position entièrement aléatoire dans les bounds de la partieBasse.
        /// Une vélocité horizontale initiale légère est appliquée pour briser la symétrie dès le début.
        /// La physique (gravité + collisions) prend ensuite le relais pour établir un tas naturel.
        /// </summary>
        private void PositionnerAléatoirement()
        {
            // GetLocalCorners retourne les 4 coins en espace local du RectTransform.
            // On utilise Min/Max sur tous les coins pour gérer les rects à hauteur négative
            // (PartieBasse est en stretch avec sizeDelta négatif, les coins sont inversés en Y).
            Vector3[] coins = new Vector3[4];
            partieBasse.GetLocalCorners(coins);

            float localXMin = Mathf.Min(coins[0].x, coins[1].x, coins[2].x, coins[3].x);
            float localXMax = Mathf.Max(coins[0].x, coins[1].x, coins[2].x, coins[3].x);
            float localYMin = Mathf.Min(coins[0].y, coins[1].y, coins[2].y, coins[3].y);
            float localYMax = Mathf.Max(coins[0].y, coins[1].y, coins[2].y, coins[3].y);

            float hw = tailleFixeCarte.x * 0.5f;
            float hh = tailleFixeCarte.y * 0.5f;

            float xMin = localXMin + hw + margeSpawnBords;
            float xMax = localXMax - hw - margeSpawnBords;
            float yMin = localYMin + hh + margeSpawnBords;
            float yMax = localYMax - hh - margeSpawnBords;

            // Réduire les marges si la zone est trop petite pour la taille des cartes
            if (xMin > xMax) { xMin = localXMin + hw; xMax = localXMax - hw; }
            if (yMin > yMax) { yMin = localYMin + hh; yMax = localYMax - hh; }

            if (xMin > xMax || yMin > yMax)
            {
                Debug.LogError($"[FormulaireLibreManager] PartieBasse trop petite pour spawner les cartes " +
                               $"(bounds X:[{localXMin:F1},{localXMax:F1}] Y:[{localYMin:F1},{localYMax:F1}], taille={tailleFixeCarte}).");
                return;
            }

            foreach (var carte in _cartes)
            {
                float x        = UnityEngine.Random.Range(xMin, xMax);
                float y        = UnityEngine.Random.Range(yMin, yMax);
                float rotation = UnityEngine.Random.Range(-rotationMax, rotationMax);

                var rt = carte.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
                rt.localEulerAngles = new Vector3(0f, 0f, rotation);

                float vx = UnityEngine.Random.Range(-vitesseSpawnMax, vitesseSpawnMax);
                float vy = UnityEngine.Random.Range(-vitesseSpawnMax * 0.5f, vitesseSpawnMax * 0.5f);
                carte.AjouterImpulsion(new Vector2(vx, vy));
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

        // ── Collision ─────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            for (int i = 0; i < ITERATIONS_COLLISION; i++)
                ResoudreCollisions();
        }

        /// <summary>
        /// Résout les collisions entre toutes les paires de cartes via AABB.
        /// La zone de collision est centrée sur la carte avec MARGE_COLLISION (30 %) de chaque côté,
        /// ce qui autorise le chevauchement des bords tout en empêchant la superposition totale.
        /// </summary>
        private void ResoudreCollisions()
        {
            // Demi-taille de la zone de collision : 70 % de la demi-taille de la carte
            Vector2 collHalf = tailleFixeCarte * 0.5f * (1f - MARGE_COLLISION);

            for (int i = 0; i < _cartes.Count; i++)
            {
                for (int j = i + 1; j < _cartes.Count; j++)
                {
                    // Exclure toute paire impliquant une carte en drag ou figée pour le game over —
                    // les cartes figées doivent rester exactement à leur slot, sans être repoussées.
                    if (_cartes[i].EstEnDrag         || _cartes[j].EstEnDrag)         continue;
                    if (_cartes[i].EstFigéePourGameOver || _cartes[j].EstFigéePourGameOver) continue;

                    Vector2 posA  = _cartes[i].Rt.anchoredPosition;
                    Vector2 posB  = _cartes[j].Rt.anchoredPosition;
                    Vector2 delta = posA - posB;

                    // Détection AABB sur les zones de collision
                    float overlapX = collHalf.x * 2f - Mathf.Abs(delta.x);
                    float overlapY = collHalf.y * 2f - Mathf.Abs(delta.y);

                    if (overlapX <= 0f || overlapY <= 0f) continue; // pas de collision

                    // Vecteur de séparation sur l'axe de moindre pénétration (MTV)
                    float signX = delta.x >= 0f ? 1f : -1f;
                    float signY = delta.y >= 0f ? 1f : -1f;

                    Vector2 push = overlapX < overlapY
                        ? new Vector2(overlapX * signX, 0f)
                        : new Vector2(0f, overlapY * signY);

                    // Correction symétrique entre les deux cartes libres
                    _cartes[i].AppliquerCorrectionCollision( push * 0.5f * RESTITUTION);
                    _cartes[j].AppliquerCorrectionCollision(-push * 0.5f * RESTITUTION);
                }
            }
        }

        // ── Logique de jeu ─────────────────────────────────────────────────────

        private void OnFormulaireRemisAuGarde(FormulaireType type)
        {
            inventaire.Retirer(type, 1);
        }

        private void OnFormulaireIncorrect()
        {
            barrePatience?.AppliquerPénalité();
            secousseEcran?.Secouer();
            RetourHaptique.VibrerDoubleImpulsion();

            foreach (var carte in _cartes)
                carte.Secouer(duréeSecousseCartes, intensitéSecousseCartes);
        }

        /// <summary>
        /// Grise toutes les cartes restantes dans la partie basse pour indiquer
        /// que le barrage est terminé et que le joueur ne peut plus interagir avec elles.
        /// </summary>
        private void GriserToutesLesCartes()
        {
            foreach (var carte in _cartes)
                carte.Griser();
        }

        /// <summary>Soumet une carte à la main du garde, la retire de la liste et la détruit.</summary>
        public void EnvoyerAMainDuGarde(FormulaireLibre carte)
        {
            _cartes.Remove(carte);
            mainDuGarde.RecevoirFormulaire(carte);
        }

        /// <summary>
        /// Retourne une copie de la liste des cartes actives.
        /// Utilisé par AnimationGameOver pour accéder aux cartes sans modifier la liste interne.
        /// </summary>
        public List<FormulaireLibre> ObtenirCartes() => new List<FormulaireLibre>(_cartes);

        /// <summary>
        /// Spawne des cartes supplémentaires jusqu'à atteindre <paramref name="cible"/>.
        /// Utilisé par AnimationGameOver pour garantir qu'il y a toujours assez de cartes.
        /// Les cartes sont placées directement dans la coucheGlissement hors de l'écran
        /// pour qu'AnimerVers() puisse les animer sans conflit d'espace de coordonnées.
        /// </summary>
        public void CompleterCartesGameOver(int cible)
        {
            int manquantes = cible - _cartes.Count;
            if (manquantes <= 0) return;

            // Construire la liste des types disponibles depuis _dataParType.
            // Si formulairesData est vide dans l'Inspector, _dataParType sera vide aussi —
            // on logue une erreur explicite pour guider le développeur.
            var types = new List<FormulaireType>(_dataParType.Keys);
            if (types.Count == 0)
            {
                Debug.LogError("[FormulaireLibreManager] CompleterCartesGameOver : _dataParType est vide. " +
                               "Vérifie que la liste 'Formulaires Data' est renseignée dans l'Inspector " +
                               "du FormulaireLibreManager.");
                return;
            }

            // Position de départ hors écran dans coucheGlissement (sous le bas visible).
            // On utilise les coins de coucheGlissement pour trouver le bas réel.
            Vector3[] coins = new Vector3[4];
            coucheGlissement.GetLocalCorners(coins);
            float yHorsEcran = Mathf.Min(coins[0].y, coins[1].y, coins[2].y, coins[3].y)
                               - tailleFixeCarte.y * 2f;

            for (int i = 0; i < manquantes; i++)
            {
                FormulaireType type = types[i % types.Count];
                int avantSpawn      = _cartes.Count;
                SpawnCarte(type);

                if (_cartes.Count <= avantSpawn) continue; // SpawnCarte a échoué

                var carte = _cartes[_cartes.Count - 1];
                var rt    = carte.Rt;

                // Re-parenter dans coucheGlissement AVANT qu'AnimerVers le fasse,
                // pour que anchoredPosition soit dans le bon espace de coordonnées.
                rt.SetParent(coucheGlissement, false);
                rt.anchoredPosition = new Vector2(
                    UnityEngine.Random.Range(-tailleFixeCarte.x, tailleFixeCarte.x),
                    yHorsEcran);
                rt.localEulerAngles = Vector3.zero;

                // Figer la physique immédiatement — ces cartes ne doivent pas tomber.
                carte.FigerPourGameOver();
            }
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
