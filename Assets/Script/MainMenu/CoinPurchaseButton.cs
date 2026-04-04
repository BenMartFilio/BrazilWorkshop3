// CoinPurchaseButton.cs — remplace le Update par un event
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
    [SerializeField] private AudioEventDispatcher audioEventDispatcher;
    [SerializeField] private AudioType purchaseSound;
    [SerializeField] private AudioType failSound;

    private Button _button;

    #region Unity Lifecycle

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void Start()
    {
        UpdatePriceLabel();
        RefreshButtonState();
    }

    private void OnEnable()
    {
        // S'abonne à l'event monnaie au lieu de poller dans Update
        if (playerDatas != null)
            playerDatas.OnMonneyChanged += RefreshButtonState;
        RefreshButtonState();
    }

    private void OnDisable()
    {
        if (playerDatas != null)
            playerDatas.OnMonneyChanged -= RefreshButtonState;
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
            audioEventDispatcher?.PlayAudio(failSound);
            onPurchaseFailed?.Invoke();
            return;
        }
        OpenConfirmationPanel();
    }

    private void OpenConfirmationPanel()
    {
        if (confirmationPanel == null) { ExecutePurchase(); return; }

        if (confirmationDescriptionLabel != null)
            confirmationDescriptionLabel.text = itemDescription;

        confirmButton?.onClick.AddListener(OnConfirm);
        cancelButton?.onClick.AddListener(OnCancel);
        confirmationPanel.SetActive(true);
    }

    private void OnConfirm() { CloseConfirmationPanel(); ExecutePurchase(); }
    private void OnCancel() { CloseConfirmationPanel(); }

    private void ExecutePurchase()
    {
        if (!ValidateSetup() || !CanAfford()) return;

        audioEventDispatcher?.PlayAudio(purchaseSound);
        playerDatas.generalMonney -= cost;
        playerDatas.NotifyMonneyChanged(); // déclenche RefreshButtonState sur tous les abonnés
        playerDatas.SaveDatas();
        coinsUpdater?.AffichageCoin();
        onPurchaseSuccess?.Invoke();
    }

    private void CloseConfirmationPanel()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        confirmButton?.onClick.RemoveListener(OnConfirm);
        cancelButton?.onClick.RemoveListener(OnCancel);
    }

    private void RefreshButtonState()
    {
        if (_button == null || playerDatas == null) return;
        if (priceLabel != null)
            priceLabel.color = CanAfford() ? affordableColor : unaffordableColor;
    }

    private void UpdatePriceLabel()
    {
        if (priceLabel != null) priceLabel.text = cost.ToString();
    }

    private bool CanAfford() => playerDatas != null && playerDatas.generalMonney >= cost;
    private bool ValidateSetup()
    {
        if (playerDatas != null) return true;
        Debug.LogError("[CoinPurchaseButton] SO_PlayerDatas non assigné !", this);
        return false;
    }

    #endregion

    public void SetCost(int newCost)
    {
        cost = Mathf.Max(0, newCost);
        UpdatePriceLabel();
        RefreshButtonState();
    }
}