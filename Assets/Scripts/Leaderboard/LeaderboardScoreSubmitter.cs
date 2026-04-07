using UnityEngine;

/// <summary>
/// À placer dans la scène de fin de partie (ResultScene).
/// Soumet automatiquement le score du joueur au leaderboard mondial
/// dès l'initialisation, en utilisant le BestScore stocké dans SO_PlayerDatas.
/// </summary>
public class LeaderboardScoreSubmitter : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas _playerDatas;

    private async void Start()
    {
        if (_playerDatas == null)
        {
            Debug.LogError("[LeaderboardScoreSubmitter] SO_PlayerDatas non assigné !", this);
            return;
        }

        if (LeaderboardService.Instance == null)
        {
            Debug.LogWarning("[LeaderboardScoreSubmitter] LeaderboardService introuvable — score non soumis.");
            return;
        }

        int scoreToSubmit = _playerDatas.actualScoreNotSaved;
        if (scoreToSubmit <= 0) return;

        await LeaderboardService.Instance.SubmitScoreAsync(scoreToSubmit);
    }
}
