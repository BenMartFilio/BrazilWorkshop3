using System.Collections.Generic;
using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Gère une pile de formulaires dans une poche de la PartieBasse.
    /// Seul le formulaire au sommet de la pile peut être attrapé.
    /// Les cartes ont une taille fixe héritée de leur prefab source ; un léger décalage
    /// vertical donne un effet visuel de profondeur entre les cartes empilées.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PocheUI : MonoBehaviour
    {
        private const float OFFSET_PROFONDEUR = 5f;

        public RectTransform RectTransform { get; private set; }

        private readonly List<FormulaireUI> _pile = new();

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        /// <summary>Retourne le formulaire au sommet de la pile (le plus récent, visible au-dessus).</summary>
        public FormulaireUI SommetPile => _pile.Count > 0 ? _pile[^1] : null;

        /// <summary>Retourne true si ce formulaire est au sommet de la pile.</summary>
        public bool EstAuSommet(FormulaireUI formulaire) => SommetPile == formulaire;

        /// <summary>Ajoute un formulaire au sommet de la pile. Retourne son index dans la pile.</summary>
        public int AjouterFormulaire(FormulaireUI formulaire)
        {
            if (_pile.Contains(formulaire))
                return _pile.IndexOf(formulaire);

            _pile.Add(formulaire);
            formulaire.AssignerPoche(this);
            return _pile.Count - 1;
        }


        /// <summary>Détruit toutes les cartes de la pile. Appelé lors d'un revive.</summary>
        public void Vider()
        {
            // Copier la liste pour éviter la modification pendant l'itération
            var copies = new System.Collections.Generic.List<FormulaireUI>(_pile);
            _pile.Clear();

            foreach (var formulaire in copies)
            {
                if (formulaire != null)
                    Destroy(formulaire.gameObject);
            }
        }


        /// <summary>Retire un formulaire de la pile et repositionne les cartes restantes.</summary>
        public void RetirerFormulaire(FormulaireUI formulaire)
        {
            if (!_pile.Remove(formulaire)) return;

            formulaire.AssignerPoche(null);
            RafraichirDisposition();
        }

        /// <summary>
        /// Retourne la position anchoredPosition pour un index dans la pile.
        /// Le sommet est centré ; les cartes inférieures sont légèrement décalées vers le bas.
        /// </summary>
        public Vector2 ObtenirPositionPourIndex(int index)
        {
            int depthFromTop = (_pile.Count - 1) - index;
            return new Vector2(0f, depthFromTop * -OFFSET_PROFONDEUR);
        }

        /// <summary>Repositionne tous les formulaires de la pile selon leur index.</summary>
        public void RafraichirDisposition()
        {
            for (int i = 0; i < _pile.Count; i++)
            {
                if (_pile[i] == null) continue;
                _pile[i].GetComponent<RectTransform>().anchoredPosition = ObtenirPositionPourIndex(i);
            }
        }
    }
}

