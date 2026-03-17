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
    }
}
