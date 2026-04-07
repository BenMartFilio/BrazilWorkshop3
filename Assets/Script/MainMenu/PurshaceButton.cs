using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>Triggers an IAP purchase when the button is clicked.</summary>
public class PurchaseButton : MonoBehaviour
{
    [SerializeField] private IAPManager.ProductKey productKey = IAPManager.ProductKey.Coins100;
    [SerializeField] private TMP_Text priceLabel; // Optionnel : affiche le prix localisé

    [Header("Panel de succès")]
    [Tooltip("Panel à activer une fois l'objet attribué.")]
    [SerializeField] private GameObject successPanel;
    [Tooltip("Image enfant du panel qui recevra le sprite de l'objet.")]
    [SerializeField] private Image successItemIcon;
    [SerializeField] private Sprite successSprite;

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

        IAPManager.Instance.OnPurchaseSuccess += OnPuchased;

        IAPManager.Instance.BuyProduct(productKey);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess -= OnPuchased;
        }
    }

    private void OnDisable()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess -= OnPuchased;
        }
    }

    private void OnPuchased(string obj)
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess -= OnPuchased;
        }
        OpenSuccessPanel(successSprite);
    }

    private void OpenSuccessPanel(Sprite itemSprite)
    {
        if (successPanel == null) return;

        if (successItemIcon != null)
            successItemIcon.sprite = itemSprite;

        successPanel.SetActive(true);
    }
}