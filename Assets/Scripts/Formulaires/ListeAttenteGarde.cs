using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// ScriptableObject définissant l'ordre exact des formulaires à remettre au garde.
    /// Le garde n'accepte que le prochain type attendu dans la séquence.
    /// </summary>
    [CreateAssetMenu(fileName = "ListeAttenteGarde", menuName = "Barrage/Liste Attente Garde")]
    public class ListeAttenteGarde : ScriptableObject
    {
        [Tooltip("Séquence ordonnée des formulaires à remettre au garde, du premier au dernier.")]
        [SerializeField] private List<FormulaireType> séquence = new();

        private int _indexCourant;

        /// <summary>Remet le compteur à zéro (utile au démarrage ou après un reset de niveau).</summary>
        public void Réinitialiser() => _indexCourant = 0;

        /// <summary>Retourne le type attendu à la position courante, ou null si la séquence est terminée.</summary>
        public FormulaireType? ProchainAttendu =>
            _indexCourant < séquence.Count ? séquence[_indexCourant] : (FormulaireType?)null;

        /// <summary>
        /// Valide le formulaire remis.
        /// Retourne true et avance l'index si c'est le bon type au bon moment.
        /// Retourne false si le type est incorrect ou si la séquence est déjà terminée.
        /// </summary>
        public bool ValiderProchain(FormulaireType type)
        {
            if (_indexCourant >= séquence.Count) return false;
            if (séquence[_indexCourant] != type)  return false;

            _indexCourant++;
            return true;
        }

        /// <summary>True quand tous les formulaires de la séquence ont été remis.</summary>
        public bool EstTerminée => _indexCourant >= séquence.Count;
    }
}
