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
        public const string COINS_100 = "com.mygame.coins_100";
        public const string PREMIUM_PACK = "com.mygame.premium_pack";
    }

    public enum ProductKey { RemoveAds, Coins100, PremiumPack }

    // --- State ---
    private StoreController _store;
    public bool IsConnected { get; private set; }

    // --- Events ---
    public event Action<string> OnPurchaseSuccess;
    public event Action<string, string> OnPurchaseFailure;
    public event Action<string> OnStoreConnectFailed;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start() => await InitializeAsync();

    // -------------------------------------------------------------------------
    // Initialisation

    private async Task InitializeAsync()
    {
        try
        {
            _store = UnityIAPServices.StoreController();

            _store.OnPurchasePending += HandlePurchasePending;
            _store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            _store.OnPurchaseFailed += HandlePurchaseFailed;
            _store.OnPurchaseDeferred += HandlePurchaseDeferred;
            _store.OnProductsFetchFailed += HandleProductsFetchFailed;
            // Le bon type du delegate : Action<IEnumerable<Product>>
            _store.OnProductsFetched += HandleProductsFetched;

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

    // Signature corrigée : IEnumerable<Product> au lieu de ProductFetchedCollection
    private void HandleProductsFetched(IEnumerable<Product> products)
    {
        var list = products.ToList();
        Debug.Log($"[IAP] {list.Count} product(s) fetched successfully.");
        foreach (var product in list)
        {
            Debug.Log($"[IAP] Product ready: {product.definition.id} — {product.metadata.localizedPriceString}");
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
        Debug.Log("[IAP] FetchProducts called.");
    }

    // Achat

    public void BuyProduct(ProductKey key)
    {
        if (!IsConnected) { Debug.LogWarning("[IAP] Store not connected."); return; }

        string id = KeyToId(key);
        if (id == null) { Debug.LogWarning($"[IAP] Unknown product key: {key}"); return; }

        _store.PurchaseProduct(id);
    }

    public string GetLocalizedPrice(ProductKey key)
    {
        if (!IsConnected) return string.Empty;

        string id = KeyToId(key);
        if (id == null) return string.Empty;

        return _store.GetProductById(id)?.metadata.localizedPriceString ?? string.Empty;
    }

    // Helper centralisé pour éviter la duplication du switch
    private string KeyToId(ProductKey key) => key switch
    {
        ProductKey.RemoveAds => ProductIDs.REMOVE_ADS,
        ProductKey.Coins100 => ProductIDs.COINS_100,
        ProductKey.PremiumPack => ProductIDs.PREMIUM_PACK,
        _ => null
    };

    // Handlers du store

    private void HandlePurchasePending(PendingOrder order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        if (product == null) return;

        Debug.Log($"[IAP] Purchase pending: {product.definition.id}");
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

    private void HandlePurchaseDeferred(DeferredOrder order)
    {
        // Ask to Buy (iOS) — ne jamais accorder la récompense ici
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        Debug.Log($"[IAP] Purchase deferred (awaiting approval): {product?.definition.id}");
    }

    private void HandleProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogError($"[IAP] Products fetch failed: {failure.FailureReason}");
    }

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

    // Restauration (obligatoire iOS)

    public void RestorePurchases()
    {
        if (!IsConnected) { Debug.LogWarning("[IAP] Store not connected."); return; }

        _store.RestoreTransactions((success, error) =>
        {
            if (success) Debug.Log("[IAP] Restore completed successfully.");
            else Debug.LogError($"[IAP] Restore failed: {error}");
        });
    }


    private void OnDestroy()
    {
        if (_store == null) return;
        _store.OnPurchasePending -= HandlePurchasePending;
        _store.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
        _store.OnPurchaseFailed -= HandlePurchaseFailed;
        _store.OnPurchaseDeferred -= HandlePurchaseDeferred;
        _store.OnProductsFetchFailed -= HandleProductsFetchFailed;
        _store.OnProductsFetched -= HandleProductsFetched;
    }
}