using System;
using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// ScriptableObject représentant la liste des formulaires requis pour valider un barrage.
    /// La complexité de la demande évolue selon le nombre de barrages complétés :
    ///   - Barrages 1-2  : 2 formulaires, types uniques uniquement
    ///   - Barrages 3-5  : 2 à 3 formulaires, avec possibilité de doublons à partir du 3e
    ///   - Barrage 6+    : 3 à 4 formulaires, doublons autorisés (2 similaires + d'autres)
    /// </summary>
    [CreateAssetMenu(fileName = "DemandeBarrage", menuName = "Barrage/Demande Barrage")]
    public class DemandeBarrage : ScriptableObject
    {
        // ── Paliers de difficulté ─────────────────────────────────────────────

        /// <summary>Nombre de barrages complétés à partir duquel le palier intermédiaire s'active (3 formulaires max, doublons possibles).</summary>
        private const int PALIER_MOYEN = 3;

        /// <summary>Nombre de barrages complétés à partir duquel le palier difficile s'active (4 formulaires max, doublons garantis).</summary>
        private const int PALIER_DIFFICILE = 6;

        // ── Événements / données ──────────────────────────────────────────────

        /// <summary>Déclenché chaque fois qu'une nouvelle demande est générée.</summary>
        public event Action OnDemandeRegénérée;

        /// <summary>Liste ordonnée des formulaires requis pour ce barrage (peut contenir des doublons aux paliers élevés).</summary>
        public IReadOnlyList<FormulaireType> Formulaires => _formulaires;

        private readonly List<FormulaireType> _formulaires = new();

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Génère une demande aléatoire dont la complexité dépend du nombre de barrages déjà complétés.
        /// <para>Palier facile  (0–2 complétés) : 2 types UNIQUES.</para>
        /// <para>Palier moyen  (3–5 complétés) : 2 à 3 entrées, doublons possibles.</para>
        /// <para>Palier difficile (6+ complétés) : 3 à 4 entrées, au moins un doublon garanti.</para>
        /// </summary>
        /// <param name="barragesComplétés">Nombre de barrages validés depuis le début de la partie.</param>
        public void Régénérer(int barragesComplétés = 0)
        {
            _formulaires.Clear();

            if (barragesComplétés < PALIER_MOYEN)
                GénérerFacile();
            else if (barragesComplétés < PALIER_DIFFICILE)
                GénérerMoyen();
            else
                GénérerDifficile();

            Debug.Log($"[DemandeBarrage] ★ Régénérer(barrages={barragesComplétés}) " +
                      $"→ {_formulaires.Count} entrée(s) : {string.Join(", ", _formulaires)}");

            OnDemandeRegénérée?.Invoke();
        }

        /// <summary>Retourne le nombre de fois qu'un type donné apparaît dans la demande.</summary>
        public int CompterType(FormulaireType type)
        {
            int count = 0;
            foreach (var f in _formulaires)
                if (f == type) count++;
            return count;
        }

        // ── Générateurs par palier ────────────────────────────────────────────

        /// <summary>
        /// Palier facile : exactement 2 types UNIQUES tirés sans remise.
        /// </summary>
        private void GénérerFacile()
        {
            var pool = MélangerPool();
            _formulaires.Add(pool[0]);
            _formulaires.Add(pool[1]);
        }

        /// <summary>
        /// Palier moyen : 2 ou 3 entrées. Si 3, un doublon est possible (50 % de chance).
        /// </summary>
        private void GénérerMoyen()
        {
            var pool  = MélangerPool();
            int total = UnityEngine.Random.Range(2, 4); // 2 ou 3

            if (total == 2)
            {
                _formulaires.Add(pool[0]);
                _formulaires.Add(pool[1]);
            }
            else // total == 3
            {
                // 50 % : 3 types uniques | 50 % : un type en double + un autre
                if (UnityEngine.Random.value < 0.5f)
                {
                    _formulaires.Add(pool[0]);
                    _formulaires.Add(pool[1]);
                    _formulaires.Add(pool[2]);
                }
                else
                {
                    _formulaires.Add(pool[0]);
                    _formulaires.Add(pool[0]); // doublon
                    _formulaires.Add(pool[1]);
                }
            }
        }

        /// <summary>
        /// Palier difficile : 3 ou 4 entrées avec au moins un doublon garanti.
        /// </summary>
        private void GénérerDifficile()
        {
            var pool  = MélangerPool();
            int total = UnityEngine.Random.Range(3, 5); // 3 ou 4

            if (total == 3)
            {
                // 2 d'un type + 1 d'un autre
                _formulaires.Add(pool[0]);
                _formulaires.Add(pool[0]);
                _formulaires.Add(pool[1]);
            }
            else // total == 4
            {
                // 50 % : 2×type0 + 2×type1  |  50 % : 2×type0 + 1×type1 + 1×type2
                if (UnityEngine.Random.value < 0.5f)
                {
                    _formulaires.Add(pool[0]);
                    _formulaires.Add(pool[0]);
                    _formulaires.Add(pool[1]);
                    _formulaires.Add(pool[1]);
                }
                else
                {
                    _formulaires.Add(pool[0]);
                    _formulaires.Add(pool[0]);
                    _formulaires.Add(pool[1]);
                    _formulaires.Add(pool[2]);
                }
            }
        }

        // ── Utilitaire ────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne true si ce type est un objet spécial non demandable au barrage
        /// (LiasseDeBillets, FormulairePasePartout, BadgeDuGouvernement).
        /// </summary>
        private static bool EstObjetSpécial(FormulaireType type)
            => type == FormulaireType.LiasseDeBillets
            || type == FormulaireType.FormulairePasePartout
            || type == FormulaireType.BadgeDuGouvernement;

        /// <summary>
        /// Retourne une copie mélangée (Fisher-Yates) des FormulaireType STANDARDS uniquement.
        /// Les objets spéciaux sont exclus — ils n'ont pas de FormulaireData
        /// et ne peuvent jamais faire partie d'une demande de barrage.
        /// </summary>
        private List<FormulaireType> MélangerPool()
        {
            var pool = new List<FormulaireType>();
            foreach (FormulaireType t in Enum.GetValues(typeof(FormulaireType)))
            {
                if (!EstObjetSpécial(t))
                    pool.Add(t);
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            return pool;
        }
    }
}
