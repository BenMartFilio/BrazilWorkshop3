using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Attribue 1 unité de l'objet à l'index <see cref="itemIndex"/> dans le catalogue
/// <see cref="SO_InventaireObjets"/>, puis sauvegarde les données joueur.
/// À brancher sur le UnityEvent <c>onPurchaseSuccess</c> de <see cref="CoinPurchaseButton"/>.
/// </summary>
public class ShopItemGranter : MonoBehaviour
{
    [Header("Données")]
    [SerializeField] private SO_PlayerDatas playerDatas;
    [SerializeField] private SO_InventaireObjets itemCatalogue;

    [Header("Objet à attribuer")]
    [Tooltip("Index de l'objet dans la liste du catalogue SO_InventaireObjets.")]
    [SerializeField] private int itemIndex = 0;

    [Header("Événements")]
    [SerializeField] private UnityEvent onGrantSuccess;
    [SerializeField] private UnityEvent onGrantFailed;

    [Header("Panel de succès")]
    [Tooltip("Panel à activer une fois l'objet attribué.")]
    [SerializeField] private GameObject successPanel;
    [Tooltip("Image enfant du panel qui recevra le sprite de l'objet.")]
    [SerializeField] private Image successItemIcon;


    /// <summary>Attribue 1 unité de l'objet défini par itemIndex et sauvegarde.</summary>
    public void GrantItem()
    {
        if (!ValidateSetup()) return;

        if (itemIndex < 0 || itemIndex >= itemCatalogue.objets.Count)
        {
            Debug.LogError($"[ShopItemGranter] itemIndex {itemIndex} hors des limites du catalogue ({itemCatalogue.objets.Count} objets).", this);
            onGrantFailed?.Invoke();
            return;
        }

        DefinitionObjetSpecial definition = itemCatalogue.objets[itemIndex];
        AddToInventory(definition.identifiant);
        OpenSuccessPanel(definition.sprite);

        playerDatas.SaveDatas();
        onGrantSuccess?.Invoke();
    }

    private void AddToInventory(string identifiant)
    {
        // S'assure qu'il existe au moins un groupe d'inventaire
        if (playerDatas.allObjectInInventory.Count == 0)
            playerDatas.allObjectInInventory.Add(new InventoryObject());

        InventoryObject groupe = playerDatas.allObjectInInventory[0];

        foreach (InventoryEntry entree in groupe.highScores)
        {
            if (entree.objectName == identifiant)
            {
                entree.quantity += 1;
                Debug.Log($"[ShopItemGranter] +1 '{identifiant}' → quantité : {entree.quantity}");
                return;
            }
        }

        // Entrée inexistante : on la crée avec 1 exemplaire
        groupe.highScores.Add(new InventoryEntry(identifiant, 1));
        Debug.Log($"[ShopItemGranter] Nouvelle entrée '{identifiant}' ajoutée à l'inventaire.");
    }

    private bool ValidateSetup()
    {
        if (playerDatas == null)
        {
            Debug.LogError("[ShopItemGranter] SO_PlayerDatas non assigné.", this);
            onGrantFailed?.Invoke();
            return false;
        }
        if (itemCatalogue == null)
        {
            Debug.LogError("[ShopItemGranter] SO_InventaireObjets non assigné.", this);
            onGrantFailed?.Invoke();
            return false;
        }
        return true;
    }

    private void OpenSuccessPanel(Sprite itemSprite)
    {
        if (successPanel == null) return;

        if (successItemIcon != null)
            successItemIcon.sprite = itemSprite;

        successPanel.SetActive(true);
    }
}
