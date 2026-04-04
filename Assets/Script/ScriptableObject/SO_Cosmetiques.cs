// Assets/Script/ScriptableObject/SO_Cosmetiques.cs

using System.Collections.Generic;
using UnityEngine;

public enum CategorieCosmetique { SkinPlayer }

[System.Serializable]
public class DefinitionCosmetique
{
    [Tooltip("Identifiant unique — doit correspondre à CosmetiqueEntry.identifiant.")]
    public string identifiant;

    public string nomAffichage;

    public Sprite sprite;

    [TextArea(2, 4)]
    public string description;

    public CategorieCosmetique categorie;
}

[CreateAssetMenu(fileName = "SO_Cosmetiques", menuName = "Scriptable Objects/SO_Cosmetiques")]
public class SO_Cosmetiques : ScriptableObject
{
    public List<DefinitionCosmetique> cosmetiques = new List<DefinitionCosmetique>();

    /// <summary>Retourne la définition d'un cosmétique par son identifiant, ou null.</summary>
    public DefinitionCosmetique ObtenirDefinition(string identifiant)
    {
        foreach (DefinitionCosmetique def in cosmetiques)
            if (def.identifiant == identifiant) return def;

        Debug.LogWarning($"[SO_Cosmetiques] Introuvable : '{identifiant}'");
        return null;
    }

    /// <summary>Retourne tous les cosmétiques d'une catégorie donnée, dans leur ordre de liste.</summary>
    public List<DefinitionCosmetique> ObtenirParCategorie(CategorieCosmetique categorie)
    {
        List<DefinitionCosmetique> resultat = new List<DefinitionCosmetique>();
        foreach (DefinitionCosmetique def in cosmetiques)
            if (def.categorie == categorie) resultat.Add(def);
        return resultat;
    }
}
