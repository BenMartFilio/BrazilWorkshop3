using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// Gestionnaire central des formulaires. Relie les FormulaireData à l'inventaire du joueur
    /// et expose les opérations d'ajout, de retrait et de consultation.
    /// </summary>
    public class FormulaireManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Liste des données de chaque type de formulaire (un par type).")]
        [SerializeField] private List<FormulaireData> formulairesData = new();

        [Tooltip("Inventaire ScriptableObject du joueur.")]
        [SerializeField] private FormulaireInventaire inventaire;

        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();

        private void Awake()
        {
            ValiderConfiguration();
            IndexerFormulaires();
            inventaire.InitialiserInventaire();
        }

        private void ValiderConfiguration()
        {
            if (inventaire == null)
            {
                Debug.LogError("[FormulaireManager] Aucun FormulaireInventaire assigné.", this);
                return;
            }

            if (formulairesData == null || formulairesData.Count == 0)
            {
                Debug.LogWarning("[FormulaireManager] Aucun FormulaireData configuré.", this);
            }
        }

        private void IndexerFormulaires()
        {
            _dataParType.Clear();

            foreach (var data in formulairesData.Where(d => d != null))
            {
                if (_dataParType.ContainsKey(data.type))
                {
                    Debug.LogWarning($"[FormulaireManager] Doublon détecté pour le type : {data.type}", this);
                    continue;
                }

                _dataParType[data.type] = data;
            }
        }

        // ── API publique ────────────────────────────────────────────────────────

        /// <summary>Ajoute des formulaires à l'inventaire du joueur.</summary>
        public void Ajouter(FormulaireType type, int quantité = 1)
        {
            inventaire.Ajouter(type, quantité);
            Debug.Log($"[FormulaireManager] +{quantité} {type} → total : {inventaire.ObtenirQuantité(type)}");
        }

        /// <summary>
        /// Retire des formulaires de l'inventaire. Retourne true si la quantité était suffisante.
        /// </summary>
        public bool Retirer(FormulaireType type, int quantité = 1)
        {
            bool succès = inventaire.Retirer(type, quantité);

            if (succès)
                Debug.Log($"[FormulaireManager] -{quantité} {type} → total : {inventaire.ObtenirQuantité(type)}");
            else
                Debug.LogWarning($"[FormulaireManager] Stock insuffisant pour {type} (possédé : {inventaire.ObtenirQuantité(type)}, demandé : {quantité})");

            return succès;
        }

        /// <summary>Retourne la quantité détenue pour un type donné.</summary>
        public int ObtenirQuantité(FormulaireType type) => inventaire.ObtenirQuantité(type);

        /// <summary>Retourne true si le joueur possède au moins la quantité demandée.</summary>
        public bool Possède(FormulaireType type, int quantité = 1) => inventaire.Possède(type, quantité);

        /// <summary>Retourne le prefab associé à un type de formulaire, ou null s'il n'est pas configuré.</summary>
        public GameObject ObtenirPrefab(FormulaireType type)
        {
            return _dataParType.TryGetValue(type, out var data) ? data.prefab : null;
        }

        /// <summary>Remet l'inventaire entier à zéro.</summary>
        public void ResetInventaire() => inventaire.ResetInventaire();
    }
}
