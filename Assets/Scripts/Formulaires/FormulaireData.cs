using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// Données de configuration d'un type de formulaire : son type et le visuel associé.
    /// Pour les formulaires standards, renseigner prefab (RawImage avec texture).
    /// Pour les objets spéciaux (LiasseDeBillets, FormulairePasePartout, BadgeDuGouvernement),
    /// renseigner textureDirecte à la place — aucun prefab n'est nécessaire.
    /// </summary>
    [CreateAssetMenu(fileName = "FormulaireData", menuName = "Barrage/Formulaire Data")]
    public class FormulaireData : ScriptableObject
    {
        [Tooltip("Type de formulaire correspondant à cet asset.")]
        public FormulaireType type;

        [Tooltip("Prefab visuel représentant ce formulaire dans la scène. Ignoré si textureDirecte est assignée.")]
        public GameObject prefab;

        [Tooltip("Texture directe à utiliser à la place du prefab. " +
                 "Renseigner pour les objets spéciaux (LiasseDeBillets, FormulairePasePartout, BadgeDuGouvernement) " +
                 "qui n'ont pas de prefab dédié.")]
        public Texture2D textureDirecte;

        [Tooltip("Taille d'affichage en pixels UI (width x height). Doit correspondre aux proportions du sprite.")]
        public Vector2 taille = new Vector2(100f, 100f);

        /// <summary>
        /// Retourne la texture à utiliser pour ce formulaire.
        /// Priorité : textureDirecte si assignée, sinon première texture trouvée dans le prefab via RawImage.
        /// Retourne null si aucune source n'est disponible.
        /// </summary>
        public Texture2D ExtraireTexture()
        {
            if (textureDirecte != null)
                return textureDirecte;

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
