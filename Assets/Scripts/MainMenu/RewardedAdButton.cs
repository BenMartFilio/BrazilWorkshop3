using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Bouton de publicité récompensée générique pour le shop (ou ailleurs).
/// - Assignez un AdRewardConfig pour définir la récompense et la limite quotidienne.
/// - Le bouton se grise automatiquement si la pub n'est pas disponible ou si
///   la limite quotidienne est atteinte.
/// </summary>
[RequireComponent(typeof(Button))]
public class RewardedAdButton : MonoBehaviour
{
    private const string DailyCountKeyPrefix = "RewardedAdButton_DailyCount_";
    private const string DailyDateKeyPrefix  = "RewardedAdButton_DailyDate_";

    [Header("Configuration")]
    [SerializeField] private string _rewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";
    [SerializeField] private AdRewardConfig _rewardConfig;

    [Header("Données joueur")]
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private SO_Cosmetiques _cosmetiques;

    [Header("UI (optionnel)")]
    [SerializeField] private CoinsUpdater _coinsUpdater;

    [Header("Animation de récompense")]
    [Tooltip("GlobalMenu du panel EarnObjectMenu. Laisser vide pour ignorer l'animation.")]
    [SerializeField] private GlobalMenu _earnMenu;
    [Tooltip("Image enfant ImageOfThingObtnain du panel EarnObjectMenu.")]
    [SerializeField] private UnityEngine.UI.Image _earnMenuIcon;

    [Header("Événements")]
    [SerializeField] private UnityEvent _onRewardGranted;
    [SerializeField] private UnityEvent _onAdUnavailable;
    [SerializeField] private UnityEvent _onDailyLimitReached;

    private Button _button;
    private LevelPlayRewardedAd _rewardedAd;
    private bool _rewardGranted;

    // Clé unique par instance (basée sur le nom du GameObject dans la hiérarchie)
    private string DailyCountKey => DailyCountKeyPrefix + gameObject.name;
    private string DailyDateKey  => DailyDateKeyPrefix  + gameObject.name;

    #region Unity Lifecycle

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void Start()
    {
        if (!ValidateSetup()) return;

        _rewardedAd = new LevelPlayRewardedAd(_rewardedAdUnitId);

        _rewardedAd.OnAdLoaded        += OnAdLoaded;
        _rewardedAd.OnAdLoadFailed    += OnAdLoadFailed;
        _rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        _rewardedAd.OnAdRewarded      += OnAdRewarded;
        _rewardedAd.OnAdClosed        += OnAdClosed;

        _button.onClick.AddListener(OnButtonClicked);

        RefreshButtonState();
        LoadAd();
    }

    private void OnDestroy()
    {
        if (_rewardedAd == null) return;

        _rewardedAd.OnAdLoaded        -= OnAdLoaded;
        _rewardedAd.OnAdLoadFailed    -= OnAdLoadFailed;
        _rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        _rewardedAd.OnAdRewarded      -= OnAdRewarded;
        _rewardedAd.OnAdClosed        -= OnAdClosed;
    }

    #endregion

    #region Ad Loading

    private void LoadAd()
    {
        if (_rewardedAd == null) return;

        SetButtonInteractable(false);
        _rewardedAd.LoadAd();
    }

    #endregion

    #region Button & Ad Callbacks

    private void OnButtonClicked()
    {
        if (IsDailyLimitReached())
        {
            _onDailyLimitReached?.Invoke();
            return;
        }

        if (_rewardedAd != null && _rewardedAd.IsAdReady())
        {
            _rewardGranted = false;
            _rewardedAd.ShowAd();
        }
        else
        {
            SetButtonInteractable(false);
            _onAdUnavailable?.Invoke();
            LoadAd();
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        RefreshButtonState();
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[RewardedAdButton] Chargement échoué : {error.ErrorMessage}");
        SetButtonInteractable(false);
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"[RewardedAdButton] Affichage échoué : {error.ErrorMessage}");
        SetButtonInteractable(false);
        LoadAd();
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        _rewardGranted = true;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            if (!_rewardGranted) return;

            IncrementDailyCount();
            GrantReward();
            RefreshButtonState();

            if (!IsDailyLimitReached()) LoadAd();
        });
    }

    #endregion

    #region Reward

    private void GrantReward()
    {
        if (_rewardConfig == null || _playerDatas == null) return;

        switch (_rewardConfig.rewardType)
        {
            case AdRewardConfig.RewardType.Coins:
                _playerDatas.generalMonney += _rewardConfig.amount;
                _playerDatas.NotifyMonneyChanged();
                _playerDatas.SaveDatas();
                _coinsUpdater?.AffichageCoin();
                break;

            case AdRewardConfig.RewardType.Gems:
                _playerDatas.premiumMonney += _rewardConfig.amount;
                _playerDatas.NotifyMonneyChanged();
                _playerDatas.SaveDatas();
                _coinsUpdater?.AffichagePremium();
                break;

            case AdRewardConfig.RewardType.Cosmetic:
                if (!string.IsNullOrEmpty(_rewardConfig.cosmeticIdentifier))
                    _playerDatas.AcheterCosmetique(_rewardConfig.cosmeticIdentifier);
                break;
        }

        _onRewardGranted?.Invoke();
        OpenEarnMenu();
    }

    private void OpenEarnMenu()
    {
        if (_earnMenu == null) return;

        if (_earnMenuIcon != null && _rewardConfig.rewardSprite != null)
            _earnMenuIcon.sprite = _rewardConfig.rewardSprite;

        _earnMenu.OpenMenu();
    }

    #endregion

    #region Daily Limit

    /// <summary>Retourne le nombre d'utilisations effectuées aujourd'hui pour ce bouton.</summary>
    private int GetTodayCount()
    {
        string savedDate = PlayerPrefs.GetString(DailyDateKey, string.Empty);
        string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (savedDate != today)
        {
            // Nouveau jour — on remet le compteur à zéro
            PlayerPrefs.SetString(DailyDateKey, today);
            PlayerPrefs.SetInt(DailyCountKey, 0);
            PlayerPrefs.Save();
            return 0;
        }

        return PlayerPrefs.GetInt(DailyCountKey, 0);
    }

    private void IncrementDailyCount()
    {
        int count = GetTodayCount() + 1;
        PlayerPrefs.SetInt(DailyCountKey, count);
        PlayerPrefs.Save();
    }

    /// <summary>Retourne true si la limite quotidienne est configurée et atteinte.</summary>
    private bool IsDailyLimitReached()
    {
        if (_rewardConfig == null) return false;
        if (_rewardConfig.dailyLimit <= 0) return false;
        return GetTodayCount() >= _rewardConfig.dailyLimit;
    }

    #endregion

    #region UI State

    /// <summary>Active ou désactive le bouton en tenant compte de la limite et de la disponibilité de la pub.</summary>
    private void RefreshButtonState()
    {
        if (_button == null) return;

        if (IsDailyLimitReached())
        {
            SetButtonInteractable(false);
            return;
        }

        // Le bouton est actif uniquement si la pub est chargée
        bool adReady = _rewardedAd != null && _rewardedAd.IsAdReady();
        SetButtonInteractable(adReady);
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }

    #endregion

    #region Validation

    private bool ValidateSetup()
    {
        if (_rewardConfig == null)
        {
            Debug.LogError("[RewardedAdButton] AdRewardConfig non assigné !", this);
            return false;
        }
        if (_playerDatas == null)
        {
            Debug.LogError("[RewardedAdButton] SO_PlayerDatas non assigné !", this);
            return false;
        }
        return true;
    }

    #endregion
}
