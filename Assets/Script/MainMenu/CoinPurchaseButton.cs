using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class CoinPurchaseButton : MonoBehaviour
{
    [Header("Données")]
    [SerializeField] private SO_PlayerDatas playerDatas;
    [SerializeField] private int cost = 100;
    [SerializeField][TextArea] private string itemDescription;

    [Header("UI")]
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;

    [Header("Panel de confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmationDescriptionLabel;

    [Header("Événements")]
    [SerializeField] private UnityEvent onPurchaseSuccess;
    [SerializeField] private UnityEvent onPurchaseFailed;

    [SerializeField] private CoinsUpdater coinsUpdater;

    private Button _button;
    private int _cachedMonney = -1;

    #region Unity Lifecycle

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);

        // Panel fermé au départ
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void OnEnable() => RefreshButtonState();

    private void Start()
    {
        UpdatePriceLabel();
        RefreshButtonState();
    }

    private void Update()
    {
        if (playerDatas != null && playerDatas.generalMonney != _cachedMonney)
            RefreshButtonState();
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(OnButtonClicked);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
    }

    #endregion

    #region Logic

    private void OnButtonClicked()
    {
        if (!ValidateSetup()) return;

        if (!CanAfford())
        {
            Debug.LogWarning("[CoinPurchaseButton] Pièces insuffisantes.");
            onPurchaseFailed?.Invoke();
            return;
        }

        OpenConfirmationPanel();
    }

    private void OpenConfirmationPanel()
    {
        if (confirmationPanel == null)
        {
            // Pas de panel assigné : on achète directement
            ExecutePurchase();
            return;
        }
        if (confirmationDescriptionLabel != null)
            confirmationDescriptionLabel.text = itemDescription;

        confirmButton?.onClick.AddListener(OnConfirm);
        cancelButton?.onClick.AddListener(OnCancel);

        confirmationPanel.SetActive(true);
    }

    private void OnConfirm()
    {
        CloseConfirmationPanel();
        ExecutePurchase();
    }

    private void OnCancel()
    {
        CloseConfirmationPanel();
    }

    private void ExecutePurchase()
    {
        if (!ValidateSetup() || !CanAfford()) return;

        playerDatas.generalMonney -= cost;
        playerDatas.SaveDatas();
        coinsUpdater.AffichageCoin();
        onPurchaseSuccess?.Invoke();
        RefreshButtonState();
    }

    private void CloseConfirmationPanel()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
    }

    private void RefreshButtonState()
    {
        if (_button == null || playerDatas == null) return;

        bool affordable = CanAfford();
        _button.interactable = affordable;

        if (priceLabel != null)
            priceLabel.color = affordable ? affordableColor : unaffordableColor;

        _cachedMonney = playerDatas.generalMonney;
    }

    private void UpdatePriceLabel()
    {
        if (priceLabel != null)
            priceLabel.text = cost.ToString();
    }

    private bool CanAfford() => playerDatas != null && playerDatas.generalMonney >= cost;

    private bool ValidateSetup()
    {
        if (playerDatas != null) return true;
        Debug.LogError("[CoinPurchaseButton] SO_PlayerDatas non assigné !", this);
        return false;
    }

    #endregion

    #region Public API

    public void SetCost(int newCost)
    {
        cost = Mathf.Max(0, newCost);
        UpdatePriceLabel();
        RefreshButtonState();
    }

    #endregion
}