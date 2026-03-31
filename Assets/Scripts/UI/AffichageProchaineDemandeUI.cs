using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Orchestre l'affichage de la prochaine demande de barrage.
    /// L'affichage ne se déclenche QUE lorsque le joueur valide le barrage courant
    /// (via l'événement OnBarrageValidé de MainDuGardeUI).
    /// Les icônes apparaissent une à une avec un délai, accompagnées du nombre requis par type.
    /// </summary>
    public class AffichageProchaineDemandeUI : MonoBehaviour
    {
        [Header("Slots d'icônes (dans l'ordre d'affichage)")]
        [Tooltip("Les quatre IconFormulaireUI dans la scène, dans l'ordre gauche→droite.")]
        [SerializeField] private List<IconFormulaireUI> slots = new();

        [Header("Données")]
        [Tooltip("La demande de barrage à générer et afficher.")]
        [SerializeField] private DemandeBarrage demande;
        [Tooltip("Les FormulaireData pour associer chaque type à sa texture.")]
        [SerializeField] private List<FormulaireData> formulairesData = new();
        [Tooltip("MainDuGardeUI dont l'événement OnBarrageValidé déclenche l'affichage.")]
        [SerializeField] private MainDuGardeUI mainDuGarde;
        [Tooltip("ScriptableObject de session partagé — pour sauvegarder la prochaine demande.")]
        [SerializeField] private DonnéesSession donnéesSession;

        [Header("Animation")]
        [Tooltip("Délai entre l'apparition de chaque icône (secondes).")]
        [SerializeField] private float délaiEntreIcones = 0.15f;

        [Header("Retour MapRoad")]
        [Tooltip("Délai d'attente après la dernière icône avant de retourner sur MapRoad.")]
        [SerializeField] private float délaiAvantRetour = 2f;

        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();
        private Coroutine _affichage;

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            Debug.Log($"[AffichageProchaineDemandeUI] Awake — {_dataParType.Count} FormulaireData chargés : " +
                      string.Join(", ", _dataParType.Keys) +
                      $" | {slots.Count} slots | demande={demande?.name ?? "NULL"} | donnéesSession={donnéesSession?.name ?? "NULL"}");

            // Masquer tous les slots au démarrage — rien ne s'affiche avant la validation
            foreach (var slot in slots)
                slot.Masquer();
        }

        private void OnEnable()
        {
            if (mainDuGarde != null)
            {
                mainDuGarde.OnBarrageValidé += OnBarrageValidé;
                Debug.Log("[AffichageProchaineDemandeUI] OnEnable — abonné à mainDuGarde.OnBarrageValidé.");
            }
            else
            {
                Debug.LogWarning("[AffichageProchaineDemandeUI] OnEnable — mainDuGarde est NULL, impossible de s'abonner à OnBarrageValidé !");
            }
        }

        private void OnDisable()
        {
            if (mainDuGarde != null)
                mainDuGarde.OnBarrageValidé -= OnBarrageValidé;
        }

        private void OnBarrageValidé()
        {
            Debug.Log("[AffichageProchaineDemandeUI] OnBarrageValidé reçu → Régénérer() + AfficherIconesUneParUne().");
            // Générer une nouvelle demande aléatoire puis l'afficher
            demande?.Régénérer();

            if (_affichage != null) StopCoroutine(_affichage);
            _affichage = StartCoroutine(AfficherIconesUneParUne());
        }

        /// <summary>
        /// Déclenche directement l'affichage de la prochaine demande sans attendre
        /// l'événement <see cref="MainDuGardeUI.OnBarrageValidé"/>.
        /// Utilisé par <see cref="PremierBarrageController"/> au premier barrage.
        /// </summary>
        public void LancerDirectement()
        {
            Debug.Log("[AffichageProchaineDemandeUI] LancerDirectement() → Régénérer() + AfficherIconesUneParUne().");
            demande?.Régénérer();

            if (_affichage != null) StopCoroutine(_affichage);
            _affichage = StartCoroutine(AfficherIconesUneParUne());
        }

        /// <summary>
        /// Affiche les icônes une à une en regroupant par type :
        /// chaque slot reçoit un type distinct avec la quantité totale de ce type dans la demande.
        /// L'ordre des slots suit l'ordre d'apparition des types dans la demande.
        /// </summary>
        private IEnumerator AfficherIconesUneParUne()
        {
            // ── Snapshot brut depuis la demande générée ───────────────────────
            var snapshotBrut = new List<FormulaireType>(demande.Formulaires);

            Debug.Log($"[AffichageProchaineDemandeUI] ── Snapshot brut ({snapshotBrut.Count} entrées) : " +
                      (snapshotBrut.Count > 0 ? string.Join(", ", snapshotBrut) : "<vide>"));

            if (snapshotBrut.Count == 0)
                Debug.LogWarning("[AffichageProchaineDemandeUI] Snapshot brut vide — vérifiez que Régénérer() a été appelé.");

            foreach (var slot in slots)
                slot.Masquer();

            // ── Filtrage : on ne conserve que les types affichables ────────────
            // Un type est affichable ssi il a un FormulaireData valide avec texture.
            // C'est la liste filtrée qui sera affichée ET sauvegardée en session :
            // affichage et validation seront toujours identiques.
            var typesAffichables = new List<FormulaireType>();
            var texturesParType  = new Dictionary<FormulaireType, Texture2D>();

            foreach (var type in snapshotBrut)
            {
                if (texturesParType.ContainsKey(type)) continue; // déjà traité

                if (!_dataParType.TryGetValue(type, out var data))
                {
                    Debug.LogWarning($"[AffichageProchaineDemandeUI] ✗ Aucun FormulaireData pour '{type}' — " +
                                     "type exclu de l'affichage ET de la sauvegarde session.");
                    continue;
                }

                Texture2D texture = data.ExtraireTexture();
                if (texture == null)
                {
                    Debug.LogWarning($"[AffichageProchaineDemandeUI] ✗ Aucune texture pour '{type}' — " +
                                     "type exclu de l'affichage ET de la sauvegarde session.");
                    continue;
                }

                typesAffichables.Add(type);
                texturesParType[type] = texture;
            }

            Debug.Log($"[AffichageProchaineDemandeUI] Types affichables après filtrage ({typesAffichables.Count}/{snapshotBrut.Count}) : " +
                      string.Join(", ", typesAffichables));

            // ── Affichage slot par slot ───────────────────────────────────────
            int nbSlots = Mathf.Min(typesAffichables.Count, slots.Count);
            Debug.Log($"[AffichageProchaineDemandeUI] Slots disponibles={slots.Count} → {nbSlots} icônes affichées.");

            for (int i = 0; i < nbSlots; i++)
            {
                FormulaireType type = typesAffichables[i];
                Debug.Log($"[AffichageProchaineDemandeUI] Slot {i} ← {type} (texture='{texturesParType[type].name}')");
                slots[i].Afficher(texturesParType[type], 1);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            // Attendre puis retourner sur MapRoad
            Debug.Log($"[AffichageProchaineDemandeUI] Attente de {délaiAvantRetour}s avant sauvegarde + retour MapRoad...");
            yield return new WaitForSeconds(délaiAvantRetour);

            // ── Sauvegarde : UNIQUEMENT les types affichés ────────────────────
            // Le tableau sauvegardé est strictement égal à ce qui a été montré au joueur.
            if (donnéesSession != null)
            {
                donnéesSession.prochaineDemandeBarrage = typesAffichables.ToArray();
                Debug.Log($"[AffichageProchaineDemandeUI] ★ Sauvegarde dans prochaineDemandeBarrage " +
                          $"({typesAffichables.Count} entrées) : {string.Join(", ", typesAffichables)}");
            }
            else
            {
                Debug.LogError("[AffichageProchaineDemandeUI] donnéesSession est NULL — " +
                               "la demande ne sera PAS sauvegardée !");
            }

            if (SessionManager.Instance != null)
            {
                Debug.Log("[AffichageProchaineDemandeUI] RetournerAMapRoad() appelé.");
                SessionManager.Instance.RetournerAMapRoad();
            }
            else
            {
                Debug.LogError("[AffichageProchaineDemandeUI] SessionManager introuvable — retour MapRoad annulé.");
            }

            _affichage = null;
        }
    }
}
