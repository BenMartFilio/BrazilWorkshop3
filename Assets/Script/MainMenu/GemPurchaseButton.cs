using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bouton d'achat d'or (generalMonney) avec des gemmes (premiumMonney).
/// Affiche un panel de confirmation avant de valider la transaction.
/// </summary>
public class GemPurchaseButton : MonoBehaviour
{
    [Header("Données")]
    [SerializeField] private SO_PlayerDatas playerDatas;
    [SerializeField] private int gemCost = 10;
    [SerializeField] private int goldReward = 500;

    [Header("UI — Prix")]
    [SerializeField] private TMP_Text gemCostLabel;
    [SerializeField] private TMP_Text goldRewardLabel;
    [SerializeField] private Color affordableColor = Color.black;
    [SerializeField] private Color unaffordableColor = Color.red;

    [Header("Panel de confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Affichage")]
    [SerializeField] private CoinsUpdater coinsUpdater;

    [Header("Événements")]
    [SerializeField] private UnityEvent onPurchaseSuccess;
    [SerializeField] private UnityEvent onPurchaseFailed;

    private Button _button;
    private int _cachedPremiumMonney = -1;

    [Header("Panel de succès")]
    [Tooltip("Panel à activer une fois l'objet attribué.")]
    [SerializeField] private GameObject successPanel;
    [Tooltip("Image enfant du panel qui recevra le sprite de l'objet.")]
    [SerializeField] private Image successItemIcon;
    [SerializeField] private Sprite successSprite;

    [SerializeField] private AudioEventDispatcher audioEventDispatcher;
    [SerializeField] private AudioType purchaseSound;

    #region Unity Lifecycle

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void OnEnable() => RefreshButtonState();

    private void Start()
    {
        UpdateLabels();
        RefreshButtonState();
    }

    private void Update()
    {
        if (playerDatas != null && playerDatas.premiumMonney != _cachedPremiumMonney)
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
            Debug.LogWarning("[GemPurchaseButton] Gemmes insuffisantes.");
            onPurchaseFailed?.Invoke();
            return;
        }

        OpenConfirmationPanel();
    }

    private void OpenConfirmationPanel()
    {
        if (confirmationPanel == null)
        {
            ExecutePurchase();
            return;
        }

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
        if(audioEventDispatcher != null) audioEventDispatcher.PlayAudio(purchaseSound);
        playerDatas.premiumMonney -= gemCost;
        playerDatas.generalMonney += goldReward;
        playerDatas.SaveDatas();

        coinsUpdater?.AffichageCoin();
        coinsUpdater?.AffichagePremium();

        onPurchaseSuccess?.Invoke();
        RefreshButtonState();
        OpenSuccessPanel(successSprite);
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

        if (gemCostLabel != null)
            gemCostLabel.color = affordable ? affordableColor : unaffordableColor;

        _cachedPremiumMonney = playerDatas.premiumMonney;
    }

    private void UpdateLabels()
    {
        if (gemCostLabel != null)
            gemCostLabel.text = gemCost.ToString();

        if (goldRewardLabel != null)
            goldRewardLabel.text = goldReward.ToString();
    }

    private bool CanAfford() => playerDatas != null && playerDatas.premiumMonney >= gemCost;

    private bool ValidateSetup()
    {
        if (playerDatas != null) return true;
        Debug.LogError("[GemPurchaseButton] SO_PlayerDatas non assigné !", this);
        return false;
    }

    #endregion

    #region Public API

    /// <summary>Modifie le coût en gemmes et la récompense en or à la volée.</summary>
    public void SetValues(int newGemCost, int newGoldReward)
    {
        gemCost = Mathf.Max(0, newGemCost);
        goldReward = Mathf.Max(0, newGoldReward);
        UpdateLabels();
        RefreshButtonState();
    }

    #endregion


    private void OpenSuccessPanel(Sprite itemSprite)
    {
        if (successPanel == null) return;

        if (successItemIcon != null)
            successItemIcon.sprite = itemSprite;

        successPanel.SetActive(true);
    }
}
