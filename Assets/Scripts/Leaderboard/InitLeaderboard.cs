using System.Collections;
using UnityEngine;

/// <summary>
/// À placer dans la scène de fin de partie (ResultScene).
/// Soumet automatiquement le score du joueur au leaderboard mondial
/// dès l'initialisation, en utilisant le BestScore stocké dans SO_PlayerDatas.
/// </summary>
public class InitLeaderboard : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas _playerDatas;

    private void Start()
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
        StartCoroutine(Waiter());
    }

    private async void Submit()
    {
        int scoreToSubmit = _playerDatas.actualScoreNotSaved;
        if (scoreToSubmit < 0) return;

        await LeaderboardService.Instance.SubmitScoreAsync(scoreToSubmit);
    }

    IEnumerator Waiter()
    {
        yield return new WaitForSeconds(1);
        Submit();
    }
}
