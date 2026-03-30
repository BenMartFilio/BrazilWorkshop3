using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// Singleton managing all in-app purchases via Unity IAP 5.x+.
/// Compatible with iOS (App Store) and Android (Google Play Billing Library 8+).
/// Requires: com.unity.purchasing >= 5.0.0
/// </summary>
public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    // --- Product IDs (must match App Store Connect / Google Play Console) ---
    public static class ProductIDs
    {
        public const string REMOVE_ADS = "com.mygame.remove_ads";
        public const string COINS_100 = "com.mygame.coins_100";   //mettre nom jeu et le truc de google play en gros com.LEJEU.LEPRODUIT (il peut aussi y avoir le nom de l'entreprise)
        public const string PREMIUM_PACK = "com.mygame.premium_pack";
    }

    public enum ProductKey { RemoveAds, Coins100, PremiumPack }

    // --- State ---
    private StoreController _store;
    public bool IsConnected { get; private set; }

    // --- Events ---
    public event Action<string> OnPurchaseSuccess;
    public event Action<string, string> OnPurchaseFailure;   // productId, reason
    public event Action<string> OnStoreConnectFailed;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    // -------------------------------------------------------------------------
    // Initialisation

    private async Task InitializeAsync()
    {
        try
        {
            _store = UnityIAPServices.StoreController();

            // Abonnement aux événements du store
            _store.OnPurchasePending += HandlePurchasePending;
            _store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            _store.OnPurchaseFailed += HandlePurchaseFailed;

            // Connexion au store (App Store / Google Play)
            await _store.Connect();

            IsConnected = true;
            Debug.Log("[IAP] Store connected.");

            FetchProducts();
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAP] Connection failed: {e.Message}");
            OnStoreConnectFailed?.Invoke(e.Message);
        }
    }

    private void FetchProducts()
    {
        var definitions = new List<ProductDefinition>
        {
            new(ProductIDs.REMOVE_ADS,   ProductType.NonConsumable),
            new(ProductIDs.COINS_100,    ProductType.Consumable),
            new(ProductIDs.PREMIUM_PACK, ProductType.Consumable),
        };

        _store.FetchProducts(definitions);
        Debug.Log("[IAP] Products fetched.");
    }

    // -------------------------------------------------------------------------
    // Achat

    /// <summary>Initiates a purchase using the typed enum key.</summary>
    public void BuyProduct(ProductKey key)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[IAP] Store not connected.");
            return;
        }

        string id = key switch
        {
            ProductKey.RemoveAds => ProductIDs.REMOVE_ADS,
            ProductKey.Coins100 => ProductIDs.COINS_100,
            ProductKey.PremiumPack => ProductIDs.PREMIUM_PACK,
            _ => null
        };

        if (id == null)
        {
            Debug.LogWarning($"[IAP] Unknown product key: {key}");
            return;
        }

        _store.PurchaseProduct(id);
    }

    /// <summary>Returns localized price string (e.g. "1,99 €") or empty string.</summary>
    public string GetLocalizedPrice(ProductKey key)
    {
        if (!IsConnected) return string.Empty;

        string id = key switch
        {
            ProductKey.RemoveAds => ProductIDs.REMOVE_ADS,
            ProductKey.Coins100 => ProductIDs.COINS_100,
            ProductKey.PremiumPack => ProductIDs.PREMIUM_PACK,
            _ => null
        };

        if (id == null) return string.Empty;

        var product = _store.GetProductById(id);
        return product?.metadata.localizedPriceString ?? string.Empty;
    }

    // -------------------------------------------------------------------------
    // Handlers du store

    private void HandlePurchasePending(PendingOrder order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        if (product == null) return;

        Debug.Log($"[IAP] Purchase pending: {product.definition.id}");

        // Pour les consumables : on peut accorder la récompense ici et confirmer.
        // Pour une validation serveur : NE PAS appeler ConfirmPurchase ici —
        // valider côté serveur d'abord, puis appeler ConfirmPurchase en callback.
        _store.ConfirmPurchase(order);
    }

    private void HandlePurchaseConfirmed(Order order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        if (product == null) return;

        var productId = product.definition.id;
        Debug.Log($"[IAP] Purchase confirmed: {productId}");

        GrantReward(productId);
        OnPurchaseSuccess?.Invoke(productId);
    }

    private void HandlePurchaseFailed(FailedOrder order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        var productId = product?.definition.id ?? "unknown";
        var reason = order.FailureReason.ToString();

        Debug.LogWarning($"[IAP] Purchase failed: {productId} — {reason}");
        OnPurchaseFailure?.Invoke(productId, reason);
    }

    // -------------------------------------------------------------------------
    // Récompenses

    private void GrantReward(string productId)
    {
        switch (productId)
        {
            case ProductIDs.REMOVE_ADS:
                // AdsManager.Instance.DisableAds();
                Debug.Log("[IAP] Ads removed.");
                break;

            case ProductIDs.COINS_100:
                // CurrencyManager.Instance.AddCoins(100);
                Debug.Log("[IAP] 100 coins granted.");
                break;

            case ProductIDs.PREMIUM_PACK:
                // UnlockPremiumContent();
                Debug.Log("[IAP] Premium pack unlocked.");
                break;

            default:
                Debug.LogWarning($"[IAP] Unhandled product: {productId}");
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Restauration des achats (iOS obligatoire)

    public void RestorePurchases()
    {
        if (!IsConnected) { Debug.LogWarning("[IAP] Store not connected."); return; }

        _store.RestoreTransactions((success, error) =>
        {
            if (success)
                Debug.Log("[IAP] Restore completed successfully.");
            else
                Debug.LogError($"[IAP] Restore failed: {error}");
        });
    }

    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        if (_store == null) return;
        _store.OnPurchasePending -= HandlePurchasePending;
        _store.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
        _store.OnPurchaseFailed -= HandlePurchaseFailed;
    }
}