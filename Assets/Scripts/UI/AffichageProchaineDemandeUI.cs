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
    [DefaultExecutionOrder(0)]
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

        [Header("Budget spawn MapRoad")]
        [Tooltip("Budget partagé avec MapRoad — calcule combien de véhicules de chaque type spawner.")]
        [SerializeField] private FormulaireSpawnBudget spawnBudget;

        [Tooltip("Patterns normaux de MapRoad — utilisés pour calculer la moyenne de Good véhicules/pattern. " +
                 "Ne pas inclure le pattern Barrage.")]
        [SerializeField] private ObstaclePattern[] patternsMapRoad;

        [Header("Animation")]
        [Tooltip("Délai entre l'apparition de chaque icône (secondes).")]
        [SerializeField] private float délaiEntreIcones = 0.15f;

        [Header("Retour MapRoad")]
        [Tooltip("Délai d'attente après la dernière icône avant de retourner sur MapRoad.")]
        [SerializeField] private float délaiAvantRetour = 2f;

        [Header("Bouton retour")]
        [Tooltip("Bouton qui apparaît après l'affichage des icônes pour permettre au joueur de revenir sur MapRoad manuellement.")]
        [SerializeField] private BoutonRetourMapRoad boutonRetour;

        [Tooltip("Délai (secondes) après la dernière icône avant que le bouton devienne visible.")]
        [SerializeField, Min(0f)] private float délaiApparitionBouton = 2f;

        [Header("Barre de patience")]
        [Tooltip("BarrePatience à geler dès que le barrage est validé pour empêcher le game over.")]
        [SerializeField] private BarrePatience barrePatience;

        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();
        private Coroutine _affichage;

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            Debug.Log($"[AffichageProchaineDemandeUI] Awake — {_dataParType.Count} FormulaireData : " +
                      string.Join(", ", _dataParType.Keys) +
                      $" | {slots.Count} slots");

            foreach (var slot in slots)
                slot.Masquer();
        }

        private void Start()
        {
            // Les slots restent masqués pendant toute la phase de remise de documents.
            // Le joueur a déjà vu la demande courante à la fin du barrage précédent.
            // L'affichage ne se déclenche QUE via OnBarrageValidé (barrages 2+)
            // ou LancerDirectement() (premier barrage via PremierBarrageController).
            foreach (var slot in slots)
                slot.Masquer();

            Debug.Log("[AffichageProchaineDemandeUI] Start — slots masqués, en attente de OnBarrageValidé.");
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
                Debug.LogWarning("[AffichageProchaineDemandeUI] OnEnable — mainDuGarde est NULL !");
            }
        }

        private void OnDisable()
        {
            if (mainDuGarde != null)
                mainDuGarde.OnBarrageValidé -= OnBarrageValidé;
        }

        private void OnBarrageValidé()
        {
            // BarrageTuto manages its own end-of-barrage flow via BarrageTutoEndController.
            // Running the normal next-demand pipeline here would save a demand and auto-redirect
            // to MapRoad, bypassing SessionReinitialiseur in MainMenu entirely.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BarrageTuto") return;

            Debug.Log("[AffichageProchaineDemandeUI] OnBarrageValidé → Geler() + masquer icônes courantes + afficher prochaine demande.");

            barrePatience?.Geler();

            // Arrêter AfficherDemandeCourante si elle tournait encore (ne devrait pas,
            // mais sécurité en cas de délai entre icônes très long).
            if (_affichage != null)
            {
                StopCoroutine(_affichage);
                _affichage = null;
            }

            // Passer le nombre de barrages déjà complétés pour calibrer la difficulté.
            int barragesComplétés = donnéesSession != null ? donnéesSession.nombreBarragesComplétés : 0;
            demande?.Régénérer(barragesComplétés);

            // Calculer le budget de spawn pour la prochaine MapRoad dès maintenant,
            // avant que le joueur ne retourne sur la route (pour ne pas fausser la progression).
            if (spawnBudget != null && demande != null)
                spawnBudget.DefinirBudget(demande.Formulaires, patternsMapRoad);

            _affichage = StartCoroutine(AfficherIconesUneParUne());
        }

        /// <summary>
        /// Déclenche directement l'affichage de la prochaine demande sans attendre
        /// l'événement <see cref="MainDuGardeUI.OnBarrageValidé"/>.
        /// Utilisé par <see cref="PremierBarrageController"/> au premier barrage.
        /// La BarrePatience est déjà gelée dès son Awake — aucune action supplémentaire nécessaire.
        /// Au premier barrage (nombreBarragesComplétés == 0), la difficulté est toujours facile.
        /// </summary>
        public void LancerDirectement()
        {
            Debug.Log("[AffichageProchaineDemandeUI] LancerDirectement() → Régénérer() + AfficherIconesUneParUne().");

            // Au premier barrage le compteur est 0 → palier facile garanti.
            int barragesComplétés = donnéesSession != null ? donnéesSession.nombreBarragesComplétés : 0;
            demande?.Régénérer(barragesComplétés);

            // Calculer le budget de spawn pour la prochaine MapRoad.
            if (spawnBudget != null && demande != null)
                spawnBudget.DefinirBudget(demande.Formulaires, patternsMapRoad);

            if (_affichage != null) StopCoroutine(_affichage);
            _affichage = StartCoroutine(AfficherIconesUneParUne());
        }

        /// <summary>
        /// Résout la texture associée à un FormulaireType.
        /// Retourne null si aucun FormulaireData valide n'existe pour ce type.
        /// </summary>
        private Texture2D ObtenirTexture(FormulaireType type)
        {
            if (!_dataParType.TryGetValue(type, out var data)) return null;
            return data.ExtraireTexture();
        }

        /// <summary>
        /// Affiche les icônes une à une : chaque entrée de la demande brute occupe son propre slot.
        /// Un même type peut apparaître sur plusieurs slots consécutifs si la demande le répète.
        /// </summary>
        private IEnumerator AfficherIconesUneParUne()
        {
            boutonRetour?.Masquer();

            var snapshotBrut = new List<FormulaireType>(demande.Formulaires);

            Debug.Log($"[AffichageProchaineDemandeUI] ── Snapshot brut ({snapshotBrut.Count}) : " +
                      (snapshotBrut.Count > 0 ? string.Join(", ", snapshotBrut) : "<vide>"));

            foreach (var slot in slots)
                slot.Masquer();

            // Une entrée de la liste brute = un slot. Pas de déduplication.
            int nbSlots = Mathf.Min(snapshotBrut.Count, slots.Count);

            Debug.Log($"[AffichageProchaineDemandeUI] Prochaine demande — {nbSlots} icône(s).");

            for (int i = 0; i < nbSlots; i++)
            {
                FormulaireType type = snapshotBrut[i];
                Texture2D tex = ObtenirTexture(type);
                if (tex == null)
                {
                    Debug.LogWarning($"[AffichageProchaineDemandeUI] Slot {i} — texture introuvable pour {type}.");
                    continue;
                }
                Debug.Log($"[AffichageProchaineDemandeUI] Slot {i} ← {type}");
                slots[i].Afficher(tex, 1);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            // Sauvegarder la liste BRUTE (avec doublons) pour que le barrage suivant
            // reçoive exactement la bonne quantité par type.
            if (donnéesSession != null)
            {
                donnéesSession.prochaineDemandeBarrage = snapshotBrut.ToArray();

                // Sauvegarder le nombre de patterns calculé par le budget.
                if (spawnBudget != null)
                    donnéesSession.patternsNécessaires = spawnBudget.PatternsNécessaires;

                // Incrémenter le compteur de barrages complétés : le prochain barrage
                // utilisera ce nouveau total pour calibrer sa difficulté.
                donnéesSession.nombreBarragesComplétés++;

                Debug.Log($"[AffichageProchaineDemandeUI] ★ Sauvegardé ({snapshotBrut.Count}) : " +
                          string.Join(", ", snapshotBrut) +
                          $" | barragesComplétés={donnéesSession.nombreBarragesComplétés}" +
                          $" | patternsNécessaires={donnéesSession.patternsNécessaires}");
            }
            else
            {
                Debug.LogError("[AffichageProchaineDemandeUI] donnéesSession NULL — demande non sauvegardée !");
            }

            if (boutonRetour != null)
            {
                // Le bouton apparaît après délaiApparitionBouton secondes.
                // C'est lui qui appelle RetournerAMapRoad au clic.
                Debug.Log($"[AffichageProchaineDemandeUI] Bouton retour dans {délaiApparitionBouton}s.");
                boutonRetour.AfficherApresDelai(délaiApparitionBouton);
            }
            else
            {
                // Pas de bouton assigné → retour automatique après délaiAvantRetour.
                Debug.Log($"[AffichageProchaineDemandeUI] Attente {délaiAvantRetour}s avant retour automatique...");
                yield return new WaitForSeconds(délaiAvantRetour);

                if (SessionManager.Instance != null)
                {
                    Debug.Log("[AffichageProchaineDemandeUI] RetournerAMapRoad() automatique (boutonRetour non assigné).");
                    SessionManager.Instance.RetournerAMapRoad();
                }
                else
                {
                    Debug.LogError("[AffichageProchaineDemandeUI] SessionManager introuvable — retour MapRoad annulé.");
                }
            }

            _affichage = null;
        }
    }
}
