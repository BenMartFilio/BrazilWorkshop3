using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// Données de configuration d'un type de formulaire : son type et le prefab visuel associé.
    /// </summary>
    [CreateAssetMenu(fileName = "FormulaireData", menuName = "Barrage/Formulaire Data")]
    public class FormulaireData : ScriptableObject
    {
        [Tooltip("Type de formulaire correspondant à cet asset.")]
        public FormulaireType type;

        [Tooltip("Prefab visuel représentant ce formulaire dans la scène.")]
        public GameObject prefab;

        [Tooltip("Taille d'affichage en pixels UI (width x height). Doit correspondre aux proportions du sprite.")]
        public Vector2 taille = new Vector2(100f, 100f);

        /// <summary>
        /// Extrait la première texture non-null depuis un RawImage du prefab sans l'instancier.
        /// Parcourt tous les enfants (y compris inactifs) pour éviter qu'un RawImage
        /// sans texture en tête de hiérarchie ne masque le vrai visuel.
        /// Retourne null si le prefab est absent ou qu'aucun RawImage n'a de texture.
        /// </summary>
        public Texture2D ExtraireTexture()
        {
            if (prefab == null) return null;

            foreach (var raw in prefab.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
            {
                if (raw.texture is Texture2D tex)
                    return tex;
            }

            return null;
        }
    }
}
