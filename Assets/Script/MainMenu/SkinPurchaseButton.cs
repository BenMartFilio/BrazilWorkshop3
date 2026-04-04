using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bouton d'achat de skin cosmétique dans la boutique.
/// Supporte deux devises (pièces ou gemmes) selon le champ deviseAchat.
/// Ouvre le panel de confirmation avant de valider la transaction,
/// puis marque le cosmétique comme acheté dans l'inventaire et équipe le skin.
/// </summary>
public class SkinPurchaseButton : MonoBehaviour
{
    /// <summary>Diffusé à toutes les instances quand un skin est équipé.</summary>
    public static event System.Action OnAnySkinEquipped;

    public enum Devise { Pieces, Gemmes }

    private const string LOG_TAG = "[SkinPurchaseButton]";

    [Header("Données")]
    [SerializeField] private SO_PlayerDatas playerDatas;
    [SerializeField] private SO_Cosmetiques catalogue;

    [Tooltip("Index du skin dans la liste SkinPlayer du catalogue SO_Cosmetiques.")]
    [SerializeField] private int skinIndex = 0;

    [SerializeField] private Devise deviseAchat = Devise.Pieces;
    [SerializeField] private int cost = 500;

    [Header("UI — Prix")]
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private Color affordableColor = Color.black;
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private Image deviseIcone;
    [SerializeField] private Sprite iconePieces;
    [SerializeField] private Sprite iconeGemmes;


