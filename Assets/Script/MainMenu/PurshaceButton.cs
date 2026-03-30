using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Triggers an IAP purchase when the button is clicked.</summary>
public class PurchaseButton : MonoBehaviour
{
    [SerializeField] private IAPManager.ProductKey productKey = IAPManager.ProductKey.Coins100;
    [SerializeField] private TMP_Text priceLabel; // Optionnel : affiche le prix localisé

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        if (priceLabel != null && IAPManager.Instance != null)
        {
            var price = IAPManager.Instance.GetLocalizedPrice(productKey);
            if (!string.IsNullOrEmpty(price))
                priceLabel.text = price;
        }
    }

    private void OnButtonClicked()
    {
        if (IAPManager.Instance == null)
        {
            Debug.LogError("[PurchaseButton] IAPManager not found in scene.");
            return;
        }
        IAPManager.Instance.BuyProduct(productKey);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }
}