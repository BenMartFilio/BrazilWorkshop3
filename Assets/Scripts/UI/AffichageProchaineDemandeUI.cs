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
    [DefaultExecutionOrder(-50)]
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

        // Demande courante capturée en Awake() avant que MainDuGardeUI (order 0) efface la session.
        private FormulaireType[] _demandeEnCours;

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            // Capturer la demande courante AVANT que MainDuGardeUI.Awake() la supprime.
            // MainDuGardeUI a l'ordre d'exécution par défaut (0) ; ce composant a -50.
            _demandeEnCours = donnéesSession != null && donnéesSession.AUneDemandeSauvegardée
                ? (FormulaireType[])donnéesSession.prochaineDemandeBarrage.Clone()
                : null;

            Debug.Log($"[AffichageProchaineDemandeUI] Awake — {_dataParType.Count} FormulaireData : " +
                      string.Join(", ", _dataParType.Keys) +
                      $" | {slots.Count} slots | demandeEnCours={(_demandeEnCours != null ? string.Join(", ", _demandeEnCours) : "aucune")}");

            foreach (var slot in slots)
                slot.Masquer();
        }

        private void Start()
        {
            // Afficher la demande COURANTE si ce n'est pas le premier barrage.
            // mainDuGarde.enabled est déjà positionné par MainDuGardeUI.Awake() (order 0),
            // qui s'est exécuté après notre Awake() (order -50) mais avant tout Start().
            bool demandeValide = _demandeEnCours != null && _demandeEnCours.Length > 0;
            bool gardeActif    = mainDuGarde != null && mainDuGarde.enabled;

            Debug.Log($"[AffichageProchaineDemandeUI] Start — demandeValide={demandeValide}, gardeActif={gardeActif}");

            if (demandeValide && gardeActif)
            {
                if (_affichage != null) StopCoroutine(_affichage);
                _affichage = StartCoroutine(AfficherDemandeCourante());
            }
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
            Debug.Log("[AffichageProchaineDemandeUI] OnBarrageValidé reçu → Geler() + Régénérer() + AfficherIconesUneParUne().");

            // Geler la barre immédiatement : le game over ne peut plus se déclencher.
            barrePatience?.Geler();

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
            Debug.Log("[AffichageProchaineDemandeUI] LancerDirectement() → Geler() + Régénérer() + AfficherIconesUneParUne().");

            // Au premier barrage, la patience n'a pas encore de sens — on la gèle d'emblée.
            barrePatience?.Geler();

            demande?.Régénérer();

            if (_affichage != null) StopCoroutine(_affichage);
            _affichage = StartCoroutine(AfficherIconesUneParUne());
        }

        /// <summary>
        /// Affiche les icônes de la demande COURANTE au début du barrage
        /// pour que le joueur sache exactement quels formulaires présenter.
        /// Utilise les mêmes slots que la prochaine demande.
        /// </summary>
        private IEnumerator AfficherDemandeCourante()
        {
            boutonRetour?.Masquer();

            foreach (var slot in slots)
                slot.Masquer();

            var affichables = FiltrerAffichables(_demandeEnCours);
            int nbSlots     = Mathf.Min(affichables.Count, slots.Count);

            Debug.Log($"[AffichageProchaineDemandeUI] Demande COURANTE — {nbSlots} icône(s) : " +
                      string.Join(", ", affichables.Take(nbSlots)));

            for (int i = 0; i < nbSlots; i++)
            {
                slots[i].Afficher(affichables[i].texture, 1);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            _affichage = null;
        }

        /// <summary>
        /// Filtre une liste de types en ne conservant que ceux qui ont un FormulaireData
        /// valide avec texture, et retourne des paires (type, texture).
        /// </summary>
        private List<(FormulaireType type, Texture2D texture)> FiltrerAffichables(IEnumerable<FormulaireType> types)
        {
            var résultat = new List<(FormulaireType, Texture2D)>();
            var vus      = new HashSet<FormulaireType>();

            foreach (var type in types)
            {
                if (!vus.Add(type)) continue;
                if (!_dataParType.TryGetValue(type, out var data)) continue;
                Texture2D tex = data.ExtraireTexture();
                if (tex == null) continue;
                résultat.Add((type, tex));
            }

            return résultat;
        }

        /// <summary>
        /// Affiche les icônes une à une en regroupant par type :
        /// chaque slot reçoit un type distinct avec la quantité totale de ce type dans la demande.
        /// L'ordre des slots suit l'ordre d'apparition des types dans la demande.
        /// </summary>
        private IEnumerator AfficherIconesUneParUne()
        {
            boutonRetour?.Masquer();

            var snapshotBrut = new List<FormulaireType>(demande.Formulaires);

            Debug.Log($"[AffichageProchaineDemandeUI] ── Snapshot brut ({snapshotBrut.Count}) : " +
                      (snapshotBrut.Count > 0 ? string.Join(", ", snapshotBrut) : "<vide>"));

            foreach (var slot in slots)
                slot.Masquer();

            var affichables = FiltrerAffichables(snapshotBrut);
            int nbSlots     = Mathf.Min(affichables.Count, slots.Count);

            Debug.Log($"[AffichageProchaineDemandeUI] Prochaine demande — {nbSlots}/{affichables.Count} icône(s).");

            for (int i = 0; i < nbSlots; i++)
            {
                Debug.Log($"[AffichageProchaineDemandeUI] Slot {i} ← {affichables[i].type}");
                slots[i].Afficher(affichables[i].texture, 1);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            // Sauvegarder immédiatement après l'affichage des icônes,
            // avant que le joueur puisse cliquer sur le bouton.
            var àSauvegarder = affichables.Take(nbSlots).Select(p => p.type).ToArray();

            if (donnéesSession != null)
            {
                donnéesSession.prochaineDemandeBarrage = àSauvegarder;
                Debug.Log($"[AffichageProchaineDemandeUI] ★ Sauvegardé ({àSauvegarder.Length}) : " +
                          string.Join(", ", àSauvegarder));
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
