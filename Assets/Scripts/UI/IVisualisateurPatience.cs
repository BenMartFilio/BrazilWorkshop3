using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Contrat que doit implémenter tout visualisateur de la barre de patience.
    /// Permet à BarrePatience de déléguer son affichage à n'importe quel composant
    /// (shader interne, Slider, etc.) sans modifier la logique de jeu.
    /// </summary>
    public interface IVisualisateurPatience
    {
        /// <summary>
        /// Met à jour l'affichage visuel en fonction de la valeur normalisée de la patience.
        /// Appelé à chaque frame par BarrePatience.
        /// </summary>
        /// <param name="fillNormalisé">Valeur entre 0 (vide) et 1 (pleine).</param>
        /// <param name="seuilMoyenne">Seuil en dessous duquel la couleur passe en orange (0–1).</param>
        /// <param name="seuilBasse">Seuil en dessous duquel la couleur passe en rouge (0–1).</param>
        /// <param name="couleurHaute">Couleur au-dessus du seuil orange.</param>
        /// <param name="couleurMoyenne">Couleur entre les deux seuils.</param>
        /// <param name="couleurBasse">Couleur en dessous du seuil rouge.</param>
        void MettreAJour(float fillNormalisé,
                         float seuilMoyenne,
                         float seuilBasse,
                         Color couleurHaute,
                         Color couleurMoyenne,
                         Color couleurBasse);

        /// <summary>
        /// Déclenche l'animation de flash (pénalité).
        /// </summary>
        void AnimerFlash(Color couleurFlash);
    }
}
