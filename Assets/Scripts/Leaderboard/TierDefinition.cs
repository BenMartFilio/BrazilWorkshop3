using UnityEngine;

/// <summary>
/// Données d'un tier de classement (identifiant UGS, nom affiché, couleur associée).
/// </summary>
[CreateAssetMenu(fileName = "TierDefinition", menuName = "Drakensland/Leaderboard/Tier Definition")]
public class TierDefinition : ScriptableObject
{
    [Tooltip("ID exact du tier dans le dashboard UGS (ex : 'bronze').")]
    public string tierId;

    [Tooltip("Nom affiché dans l'UI (ex : 'Bronze').")]
    public string displayName;

    [Tooltip("Couleur associée aux scores et au bouton de ce tier.")]
    public Color color = Color.white;

    [Tooltip("Sprite optionnel pour l'icône du tier.")]
    public Sprite icon;
}
