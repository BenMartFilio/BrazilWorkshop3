
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère le bouton de revive lié à une publicité récompensée LevelPlay.
/// - Grise le bouton si la pub n'est pas disponible (mode avion, pas de connexion, etc.)
/// - Cache le bouton après MAX_REVIVES revives.
/// </summary>
[RequireComponent(typeof(Button))]
public class ReviveButtonAd : MonoBehaviour
{
    private const int MaxRevives = 3;
    private const string ReviveCountKey = "ReviveCount";

    [SerializeField] private string _rewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";
    [SerializeField] private EndManager _endManager;

    private Button _button;
    private LevelPlayRewardedAd _rewardedAd;
    private int _reviveCount;
    private bool _rewardGranted;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _reviveCount = PlayerPrefs.GetInt(ReviveCountKey, 0);
    }

    private void Start()
    {
        ApplyReviveCountState();

        _rewardedAd = new LevelPlayRewardedAd(_rewardedAdUnitId);

        _rewardedAd.OnAdLoaded += OnAdLoaded;
        _rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        _rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        _rewardedAd.OnAdRewarded += OnAdRewarded;
        _rewardedAd.OnAdClosed += OnAdClosed;

        _button.onClick.AddListener(OnButtonClicked);

        LoadAd();
    }

    private void OnDestroy()
    {
        if (_rewardedAd == null) return;

        _rewardedAd.OnAdLoaded -= OnAdLoaded;
        _rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
        _rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        _rewardedAd.OnAdRewarded -= OnAdRewarded;
        _rewardedAd.OnAdClosed -= OnAdClosed;
    }

    // --- Chargement ---

    private void LoadAd()
    {
        if (_rewardedAd == null)
        {
            Debug.LogWarning("RewardedAd not initialized yet");
            return;
        }

        SetButtonInteractable(false);
        _rewardedAd.LoadAd();
    }

    // --- Interactions bouton ---

    private void OnButtonClicked()
    {
        if (_rewardedAd.IsAdReady())
        {
            _rewardGranted = false;
            _rewardedAd.ShowAd();
        }
        else
        {
            SetButtonInteractable(false);
            LoadAd();
        }
    }

    // --- Callbacks publicité ---

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        SetButtonInteractable(true);
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[ReviveButtonAd] Chargement échoué : {error.ErrorMessage}");
        SetButtonInteractable(false);
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"[ReviveButtonAd] Affichage échoué : {error.ErrorMessage}");
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

            _reviveCount++;
            PlayerPrefs.SetInt(ReviveCountKey, _reviveCount);
            PlayerPrefs.Save();
            _endManager.Revive();
            ApplyReviveCountState();

            if (_reviveCount < MaxRevives) LoadAd();
        });
    }

    // --- Helpers état ---

    /// <summary>Cache le bouton si le quota est atteint, sinon grise en attendant le chargement.</summary>
    private void ApplyReviveCountState()
    {
        if (_reviveCount >= MaxRevives)
        {
            gameObject.SetActive(false);
            return;
        }

        SetButtonInteractable(false);
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }

    /// <summary>Réinitialise le compteur de revives (à appeler en début de partie).</summary>
    public void ResetReviveCount()
    {
        _reviveCount = 0;
        PlayerPrefs.SetInt(ReviveCountKey, 0);
        PlayerPrefs.Save();
        gameObject.SetActive(true);
        SetButtonInteractable(false);
        LoadAd();
    }
}