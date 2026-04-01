using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Affiche la demande du premier barrage dès le démarrage de MapRoad,
    /// avant que le jeu (véhicule, sol, spawner) ne commence.
    ///
    /// Flux :
    ///   1. <see cref="MapRoadSessionBridge"/> gèle les systèmes puis appelle <see cref="Lancer"/>.
    ///   2. Les icônes apparaissent une à une (même logique que AffichageProchaineDemandeUI).
    ///   3. Après <see cref="duréeAffichage"/> secondes, le panneau disparaît et
    ///      <see cref="MapRoadSessionBridge.DémarrerJeu"/> est invoqué.
    ///
    /// Si <see cref="DonnéesSession.prochaineDemandeBarrage"/> est vide (nouvelle partie),
    /// une demande aléatoire est générée localement pour informer le joueur.
    /// </summary>
    public class AffichagePremierBarrageUI : MonoBehaviour
    {
        // ── Configuration ─────────────────────────────────────────────────────

        [Header("Slots d'icônes")]
        [Tooltip("Les IconFormulaireUI dans la scène MapRoad, dans l'ordre gauche→droite.")]
        [SerializeField] private List<IconFormulaireUI> slots = new();

        [Header("Données")]
        [Tooltip("ScriptableObject de session partagé — contient prochaineDemandeBarrage.")]
        [SerializeField] private DonnéesSession donnéesSession;

        [Tooltip("Les FormulaireData pour associer chaque type à sa texture.")]
        [SerializeField] private List<FormulaireData> formulairesData = new();

        [Tooltip("DemandeBarrage utilisé pour générer une demande aléatoire si la session n'en contient pas.")]
        [SerializeField] private DemandeBarrage demandeAléatoire;

        [Header("Timing")]
        [Tooltip("Durée d'affichage des icônes avant que le jeu démarre (secondes).")]
        [SerializeField, Min(0f)] private float duréeAffichage = 3f;

        [Tooltip("Délai entre l'apparition de chaque icône (secondes).")]
        [SerializeField, Min(0f)] private float délaiEntreIcones = 0.15f;

        // ── État interne ──────────────────────────────────────────────────────

        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();
        private MapRoadSessionBridge _bridge;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            foreach (var slot in slots)
                slot.Masquer();
        }

        private void Start()
        {
            // ObtenirTypesÀAfficher() gère les deux cas :
            //   • session avec demande → rappel des documents à présenter
            //   • nouvelle partie → génération aléatoire + sauvegarde en session
            //     pour que le premier barrage exige exactement ces documents
            var types = ObtenirTypesÀAfficher();

            if (types.Count == 0)
            {
                Debug.Log("[AffichagePremierBarrageUI] Aucun type à afficher — rappel ignoré.");
                return;
            }

            Debug.Log($"[AffichagePremierBarrageUI] Rappel de la demande ({types.Count} entrées) : " +
                      string.Join(", ", types));

            StartCoroutine(SéquenceRappel(types));
        }

        // ── API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Conservé pour compatibilité. Le rappel se déclenche maintenant automatiquement
        /// depuis <see cref="Start"/> — cette méthode n'a plus besoin d'être appelée.
        /// </summary>
        public void Lancer(MapRoadSessionBridge bridge)
        {
            _bridge = bridge;
        }

        // ── Séquence ──────────────────────────────────────────────────────────

        /// <summary>
        /// Affiche les icônes de la demande à apporter au prochain barrage,
        /// puis les masque après <see cref="duréeAffichage"/> secondes.
        /// Le jeu tourne normalement en dessous — aucun gel.
        /// </summary>
        private IEnumerator SéquenceRappel(List<FormulaireType> types)
        {
            // Filtrage : ne conserver que les types affichables
            var affichables = new List<FormulaireType>();
            var texturesParType = new Dictionary<FormulaireType, Texture2D>();

            foreach (var type in types)
            {
                if (texturesParType.ContainsKey(type)) continue;
                if (!_dataParType.TryGetValue(type, out var data)) continue;
                Texture2D texture = data.ExtraireTexture();
                if (texture == null) continue;
                affichables.Add(type);
                texturesParType[type] = texture;
            }

            int nbSlots = Mathf.Min(affichables.Count, slots.Count);
            Debug.Log($"[AffichagePremierBarrageUI] Affichage de {nbSlots} icône(s) de rappel.");

            for (int i = 0; i < nbSlots; i++)
            {
                slots[i].Afficher(texturesParType[affichables[i]], 1);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            yield return new WaitForSeconds(duréeAffichage);

            foreach (var slot in slots)
                slot.Masquer();

            Debug.Log("[AffichagePremierBarrageUI] Rappel terminé — icônes masquées.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne la liste de types à afficher :
        /// - depuis <see cref="DonnéesSession.prochaineDemandeBarrage"/> si disponible,
        /// - sinon depuis une <see cref="demandeAléatoire"/> régénérée,
        ///   puis sauvegardée dans la session pour que le premier barrage reçoive
        ///   exactement les mêmes documents.
        /// </summary>
        private List<FormulaireType> ObtenirTypesÀAfficher()
        {
            if (donnéesSession != null && donnéesSession.AUneDemandeSauvegardée)
                return new List<FormulaireType>(donnéesSession.prochaineDemandeBarrage);

            if (demandeAléatoire != null)
            {
                demandeAléatoire.Régénérer();
                var types = new List<FormulaireType>(demandeAléatoire.Formulaires);

                // Sauvegarder immédiatement dans la session :
                // ListeAttenteGarde.ChargerDepuisSession() lira ces données
                // et exigera exactement ces documents au premier barrage.
                if (donnéesSession != null)
                    donnéesSession.prochaineDemandeBarrage = types.ToArray();

                return types;
            }

            Debug.LogWarning("[AffichagePremierBarrageUI] Aucune demande disponible — démarrage sans affichage.");
            return new List<FormulaireType>();
        }
    }
}
