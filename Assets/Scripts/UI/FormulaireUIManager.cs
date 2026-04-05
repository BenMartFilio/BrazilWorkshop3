using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Gère l'affichage et la distribution des formulaires dans les poches de la PartieBasse.
    /// Calcule la répartition optimale (moitié dans la première poche, puis les suivantes)
    /// et interleave les types pour maximiser la variété dans chaque poche.
    /// </summary>
    public class FormulaireUIManager : MonoBehaviour
    {
        [Header("Zones UI")]
        [SerializeField] private PocheUI premièrePoche;
        [SerializeField] private PocheUI deuxièmePoche;
        [SerializeField] private PocheUI dernièrePoche;
        [SerializeField] private MainDuGardeUI mainDuGarde;

        [Tooltip("RectTransform transparent rendu au-dessus de tout, utilisé pendant le glissement.")]
        [SerializeField] private RectTransform coucheGlissement;

        [Header("Données")]
        [SerializeField] private FormulaireInventaire inventaire;
        [SerializeField] private List<FormulaireData> formulairesData = new();
        [SerializeField] private AudioEventDispatcher _audioEventDispatcher;

        [Header("Objets spéciaux")]
        [Tooltip("Données joueur pour lire les quantités d'objets spéciaux à spawner comme cartes.")]
        [SerializeField] private SO_PlayerDatas donneesJoueur;
        
        [SerializeField] private AudioClip _barrageMusic;
        private PocheUI[] _poches;
        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();

        /// <summary>RectTransform de la couche de glissement (drag layer).</summary>
        public RectTransform CoucheGlissement => coucheGlissement;

        private void Awake()
        {
            _poches = new[] { premièrePoche, deuxièmePoche, dernièrePoche };

            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            mainDuGarde.OnFormulaireRemis += OnFormulaireRemisAuGarde;
        }

        private void Start()
        {
            SoundManager.Instance?.PlayMusicWithLowPass(_barrageMusic);
            // L'inventaire n'est pas réinitialisé ici : il doit persister depuis MapRoad.
            // La réinitialisation est gérée par SessionReinitialiseur et SessionManager.
            SpawnFormulaires();
            SpawnObjetsSpeciaux();
        }

        private void OnDestroy()
        {
            if (mainDuGarde != null)
                mainDuGarde.OnFormulaireRemis -= OnFormulaireRemisAuGarde;
        }

        // ── Spawn & distribution ───────────────────────────────────────────────


        /// <summary>
        /// Détruit toutes les cartes présentes dans les poches et dans la couche de glissement.
        /// Appelé lors d'un revive pour nettoyer visuellement la scène avant d'afficher la prochaine demande.
        /// </summary>
        public void NettoyerCartes()
        {
            foreach (var poche in _poches)
                poche?.Vider();

            // Cartes potentiellement en cours de glissement au moment de la mort
            if (coucheGlissement != null)
            {
                foreach (Transform enfant in coucheGlissement)
                {
                    if (enfant.GetComponent<FormulaireUI>() != null)
                        Destroy(enfant.gameObject);
                }
            }
        }



        private void SpawnFormulaires()
        {
            List<FormulaireType> liste = BuildListeInterleaved();
            int n = liste.Count;

            // Distribution : moitié dans la première poche, moitié du reste dans la deuxième
            int p1 = Mathf.CeilToInt(n / 2f);
            int p2 = Mathf.CeilToInt((n - p1) / 2f);
            int p3 = n - p1 - p2;

            int idx = 0;
            for (int i = 0; i < p1 && idx < n; i++) SpawnDansPoche(liste[idx++], premièrePoche);
            for (int i = 0; i < p2 && idx < n; i++) SpawnDansPoche(liste[idx++], deuxièmePoche);
            for (int i = 0; i < p3 && idx < n; i++) SpawnDansPoche(liste[idx++], dernièrePoche);
        }

        /// <summary>
        /// Construit une liste interleaved des types de formulaires selon les quantités de l'inventaire.
        /// Les types les plus fréquents sont distribués en premier pour maximiser la variété par poche.
        /// Les objets spéciaux (LiasseDeBillets, FormulairePasePartout, BadgeDuGouvernement) sont exclus :
        /// ils sont gérés par SO_PlayerDatas et injectés séparément par SpawnObjetsSpeciaux.
        /// </summary>
        private List<FormulaireType> BuildListeInterleaved()
        {
            var comptes = new Dictionary<FormulaireType, int>();
            foreach (FormulaireType type in Enum.GetValues(typeof(FormulaireType)))
            {
                if (EstObjetSpécial(type)) continue;
                comptes[type] = inventaire.ObtenirQuantité(type);
            }

            var résultat = new List<FormulaireType>();
            bool anyLeft = true;

            while (anyLeft)
            {
                anyLeft = false;
                IEnumerable<FormulaireType> typesTriés = comptes
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => kv.Key);

                foreach (var type in typesTriés)
                {
                    if (comptes[type] <= 0) continue;
                    résultat.Add(type);
                    comptes[type]--;
                    anyLeft = true;
                }
            }

            return résultat;
        }

        private static bool EstObjetSpécial(FormulaireType type)
            => type == FormulaireType.LiasseDeBillets
            || type == FormulaireType.FormulairePasePartout
            || type == FormulaireType.BadgeDuGouvernement;

        private void SpawnDansPoche(FormulaireType type, PocheUI poche)
        {

            if (!_dataParType.TryGetValue(type, out var data) || data.prefab == null)
            {
                Debug.LogWarning($"[FormulaireUIManager] Aucun prefab configuré pour : {type}");
                return;
            }

            // Lire la texture depuis le prefab sans l'instancier dans la scène
            RawImage sourceImage = data.prefab.GetComponentInChildren<RawImage>(true);
            Texture2D texture = sourceImage != null ? sourceImage.texture as Texture2D : null;

            if (texture == null)
            {
                Debug.LogError($"[FormulaireUIManager] Aucune texture trouvée dans le prefab '{data.prefab.name}'.");
                return;
            }

            // Lire les dimensions configurées dans le FormulaireData
            Vector2 tailleOriginale = data.taille;

            // Créer un GameObject UI propre, enfant direct de la poche
            var go = new GameObject(data.prefab.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(poche.transform, false);

            // Taille fixe centrée héritée du prefab source
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = tailleOriginale;
            rt.anchoredPosition = Vector2.zero;

            // Assigner la texture
            var img = go.GetComponent<RawImage>();
            img.texture       = texture;
            img.raycastTarget = true;

            // Ajouter le comportement drag-and-drop
            var formulaireUI = go.AddComponent<FormulaireUI>();
            formulaireUI.Initialiser(type, tailleOriginale, this,_audioEventDispatcher);

            int index = poche.AjouterFormulaire(formulaireUI);
            rt.anchoredPosition = poche.ObtenirPositionPourIndex(index);
        }

        // ── Logique de jeu ─────────────────────────────────────────────────────

        private void OnFormulaireRemisAuGarde(FormulaireType type)
        {
            inventaire.Retirer(type, 1);
            Debug.Log($"[FormulaireUIManager] Inventaire mis à jour après remise : {type} → {inventaire.ObtenirQuantité(type)} restants");
        }

        /// <summary>Envoie un formulaire à la main du garde et met à jour l'inventaire.</summary>
        public void EnvoyerAMainDuGarde(FormulaireUI formulaire)
        {
            mainDuGarde.RecevoirFormulaire(formulaire);
        }

        // ── Objets spéciaux ────────────────────────────────────────────────────

        /// <summary>
        /// Spawne les objets spéciaux du joueur (LiasseDeBillets, FormulairePasePartout, BadgeDuGouvernement)
        /// comme des cartes draggables dans les poches, à côté des formulaires normaux.
        /// Chaque objet spawne autant de cartes que la quantité possédée.
        /// </summary>
        private void SpawnObjetsSpeciaux()
        {
            if (donneesJoueur == null) return;

            var mapping = new Dictionary<string, FormulaireType>
            {
                { "LiasseDeBillets",      FormulaireType.LiasseDeBillets      },
                { "FormulairePasePartout",FormulaireType.FormulairePasePartout },
                { "BadgeDuGouvernement",  FormulaireType.BadgeDuGouvernement  },
            };

            foreach (InventoryEntry entree in donneesJoueur.ObtenirInventairePlat())
            {
                if (!mapping.TryGetValue(entree.objectName, out FormulaireType type)) continue;
                if (entree.quantity <= 0) continue;

                for (int i = 0; i < entree.quantity; i++)
                {
                    PocheUI poche = _poches[i % _poches.Length];
                    SpawnDansPoche(type, poche);
                }
            }
        }

        // ── Helpers de détection ───────────────────────────────────────────────

        /// <summary>
        /// Retourne true si la position écran est dans la zone de la main du garde
        /// ET que le composant MainDuGardeUI est actif (pas de game over, pas de premier barrage).
        /// La caméra utilisée est dérivée du Canvas racine de MainDuGarde :
        ///   - Screen Space Overlay → null (comportement correct pour ce mode)
        ///   - Screen Space Camera / World Space → worldCamera du canvas
        /// Cela garantit un hit-test correct indépendamment de la caméra du canvas des cartes.
        /// </summary>
        public bool EstSurMainDuGarde(Vector2 screenPos, Camera _)
        {
            // Si MainDuGardeUI est désactivé (game over, premier barrage…), la zone n'est pas valide.
            if (mainDuGarde == null || !mainDuGarde.enabled) return false;

            Canvas canvas = mainDuGarde.RectTransform.GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            Camera camGarde = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                              ? canvas.worldCamera
                              : null;
            return RectTransformUtility.RectangleContainsScreenPoint(mainDuGarde.RectTransform, screenPos, camGarde);
        }

        /// <summary>Retourne la poche dont la zone contient la position écran, ou null si aucune.</summary>
        public PocheUI TrouverPocheSousPointeur(Vector2 screenPos, Camera cam)
        {
            foreach (var poche in _poches)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(poche.RectTransform, screenPos, cam))
                    return poche;
            }
            return null;
        }

        /// <summary>Retourne la poche la plus proche d'une position monde.</summary>
        public PocheUI TrouverPocheProche(Vector3 positionMonde)
        {
            return _poches.OrderBy(p => Vector3.Distance(p.transform.position, positionMonde)).First();
        }
    }
}
