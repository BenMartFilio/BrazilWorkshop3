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

        // ── API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Démarre la séquence d'affichage. Appelé par <see cref="MapRoadSessionBridge"/>.
        /// </summary>
        public void Lancer(MapRoadSessionBridge bridge)
        {
            _bridge = bridge;
            StartCoroutine(SéquenceIntro());
        }

        // ── Séquence ──────────────────────────────────────────────────────────

        private IEnumerator SéquenceIntro()
        {
            // Construire la liste de types à afficher
            var types = ObtenirTypesÀAfficher();

            if (types.Count == 0)
            {
                // Rien à afficher — démarrer directement
                _bridge.DémarrerJeu();
                yield break;
            }

            // Regrouper par type et compter les occurrences
            var typesOrdrés = new List<FormulaireType>();
            var comptes     = new Dictionary<FormulaireType, int>();

            foreach (var type in types)
            {
                if (!comptes.ContainsKey(type))
                {
                    typesOrdrés.Add(type);
                    comptes[type] = 0;
                }
                comptes[type]++;
            }

            // Afficher les slots un à un
            int nbSlots = Mathf.Min(typesOrdrés.Count, slots.Count);

            for (int i = 0; i < nbSlots; i++)
            {
                FormulaireType type = typesOrdrés[i];

                if (!_dataParType.TryGetValue(type, out var data))
                {
                    Debug.LogWarning($"[AffichagePremierBarrageUI] Aucun FormulaireData pour : {type}");
                    continue;
                }

                Texture2D texture = data.ExtraireTexture();
                if (texture == null)
                {
                    Debug.LogWarning($"[AffichagePremierBarrageUI] Aucune texture dans le prefab de : {type}");
                    continue;
                }

                slots[i].Afficher(texture, comptes[type]);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            // Attendre la durée d'affichage totale
            yield return new WaitForSeconds(duréeAffichage);

            // Masquer les icônes
            foreach (var slot in slots)
                slot.Masquer();

            // Lancer le jeu
            _bridge.DémarrerJeu();
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
