using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Bouton de revive lié à une publicité récompensée LevelPlay, pour la scène Barrage.
    /// Identique à ReviveButtonAd mais appelle LightEndManager.Revive() qui déclenche
    /// la victoire du barrage (affichage de la prochaine demande + retour MapRoad).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ReviveButtonAdBarrage : MonoBehaviour
    {
        private const int MaxRevives = 3;

        [SerializeField] private string _rewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";
        [SerializeField] private LightEndManager _lightEndManager;
        [SerializeField] private DonnéesSession _donnéesSession;
        [SerializeField] private BackToMenu _backToMenu;

        private Button _button;
        private LevelPlayRewardedAd _rewardedAd;
        private int _reviveCount;
        private bool _rewardGranted;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _reviveCount = _donnéesSession != null ? _donnéesSession.adWatched : 0;
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

        // ── Chargement ──────────────────────────────────────────────────────────

        private void LoadAd()
        {
            if (_rewardedAd == null)
            {
                Debug.LogWarning("[ReviveButtonAdBarrage] RewardedAd non initialisé.");
                return;
            }

            SetButtonInteractable(false);
            _rewardedAd.LoadAd();
        }

        // ── Interactions bouton ─────────────────────────────────────────────────

        private void OnButtonClicked()
        {
            if (_rewardedAd.IsAdReady())
            {
                _rewardGranted = false;
                _backToMenu._watchinAds = true;
                _rewardedAd.ShowAd();
            }
            else
            {
                SetButtonInteractable(false);
                LoadAd();
            }
        }

        // ── Callbacks publicité ─────────────────────────────────────────────────

        private void OnAdLoaded(LevelPlayAdInfo adInfo) => SetButtonInteractable(true);

        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"[ReviveButtonAdBarrage] Chargement échoué : {error.ErrorMessage}");
            SetButtonInteractable(false);
        }

        private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.LogWarning($"[ReviveButtonAdBarrage] Affichage échoué : {error.ErrorMessage}");
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
                if (!_rewardGranted)
                {
                    _backToMenu.DisplayEndScore();
                    return;
                }

                _reviveCount++;
                if (_donnéesSession != null) _donnéesSession.adWatched = _reviveCount;
                _lightEndManager.Revive();
                ApplyReviveCountState();

                if (_reviveCount < MaxRevives) LoadAd();
            });
        }

        // ── Helpers état ────────────────────────────────────────────────────────

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

        /// <summary>Réinitialise le compteur (à appeler en début de partie).</summary>
        public void ResetReviveCount()
        {
            _reviveCount = 0;
            if (_donnéesSession != null) _donnéesSession.adWatched = 0;
            gameObject.SetActive(true);
            SetButtonInteractable(false);
            LoadAd();
        }
    }
}
