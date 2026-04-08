// Assets/Script/ScriptableObject/SO_Cosmetiques.cs

using System.Collections.Generic;
using Drakensland.Localization;
using UnityEngine;

public enum CategorieCosmetique { SkinPlayer }

[System.Serializable]
public class DefinitionCosmetique
{
    [Tooltip("Identifiant unique � doit correspondre � CosmetiqueEntry.identifiant.")]
    public string identifiant;

    public string nomAffichage;

    [Tooltip("Clé de localisation pour le nom. Ex: 'skin_dragon_nom'. Si vide, nomAffichage est utilisé.")]
    public string cleNom;

    public Sprite sprite;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Clé de localisation pour la description. Ex: 'skin_dragon_desc'. Si vide, description est utilisé.")]
    public string cleDescription;

    public CategorieCosmetique categorie;

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

[CreateAssetMenu(fileName = "SO_Cosmetiques", menuName = "Scriptable Objects/SO_Cosmetiques")]
public class SO_Cosmetiques : ScriptableObject
{
    public List<DefinitionCosmetique> cosmetiques = new List<DefinitionCosmetique>();

    /// <summary>Retourne la d�finition d'un cosm�tique par son identifiant, ou null.</summary>
    public DefinitionCosmetique ObtenirDefinition(string identifiant)
    {
        foreach (DefinitionCosmetique def in cosmetiques)
            if (def.identifiant == identifiant) return def;

        Debug.LogWarning($"[SO_Cosmetiques] Introuvable : '{identifiant}'");
        return null;
    }

    /// <summary>Retourne tous les cosm�tiques d'une cat�gorie donn�e, dans leur ordre de liste.</summary>
    public List<DefinitionCosmetique> ObtenirParCategorie(CategorieCosmetique categorie)
    {
        List<DefinitionCosmetique> resultat = new List<DefinitionCosmetique>();
        foreach (DefinitionCosmetique def in cosmetiques)
            if (def.categorie == categorie) resultat.Add(def);
        return resultat;
    }
}
