using System;
using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// ScriptableObject représentant la liste des formulaires requis pour valider un barrage.
    /// Génère une demande de N types UNIQUES tirés aléatoirement parmi les quatre disponibles
    /// (N entre QUANTITE_MIN et le total disponible).
    /// Aucun doublon : 1 icône affichée = exactement 1 formulaire à remettre.
    /// </summary>
    [CreateAssetMenu(fileName = "DemandeBarrage", menuName = "Barrage/Demande Barrage")]
    public class DemandeBarrage : ScriptableObject
    {
        /// <summary>Nombre minimum de types uniques demandés par barrage.</summary>
        private const int QUANTITE_MIN = 2;

        /// <summary>Déclenché chaque fois qu'une nouvelle demande est générée.</summary>
        public event Action OnDemandeRegénérée;

        /// <summary>Liste ordonnée des formulaires requis pour ce barrage.</summary>
        public IReadOnlyList<FormulaireType> Formulaires => _formulaires;

        private readonly List<FormulaireType> _formulaires = new();

        /// <summary>
        /// Génère une demande aléatoire de N types UNIQUES (N entre QUANTITE_MIN
        /// et le nombre total de FormulaireType disponibles).
        /// Chaque type n'apparaît qu'une seule fois :
        /// le nombre d'icônes affiché est identique au nombre de formulaires à remettre.
        /// </summary>
        public void Régénérer()
        {
            _formulaires.Clear();

            // Pool de tous les types disponibles.
            var pool = new List<FormulaireType>((FormulaireType[])Enum.GetValues(typeof(FormulaireType)));

            // Nombre de types à demander (min → max disponible).
            int total = UnityEngine.Random.Range(QUANTITE_MIN, pool.Count + 1);

            // Mélange Fisher-Yates pour un tirage sans remise.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            for (int i = 0; i < total; i++)
                _formulaires.Add(pool[i]);

            Debug.Log($"[DemandeBarrage] ★ Régénérer() → {_formulaires.Count} types uniques : " +
                      string.Join(", ", _formulaires));

            OnDemandeRegénérée?.Invoke();
        }

        /// <summary>Retourne le nombre de formulaires d'un type donné dans la demande (0 ou 1 avec la nouvelle logique sans doublon).</summary>
        public int CompterType(FormulaireType type)
        {
            int count = 0;
            foreach (var f in _formulaires)
                if (f == type) count++;
            return count;
        }
    }
}
