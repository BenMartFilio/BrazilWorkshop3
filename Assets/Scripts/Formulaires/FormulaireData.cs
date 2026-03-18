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

        /// <summary>
        /// Extrait la texture depuis la RawImage du prefab sans l'instancier dans la scène.
        /// Retourne null si le prefab ou la RawImage est absent.
        /// </summary>
        public Texture2D ExtraireTexture()
        {
            if (prefab == null) return null;
            var raw = prefab.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
            return raw != null ? raw.texture as Texture2D : null;
        }
    }
}
