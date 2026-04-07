using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

/// <summary>
/// Service centralisé pour l'envoi et la récupération des scores UGS Leaderboards.
/// Assume que UnityServices et AuthenticationService sont déjà initialisés et connectés.
/// </summary>
public class LeaderboardService : MonoBehaviour
{
    public static LeaderboardService Instance { get; private set; }

    public const string LeaderboardId = "Drakensland_Leaderboard";
    public const int MaxEntriesPerTier = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Soumet le score du joueur au leaderboard mondial.</summary>
    public async Task SubmitScoreAsync(int score)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[LeaderboardService] Joueur non connecté — score non soumis.");
            return;
        }

        try
        {
            LeaderboardEntry entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                LeaderboardId,
                score
            );
            Debug.Log($"[LeaderboardService] Score soumis : {entry.Score} (rang {entry.Rank + 1}, tier {entry.Tier})");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardService] Échec de la soumission du score : {e.Message}");
        }
    }

    /// <summary>Récupère les scores d'un tier spécifique (jusqu'à MaxEntriesPerTier).</summary>
    public async Task<List<LeaderboardEntry>> GetScoresByTierAsync(string tierId, int offset = 0)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[LeaderboardService] Joueur non connecté — classement non disponible.");
            return null;
        }

        try
        {
            LeaderboardTierScoresPage page = await LeaderboardsService.Instance.GetScoresByTierAsync(
                LeaderboardId,
                tierId,
                new GetScoresByTierOptions { Offset = offset, Limit = MaxEntriesPerTier }
            );
            return page.Results;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardService] Échec GetScoresByTier ({tierId}) : {e.Message}");
            return null;
        }
    }

    /// <summary>Récupère l'entrée du joueur courant (contient son Tier).</summary>
    public async Task<LeaderboardEntry> GetPlayerScoreAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[LeaderboardService] Joueur non connecté.");
            return null;
        }

        try
        {
            return await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardService] Joueur non classé ou erreur : {e.Message}");
            return null;
        }
    }
}
