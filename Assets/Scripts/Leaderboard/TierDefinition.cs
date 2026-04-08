using Drakensland.Localization;
using UnityEngine;

/// <summary>
/// Données d'un tier de classement (identifiant UGS, nom affiché, couleur associée).
/// </summary>
[CreateAssetMenu(fileName = "TierDefinition", menuName = "Drakensland/Leaderboard/Tier Definition")]
public class TierDefinition : ScriptableObject
{
    [Tooltip("ID exact du tier dans le dashboard UGS (ex : 'bronze').")]
    public string tierId;

    [Tooltip("Nom affiché dans l'UI (ex : 'Bronze'). Utilisé en fallback si cleNom est vide.")]
    public string displayName;

    [Tooltip("Clé de localisation pour le nom. Ex: 'tier_bronze'. Si vide, displayName est utilisé.")]
    public string cleNom;

    [Tooltip("Couleur associée aux scores et au bouton de ce tier.")]
    public Color color = Color.white;

    [Tooltip("Sprite optionnel pour l'icône du tier.")]
    public Sprite icon;

    /// <summary>Retourne le nom localisé, avec fallback sur displayName.</summary>
    public string ObtenirNomLocalise()
    {
        if (!string.IsNullOrEmpty(cleNom) && LocalizationManager.Instance != null)
            return LocalizationManager.Instance.Get(cleNom);
        return displayName;
    }
}
