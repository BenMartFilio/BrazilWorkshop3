using System.Collections.Generic;
using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Gère une pile de formulaires dans une poche de la PartieBasse.
    /// Seul le formulaire au sommet de la pile peut être attrapé.
    /// Les cartes s'étirent pour remplir la poche ; les cartes inférieures ont un padding
    /// plus grand pour donner un effet visuel de profondeur.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PocheUI : MonoBehaviour
    {
        private const float PADDING_BASE = 8f;
        private const float OFFSET_PILE  = 6f;

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

        /// <summary>Retire un formulaire de la pile et repositionne les cartes restantes.</summary>
        public void RetirerFormulaire(FormulaireUI formulaire)
        {
            if (!_pile.Remove(formulaire)) return;

            formulaire.AssignerPoche(null);
            RafraichirDisposition();
        }

        /// <summary>
        /// Retourne les offsets RectTransform pour un index donné.
        /// Le sommet a le padding minimal ; les cartes dessous ont un padding croissant.
        /// </summary>
        public (Vector2 offsetMin, Vector2 offsetMax) ObtenirOffsetsPourIndex(int index)
        {
            int depthFromTop = (_pile.Count - 1) - index;
            float p = PADDING_BASE + depthFromTop * OFFSET_PILE;
            return (new Vector2(p, p), new Vector2(-p, -p));
        }

        /// <summary>Conservé pour la compatibilité avec l'animation de chute (les cartes stretch n'ont pas d'anchoredPosition).</summary>
        public Vector2 ObtenirPositionPourIndex(int index) => Vector2.zero;

        /// <summary>Repositionne tous les formulaires de la pile selon leur index.</summary>
        public void RafraichirDisposition()
        {
            for (int i = 0; i < _pile.Count; i++)
            {
                if (_pile[i] == null) continue;
                var rt = _pile[i].GetComponent<RectTransform>();
                var (oMin, oMax) = ObtenirOffsetsPourIndex(i);
                rt.offsetMin = oMin;
                rt.offsetMax = oMax;
            }
        }
    }
}

