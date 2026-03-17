using System;
using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// ScriptableObject qui mémorise la quantité de chaque formulaire détenu par le joueur.
    /// Persistant entre les scènes, réinitialisable via ResetInventaire().
    /// </summary>
    [CreateAssetMenu(fileName = "FormulaireInventaire", menuName = "Barrage/Formulaire Inventaire")]
    public class FormulaireInventaire : ScriptableObject
    {
        [Serializable]
        public class EntreeFormulaire
        {
            public FormulaireType type;
            [Min(0)]
            public int quantité;
        }

        [Tooltip("Quantités initiales de chaque formulaire au démarrage.")]
        [SerializeField]
        private List<EntreeFormulaire> quantitésInitiales = new();

        private Dictionary<FormulaireType, int> _inventaire = new();

        /// <summary>Événement déclenché lorsque la quantité d'un formulaire change.</summary>
        public event Action<FormulaireType, int> OnQuantitéChangée;

        private void OnEnable()
        {
            InitialiserInventaire();
        }

        /// <summary>Initialise l'inventaire à partir des valeurs configurées dans l'Inspector.</summary>
        public void InitialiserInventaire()
        {
            _inventaire.Clear();

            foreach (FormulaireType type in Enum.GetValues(typeof(FormulaireType)))
            {
                _inventaire[type] = 0;
            }

            foreach (var entrée in quantitésInitiales)
            {
                _inventaire[entrée.type] = Mathf.Max(0, entrée.quantité);
            }
        }

        /// <summary>Retourne la quantité détenue pour un type de formulaire donné.</summary>
        public int ObtenirQuantité(FormulaireType type)
        {
            return _inventaire.TryGetValue(type, out int quantité) ? quantité : 0;
        }

        /// <summary>Ajoute une quantité de formulaires au joueur.</summary>
        public void Ajouter(FormulaireType type, int quantité = 1)
        {
            if (quantité <= 0) return;

            _inventaire[type] = ObtenirQuantité(type) + quantité;
            OnQuantitéChangée?.Invoke(type, _inventaire[type]);
        }

        /// <summary>
        /// Retire une quantité de formulaires. Retourne true si le joueur en possédait assez.
        /// </summary>
        public bool Retirer(FormulaireType type, int quantité = 1)
        {
            if (quantité <= 0) return false;

            int actuelle = ObtenirQuantité(type);
            if (actuelle < quantité) return false;

            _inventaire[type] = actuelle - quantité;
            OnQuantitéChangée?.Invoke(type, _inventaire[type]);
            return true;
        }

        /// <summary>Vérifie si le joueur possède au moins une quantité donnée d'un formulaire.</summary>
        public bool Possède(FormulaireType type, int quantité = 1)
        {
            return ObtenirQuantité(type) >= quantité;
        }

        /// <summary>Remet tous les compteurs à zéro.</summary>
        public void ResetInventaire()
        {
            InitialiserInventaire();

            foreach (FormulaireType type in Enum.GetValues(typeof(FormulaireType)))
            {
                OnQuantitéChangée?.Invoke(type, 0);
            }
        }
    }
}