    [Header("Panel de confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmationNameLabel;
    [SerializeField] private TMP_Text confirmationDescriptionLabel;

    [Header("Panel de succès")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private Image successItemIcon;

    [Header("Audio")]
    [SerializeField] private AudioEventDispatcher audioEventDispatcher;
    [SerializeField] private AudioType purchaseSound;
    [SerializeField] private AudioType failSound;

    [Header("Affichage monnaie")]
    [SerializeField] private CoinsUpdater coinsUpdater;

    [Header("Événements")]
    [SerializeField] private UnityEvent onPurchaseSuccess;
    [SerializeField] private UnityEvent onPurchaseFailed;

    [Header("État acheté")]
    [SerializeField] private Sprite fondAchatSkin;       // non acheté
    [SerializeField] private Sprite fondAcheteSkin;      // acheté, non équipé
    [SerializeField] private Sprite fondEquipeSkin;      // acheté ET équipé
    [SerializeField] private Image fondImage;
    [SerializeField] private TMP_Text labelEtatEquipe;
    [SerializeField] private string texteEquipe = "Équipé";
    [SerializeField] private string texteNonEquipe = "Non équipé";

    [Header("Panel d'équipement")]
    [SerializeField] private SkinEquipFocus equipFocus;
    [SerializeField] private Image iconeSource;  // enfant Icone du bouton

    [SerializeField] private GameObject prixContainer; // ← le GO "Price" parent de priceLabel et deviseIcone



    private Button _button;
    private int _cachedMonney = -1;

    #region Unity Lifecycle

    /// <summary>À appeler par SkinEquipFocus après un équipement.</summary>
    public static void NotifierSkinEquipe()
    {
        OnAnySkinEquipped?.Invoke();
    }


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
        OnAnySkinEquipped += RefreshButtonState;
        RefreshButtonState();
    }

    private void OnDisable()
    {
        OnAnySkinEquipped -= RefreshButtonState;
    }


    private void Update()
    {
        if (playerDatas == null) return;

        int currentMonney = deviseAchat == Devise.Pieces
            ? playerDatas.generalMonney
            : playerDatas.premiumMonney;

        if (currentMonney != _cachedMonney)
            RefreshButtonState();
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(OnButtonClicked);
        UnsubscribeConfirmPanel();
    }

    #endregion

    #region Logic

    private void OnButtonClicked()
    {
        if (!ValidateSetup()) return;

        if (EstAchete())
        {
            DefinitionCosmetique def = ObtenirDefinition();
            if (def != null && equipFocus != null)
                equipFocus.Ouvrir(iconeSource, skinIndex, def, RefreshButtonState);
            return;
        }

        if (!CanAfford())
        {
            audioEventDispatcher?.PlayAudio(failSound);
            onPurchaseFailed?.Invoke();
            return;
        }

        OpenConfirmationPanel();
    }


    private bool EstAchete()
    {
        if (playerDatas == null || catalogue == null) return false;

        DefinitionCosmetique def = ObtenirDefinition();
        if (def == null) return false;

        CosmetiqueEntry entry = playerDatas.ObtenirCosmetique(def.identifiant);
        return entry != null && entry.estAchete;
    }

    private void OpenConfirmationPanel()
    {
        if (confirmationPanel == null)
        {
            ExecutePurchase();
            return;
        }

        DefinitionCosmetique def = ObtenirDefinition();
        if (def != null)
        {
            if (confirmationNameLabel != null)
                confirmationNameLabel.text = def.nomAffichage;

            if (confirmationDescriptionLabel != null)
                confirmationDescriptionLabel.text = def.description;
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

    private void OnCancel() => CloseConfirmationPanel();

    private void ExecutePurchase()
    {
        if (!ValidateSetup() || !CanAfford()) return;

        if (deviseAchat == Devise.Pieces)
            playerDatas.generalMonney -= cost;
        else
            playerDatas.premiumMonney -= cost;

        DefinitionCosmetique def = ObtenirDefinition();
        if (def != null)
            playerDatas.AcheterCosmetique(def.identifiant); // sauvegarde incluse
        else
            playerDatas.SaveDatas();

        playerDatas.skinEquiped = skinIndex;
        playerDatas.SaveDatas();

        audioEventDispatcher?.PlayAudio(purchaseSound);
        coinsUpdater?.AffichageCoin();
        coinsUpdater?.AffichagePremium();

        OpenSuccessPanel(def?.sprite);
        RefreshButtonState();
        onPurchaseSuccess?.Invoke();
    }

    private void CloseConfirmationPanel()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        UnsubscribeConfirmPanel();
    }

    private void UnsubscribeConfirmPanel()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
    }

    private void OpenSuccessPanel(Sprite sprite)
    {
        if (successPanel == null) return;
        if (successItemIcon != null && sprite != null)
            successItemIcon.sprite = sprite;
        successPanel.SetActive(true);
    }

    private void RefreshButtonState()
    {
        if (_button == null || playerDatas == null) return;

        bool achete = EstAchete();
        bool equipe = playerDatas.skinEquiped == skinIndex;

        // Fond du bouton — trois états
        if (fondImage != null)
        {
            if (!achete) fondImage.sprite = fondAchatSkin;
            else if (equipe) fondImage.sprite = fondEquipeSkin;
            else fondImage.sprite = fondAcheteSkin;
        }

        if (achete)
        {
            if (prixContainer != null) prixContainer.SetActive(false);
            if (labelEtatEquipe != null)
            {
                labelEtatEquipe.gameObject.SetActive(true);
                labelEtatEquipe.text = equipe ? texteEquipe : texteNonEquipe;
            }
        }
        else
        {
            if (prixContainer != null) prixContainer.SetActive(true);
            if (labelEtatEquipe != null) labelEtatEquipe.gameObject.SetActive(false);

            if (priceLabel != null)
                priceLabel.color = CanAfford() ? affordableColor : unaffordableColor;
        }

        _cachedMonney = deviseAchat == Devise.Pieces
            ? playerDatas.generalMonney
            : playerDatas.premiumMonney;
    }


    private void UpdatePriceLabel()
    {
        if (priceLabel != null)
            priceLabel.text = cost.ToString();

        if (deviseIcone != null)
            deviseIcone.sprite = deviseAchat == Devise.Pieces ? iconePieces : iconeGemmes;
    }

    private bool CanAfford()
    {
        if (playerDatas == null) return false;
        return deviseAchat == Devise.Pieces
            ? playerDatas.generalMonney >= cost
            : playerDatas.premiumMonney >= cost;
    }

    private DefinitionCosmetique ObtenirDefinition()
    {
        if (catalogue == null) return null;

        var skins = catalogue.ObtenirParCategorie(CategorieCosmetique.SkinPlayer);

        if (skinIndex < 0 || skinIndex >= skins.Count)
        {
            Debug.LogWarning($"{LOG_TAG} skinIndex {skinIndex} hors limites ({skins.Count} skins).", this);
            return null;
        }

        return skins[skinIndex];
    }

    private bool ValidateSetup()
    {
        if (playerDatas == null)
        {
            Debug.LogError($"{LOG_TAG} SO_PlayerDatas non assigné.", this);
            return false;
        }
        if (catalogue == null)
        {
            Debug.LogError($"{LOG_TAG} SO_Cosmetiques non assigné.", this);
            return false;
        }
        return true;
    }

    #endregion

    #region Public API

    /// <summary>Modifie le coût et rafraîchit le label à la volée.</summary>
    public void SetCost(int newCost)
    {
        cost = Mathf.Max(0, newCost);
        UpdatePriceLabel();
        RefreshButtonState();
    }

    #endregion
}
