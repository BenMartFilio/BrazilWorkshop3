using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Bootstrap controller specific to the BarrageTuto scene.
    ///
    /// Runs before PremierBarrageController (order -100) and MainDuGardeUI (order -50).
    /// Initialises FormulaireLibreManager's inventory from its own quantitésInitiales,
    /// then writes the matching demand into DonnéesSession so the guard pipeline is consistent.
    /// No extra Inspector wiring required.
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

            // Initialise FormulaireLibreManager's inventory from its quantitésInitiales,
            // then force-add the required documents so the spawn is guaranteed.
            FormulaireLibreManager flm = FindAnyObjectByType<FormulaireLibreManager>();
            if (flm != null)
            {
                FormulaireInventaire inv = flm.ObtenirInventaire();
                if (inv != null)
                {
                    inv.InitialiserInventaire();
                    inv.Ajouter(typeRequis, quantité);
                    Debug.Log($"[BarrageTutoController] Inventaire initialisé → {quantité}×{typeRequis} injecté.");
                }
                else
                {
                    Debug.LogError("[BarrageTutoController] FormulaireLibreManager.ObtenirInventaire() retourne null.");
                }
            }
            else
            {
                Debug.LogError("[BarrageTutoController] FormulaireLibreManager introuvable dans la scène.");
            }

            // Write into DonnéesSession so the guard-side pipeline (MainDuGardeUI) is consistent.
            var demande = new FormulaireType[quantité];
            for (int i = 0; i < quantité; i++)
                demande[i] = typeRequis;

            donnéesSession.prochaineDemandeBarrage = demande;
            donnéesSession.sessionValide           = true;

            Debug.Log($"[BarrageTutoController] DonnéesSession → {quantité}×{typeRequis}, sessionValide=true.");
        }
    }
}
