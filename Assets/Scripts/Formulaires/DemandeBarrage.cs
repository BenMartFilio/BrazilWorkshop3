using System;
using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// ScriptableObject représentant la liste des formulaires requis pour valider un barrage.
    /// Stocke une liste ordonnée de types à fournir, tirée aléatoirement entre 4 et 6 items
    /// répartis sur les quatre types disponibles.
    /// </summary>
    [CreateAssetMenu(fileName = "DemandeBarrage", menuName = "Barrage/Demande Barrage")]
    public class DemandeBarrage : ScriptableObject
    {
        private const int QUANTITE_MIN = 4;
        private const int QUANTITE_MAX = 6;

        /// <summary>Déclenché chaque fois qu'une nouvelle demande est générée.</summary>
        public event Action OnDemandeRegénérée;

        /// <summary>Liste ordonnée des formulaires requis pour ce barrage.</summary>
        public IReadOnlyList<FormulaireType> Formulaires => _formulaires;

        private readonly List<FormulaireType> _formulaires = new();

        /// <summary>
        /// Génère aléatoirement une nouvelle demande entre QUANTITE_MIN et QUANTITE_MAX formulaires,
        /// répartis aléatoirement sur les quatre types.
        /// </summary>
        public void Régénérer()
        {
            _formulaires.Clear();

            int total = UnityEngine.Random.Range(QUANTITE_MIN, QUANTITE_MAX + 1);
            var types = (FormulaireType[])Enum.GetValues(typeof(FormulaireType));

            for (int i = 0; i < total; i++)
                _formulaires.Add(types[UnityEngine.Random.Range(0, types.Length)]);

            OnDemandeRegénérée?.Invoke();
        }

        /// <summary>Retourne le nombre de formulaires d'un type donné dans la demande.</summary>
        public int CompterType(FormulaireType type)
        {
            int count = 0;
            foreach (var f in _formulaires)
                if (f == type) count++;
            return count;
        }
    }
}
