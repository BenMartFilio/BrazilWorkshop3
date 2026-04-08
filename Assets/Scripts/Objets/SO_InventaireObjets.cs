using System.Collections.Generic;
using Drakensland.Localization;
using UnityEngine;

/// <summary>
/// Catalogue immuable des objets spéciaux : identifiant, nom d'affichage, sprite et mode passif.
/// Jamais modifié à l'exécution — injecté via Inspector.
/// </summary>
[System.Serializable]
public class DefinitionObjetSpecial
{
    [Tooltip("Clé correspondant à InventoryEntry.objectName dans SO_PlayerDatas.")]
    public string identifiant;

    [Tooltip("Nom affiché dans l'interface inventaire (fallback si cleNom est vide).")]
    public string nomAffichage;

    [Tooltip("Clé de localisation pour le nom. Ex: 'item_bouclier_nom'. Si vide, nomAffichage est utilisé.")]
    public string cleNom;

    public Sprite sprite;

    [Tooltip("Si vrai, l'objet est passif : affiché dans l'inventaire mais le bouton est désactivé (utilisation automatique par le système).")]
    public bool estPassif;

    [Tooltip("Description affichée lors du clic sur l'objet dans l'inventaire du menu principal (fallback si cleDescription est vide).")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Clé de localisation pour la description. Ex: 'item_bouclier_desc'. Si vide, description est utilisé.")]
    public string cleDescription;

    /// <summary>Retourne le nom localisé, avec fallback sur nomAffichage.</summary>
    public string ObtenirNomLocalise()
    {
        if (!string.IsNullOrEmpty(cleNom) && LocalizationManager.Instance != null)
            return LocalizationManager.Instance.Get(cleNom);
        return nomAffichage;
    }

    /// <summary>Retourne la description localisée, avec fallback sur description.</summary>
    public string ObtenirDescriptionLocalisee()
    {
        if (!string.IsNullOrEmpty(cleDescription) && LocalizationManager.Instance != null)
            return LocalizationManager.Instance.Get(cleDescription);
        return string.IsNullOrEmpty(description) ? nomAffichage : description;
    }
}

[CreateAssetMenu(fileName = "SO_InventaireObjets", menuName = "Scriptable Objects/SO_InventaireObjets")]
public class SO_InventaireObjets : ScriptableObject
{
    public List<DefinitionObjetSpecial> objets = new List<DefinitionObjetSpecial>();

    /// <summary>Retourne la définition correspondant à l'identifiant donné, ou null si introuvable.</summary>
    public DefinitionObjetSpecial ObtenirDefinition(string identifiant)
    {
        foreach (DefinitionObjetSpecial def in objets)
        {
            if (def.identifiant == identifiant)
                return def;
        }

        Debug.LogWarning($"[SO_InventaireObjets] Définition introuvable pour l'identifiant : '{identifiant}'");
        return null;
    }
}
