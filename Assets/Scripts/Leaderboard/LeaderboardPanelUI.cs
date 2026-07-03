using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôle le panneau de classement mondial avec navigation par tier.
/// À l'ouverture, se positionne automatiquement sur le tier et la ligne du joueur.
/// Le joueur peut naviguer entre les tiers via des boutons onglets.
/// </summary>
public class LeaderboardPanelUI : MonoBehaviour
{
    [Header("Panneau racine")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private RectTransform _panelTransform;

    [Header("Tiers — ordre : Bronze → Argent → Or → Diamant → Rubis")]
    [SerializeField] private TierDefinition[] _tiers;

    [Header("Onglets de tier")]
    [SerializeField] private Transform _tabContainer;
    [SerializeField] private TierTabButtonUI _tabPrefab;

    [Header("Liste de scores")]
    [SerializeField] private Transform _rowContainer;
    [SerializeField] private LeaderboardRowUI _rowPrefab;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("États")]
    [SerializeField] private GameObject _loadingIndicator;
    [SerializeField] private GameObject _errorIndicator;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private TMP_Text _tierTitleText;

    [Header("Ligne «non classé»")]
    [SerializeField] private GameObject _unrankedRow;
    [SerializeField] private TMP_Text _unrankedScoreText;

    [Header("Fermeture")]
    [SerializeField] private Button _closeButton;

    [Header("Animation")]
    [SerializeField] private float _openAnimDuration = 0.15f;

    // ── État interne ──────────────────────────────────────────────────────────

    private readonly List<LeaderboardRowUI> _rowPool = new();
    private readonly List<TierTabButtonUI> _tabPool = new();

    private int _activeTierIndex = 0;
    private string _currentPlayerId = string.Empty;
    private string _playerTierId = string.Empty;
    private int _playerRankInTier = -1;

    private Vector3 _originalScale;
    private Coroutine _scaleCoroutine;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _originalScale = _panelTransform != null ? _panelTransform.localScale : Vector3.one;

        if (_closeButton != null)
            _closeButton.onClick.AddListener(ClosePanel);

        BuildTabs();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Ouvre le panneau, détecte le tier du joueur et charge son classement.</summary>
    public void OpenPanel()
    {
        _panelRoot.SetActive(true);
        SetLoadingState(true);
        SetErrorState(false);
        HideUnrankedRow();
        ClearRows();

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(AnimateOpen());

        _currentPlayerId = AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : string.Empty;

        StartCoroutine(InitAndLoad());
    }

    /// <summary>Ferme le panneau.</summary>
    public void ClosePanel()
    {
        _panelRoot.SetActive(false);
        ClearRows();
    }

    /// <summary>Sélectionne un tier par index et recharge la liste.</summary>
    public void SelectTier(int index)
    {
        if (index < 0 || index >= _tiers.Length) return;

        _activeTierIndex = index;
        UpdateTabVisuals();
        SetLoadingState(true);
        SetErrorState(false);
        HideUnrankedRow();
        ClearRows();
        StartCoroutine(LoadTier(_tiers[index]));
    }

    // ── Init : récupération du score joueur pour connaître son tier ───────────

    private IEnumerator InitAndLoad()
    {
        if (LeaderboardService.Instance == null)
        {
            ShowError("Service non disponible.");
            yield break;
        }

        var playerTask = LeaderboardService.Instance.GetPlayerScoreAsync();
        yield return new WaitUntil(() => playerTask.IsCompleted);

        _playerTierId = string.Empty;
        _playerRankInTier = -1;

        if (!playerTask.IsFaulted && playerTask.Result != null)
        {
            _playerTierId = playerTask.Result.Tier ?? string.Empty;
            _playerRankInTier = playerTask.Result.Rank;
        }

        // On cherche l'index du tier du joueur dans le tableau configuré
        int defaultIndex = FindTierIndex(_playerTierId);
        _activeTierIndex = defaultIndex >= 0 ? defaultIndex : 0;

        UpdateTabVisuals();
        StartCoroutine(LoadTier(_tiers[_activeTierIndex]));
    }

    // ── Chargement d'un tier ──────────────────────────────────────────────────

    private IEnumerator LoadTier(TierDefinition tier)
    {
        if (_tierTitleText != null)
            _tierTitleText.text = tier.ObtenirNomLocalise();

        var task = LeaderboardService.Instance.GetScoresByTierAsync(tier.tierId);
        yield return new WaitUntil(() => task.IsCompleted);

        SetLoadingState(false);

        if (task.IsFaulted || task.Result == null)
        {
            ShowError($"Impossible de charger le classement {tier.ObtenirNomLocalise()}.\nVérifie ta connexion.");
            yield break;
        }

        PopulateRows(task.Result, tier.color);
    }

