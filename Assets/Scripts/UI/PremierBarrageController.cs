using System.Collections;
using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Orchestre le tout premier barrage de la partie.
    ///
    /// Quand aucun document n'a encore été sauvegardé en session
    /// (<see cref="DonnéesSession.AUneDemandeSauvegardée"/> == false), ce barrage
    /// sert uniquement à informer le joueur des documents à apporter la prochaine fois.
    /// Aucune validation de formulaire n'est exigée.
    ///
    /// Flux :
    ///   1. Le garde affiche le message "Il vous faudrait ces formulaires…" (géré par BulleDialogueGardeUI).
    ///   2. Après <see cref="délaiAvantAffichageDemande"/> secondes, la demande visuelle apparaît
    ///      via <see cref="AffichageProchaineDemandeUI.LancerDirectement"/>.
    ///   3. <see cref="AffichageProchaineDemandeUI"/> génère la demande, la sauvegarde en session
    ///      et retourne sur MapRoad.
    /// </summary>
    // Priorité -100 : Awake() s'exécute AVANT celui de MainDuGardeUI (ordre par défaut = 0)
    // pour lire AUneDemandeSauvegardée avant que ChargerDepuisSession() ne l'efface.
    [DefaultExecutionOrder(-100)]
    public class PremierBarrageController : MonoBehaviour
    {
        [Header("Données")]
        [Tooltip("ScriptableObject de session partagé — détecte si c'est le premier barrage.")]
        [SerializeField] private DonnéesSession donnéesSession;

        [Header("Références")]
        [Tooltip("AffichageProchaineDemandeUI à déclencher pour afficher et sauvegarder la prochaine demande.")]
        [SerializeField] private AffichageProchaineDemandeUI affichageDemande;

        [Header("Timing")]
        [Tooltip("Délai (s) entre l'apparition du message du garde et l'affichage des icônes de demande.")]
        [SerializeField, Min(0f)] private float délaiAvantAffichageDemande = 2.5f;

        // Capturé dans Awake AVANT que ListeAttenteGarde.ChargerDepuisSession()
        // n'efface prochaineDemandeBarrage dans son propre Awake/Start.
        private bool _estPremierBarrage;

        private void Awake()
        {
            // On lit la valeur ici, avant que d'autres Awake() ne consomment la demande.
            _estPremierBarrage = donnéesSession == null || !donnéesSession.AUneDemandeSauvegardée;

            Debug.Log($"[PremierBarrageController] Awake (ordre=-100) — donnéesSession={donnéesSession?.name ?? "NULL"}, " +
                      $"AUneDemandeSauvegardée={donnéesSession?.AUneDemandeSauvegardée ?? false}, " +
                      $"_estPremierBarrage={_estPremierBarrage}");

            if (_estPremierBarrage && donnéesSession != null)
            {
                Debug.Log("[PremierBarrageController] prochaineDemandeBarrage actuel : " +
                          (donnéesSession.prochaineDemandeBarrage?.Length > 0
                              ? string.Join(", ", donnéesSession.prochaineDemandeBarrage)
                              : "<vide — premier barrage confirmé>"));
            }
        }

        private void Start()
        {
            Debug.Log($"[PremierBarrageController] Start — _estPremierBarrage={_estPremierBarrage}");
            if (!_estPremierBarrage) return;

            StartCoroutine(SéquencePremierBarrage());
        }

        private IEnumerator SéquencePremierBarrage()
        {
            Debug.Log($"[PremierBarrageController] SéquencePremierBarrage — attente de {délaiAvantAffichageDemande}s...");
            yield return new WaitForSeconds(délaiAvantAffichageDemande);

            if (affichageDemande != null)
            {
                Debug.Log("[PremierBarrageController] LancerDirectement() →");
                affichageDemande.LancerDirectement();
            }
            else
            {
                Debug.LogError("[PremierBarrageController] affichageDemande non assigné — " +
                               "la demande ne sera pas affichée et le retour sur MapRoad ne se déclenchera pas.");
            }
        }
    }
}
