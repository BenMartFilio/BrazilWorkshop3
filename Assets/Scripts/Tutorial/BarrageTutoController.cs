using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Bootstrap controller specific to the BarrageTuto scene.
    ///
    /// Runs before PremierBarrageController (order -100) and MainDuGardeUI (order -50)
    /// so it can inject a fixed 1×ITA demand into ListeAttenteGarde and mark the
    /// DonnéesSession as valid, making the rest of the barrage pipeline treat this
    /// as a normal barrage that requires the player to submit a real document.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class BarrageTutoController : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("ListeAttenteGarde ScriptableObject to inject the fixed demand into.")]
        [SerializeField] private ListeAttenteGarde listeAttenteGarde;

        [Tooltip("DonnéesSession ScriptableObject — marked valid so PremierBarrageController and MainDuGardeUI skip first-barrage logic.")]
        [SerializeField] private DonnéesSession donnéesSession;

        [Header("Demande fixe")]
        [Tooltip("Document type required to validate the tutorial barrage.")]
        [SerializeField] private FormulaireType typeRequis = FormulaireType.ReclassificationDesIndividus;

        [Tooltip("Number of documents of that type required (keep at 1 for the tutorial).")]
        [SerializeField, Min(1)] private int quantité = 1;

        private void Awake()
        {
            if (donnéesSession == null)
            {
                Debug.LogError("[BarrageTutoController] donnéesSession not assigned.");
                return;
            }

            if (listeAttenteGarde == null)
            {
                Debug.LogError("[BarrageTutoController] listeAttenteGarde not assigned.");
                return;
            }

            // Build the fixed demand array and write it into DonnéesSession so that
            // MainDuGardeUI.Awake() → ChargerDepuisSession() picks it up correctly.
            var demande = new FormulaireType[quantité];
            for (int i = 0; i < quantité; i++)
                demande[i] = typeRequis;

            donnéesSession.prochaineDemandeBarrage = demande;
            donnéesSession.sessionValide           = true;

            Debug.Log($"[BarrageTutoController] Injected fixed demand: {quantité}×{typeRequis}. " +
                      $"sessionValide set to true — normal barrage flow will apply.");
        }
    }
}
