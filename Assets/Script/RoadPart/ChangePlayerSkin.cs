// Assets/Script/RoadPart/ChangePlayerSkin.cs

using System.Collections.Generic;
using UnityEngine;

public class ChangePlayerSkin : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas playerDatas;
    [SerializeField] private SO_Cosmetiques catalogue;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        ChangeActualSkin();
    }

    /// <summary>
    /// Applique le skin équipé (skinEquiped = index dans la liste SkinPlayer du catalogue).
    /// </summary>
    public void ChangeActualSkin()
    {
        if (playerDatas == null || catalogue == null) return;

        List<DefinitionCosmetique> skins = catalogue.ObtenirParCategorie(CategorieCosmetique.SkinPlayer);

        int index = playerDatas.skinEquiped;
        if (index < 0 || index >= skins.Count)
        {
            Debug.LogWarning($"[ChangePlayerSkin] Index skin {index} hors limites ({skins.Count} skins).");
            return;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.sprite = skins[index].sprite;
    }
}