    // ── Affichage des lignes ──────────────────────────────────────────────────

    private void PopulateRows(List<LeaderboardEntry> entries, Color tierColor)
    {
        ClearRows();

        bool isCurrentTier = _tiers[_activeTierIndex].tierId == _playerTierId;
        int playerRowIndex = -1;

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            bool isMe = entry.PlayerId == _currentPlayerId;

            LeaderboardRowUI row = GetOrCreateRow(i);
            row.Populate(entry.Rank + 1, entry.PlayerName, entry.Score, isMe, tierColor);
            row.gameObject.SetActive(true);

            if (isMe)
                playerRowIndex = i;
        }

        // Joueur dans ce tier mais non présent dans le top 100 affiché → ligne «non classé» en bas
        bool playerInListedEntries = playerRowIndex >= 0;
        bool playerBelongsToThisTier = isCurrentTier && !string.IsNullOrEmpty(_currentPlayerId);

        if (playerBelongsToThisTier && !playerInListedEntries)
        {
            ShowUnrankedRow(tierColor);
            // scroll tout en bas
            StartCoroutine(ScrollToBottom());
        }
        else if (playerInListedEntries)
        {
            StartCoroutine(ScrollToRow(playerRowIndex));
        }
        else
        {
            // Autre tier : scroll au top
            StartCoroutine(ScrollToTop());
        }
    }

    // ── Pool de lignes ────────────────────────────────────────────────────────

    private LeaderboardRowUI GetOrCreateRow(int index)
    {
        if (index < _rowPool.Count)
            return _rowPool[index];

        LeaderboardRowUI row = Instantiate(_rowPrefab, _rowContainer);
        _rowPool.Add(row);
        return row;
    }

    private void ClearRows()
    {
        foreach (LeaderboardRowUI row in _rowPool)
        {
            if (row != null)
                row.gameObject.SetActive(false);
        }
    }

    // ── Ligne «non classé» ────────────────────────────────────────────────────

    private void ShowUnrankedRow(Color tierColor)
    {
        if (_unrankedRow == null) return;
        _unrankedRow.SetActive(true);

        if (_unrankedScoreText != null)
            _unrankedScoreText.color = tierColor;
    }

    private void HideUnrankedRow()
    {
        if (_unrankedRow != null)
            _unrankedRow.SetActive(false);
    }

    // ── Onglets ───────────────────────────────────────────────────────────────

    private void BuildTabs()
    {
        if (_tabPrefab == null || _tabContainer == null || _tiers == null) return;

        for (int i = 0; i < _tiers.Length; i++)
        {
            TierTabButtonUI tab = Instantiate(_tabPrefab, _tabContainer);
            tab.Setup(_tiers[i]);

            int capturedIndex = i;
            Button btn = tab.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => SelectTier(capturedIndex));

            _tabPool.Add(tab);
        }
    }

    private void UpdateTabVisuals()
    {
        for (int i = 0; i < _tabPool.Count; i++)
            _tabPool[i].SetActive(i == _activeTierIndex);
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private int FindTierIndex(string tierId)
    {
        if (string.IsNullOrEmpty(tierId)) return -1;
        for (int i = 0; i < _tiers.Length; i++)
            if (_tiers[i].tierId == tierId) return i;
        return -1;
    }

    // ── États d'UI ────────────────────────────────────────────────────────────

    private void SetLoadingState(bool isLoading)
    {
        if (_loadingIndicator != null)
            _loadingIndicator.SetActive(isLoading);
    }

    private void SetErrorState(bool hasError)
    {
        if (_errorIndicator != null)
            _errorIndicator.SetActive(hasError);
    }

    private void ShowError(string message)
    {
        SetLoadingState(false);
        SetErrorState(true);
        if (_errorText != null)
            _errorText.text = message;
    }

    // ── Scroll ────────────────────────────────────────────────────────────────

    private IEnumerator ScrollToRow(int rowIndex)
    {
        yield return null; // attendre un frame que le layout se mette à jour
        yield return null;

        if (_scrollRect == null || _rowPool.Count == 0) yield break;

        float normalizedPos = 1f - (float)rowIndex / Mathf.Max(1, _rowPool.Count - 1);
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
    }

    private IEnumerator ScrollToTop()
    {
        yield return null;
        yield return null;
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        yield return null;
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
    }

    // ── Animation d'ouverture ─────────────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
        if (_panelTransform == null) yield break;

        Vector3 fromScale = _originalScale * 0.85f;
        float elapsed = 0f;

        while (elapsed < _openAnimDuration)
        {
            elapsed += Time.deltaTime;
            _panelTransform.localScale = Vector3.Lerp(fromScale, _originalScale, EaseOutCubic(elapsed / _openAnimDuration));
            yield return null;
        }

        _panelTransform.localScale = _originalScale;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}
