using UnityEngine;

public class PurchasedFinishedAndUpdateMonney : MonoBehaviour
{
    [SerializeField] private CoinsUpdater coinsUpdater;
    [SerializeField] private GameObject successPanel;

    /// <summary>À brancher sur IAPManager.OnPurchaseSuccess via le panneau Inspector ou du code.</summary>
    public void OnAchatTermine(string productId)
    {
        coinsUpdater?.AffichagePremium();
        if (successPanel != null)
            successPanel.SetActive(true);
    }
}
