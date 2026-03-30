using System.Collections;
using TMPro;
using UnityEngine;

public class UpdaterScore : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _coinsText;
    [SerializeField] private CoinsUpdater updater;

    private void Start()
    {
        UpdateScore();
    }
    
    public void UpdateScore()
    {
        StartCoroutine(ScoreScaler(1.5f));
    } 

    IEnumerator ScoreScaler(float duration)
    {
        float time = 0f;
        int score = _playerDatas.actualScoreNotSaved;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, score, t));

            _scoreText.text = currentScore.ToString();

            yield return null;
        }
        _scoreText.text = score.ToString();

        yield return new WaitForSeconds(1f);
        StartCoroutine(CoinsScaler(duration));
    }
    IEnumerator CoinsScaler(float duration)
    {
        float time = 0f;
        int coins = _playerDatas.actualCoinsNotSaved;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            int currentCoins = Mathf.RoundToInt(Mathf.Lerp(0, coins, t));

            _coinsText.text = currentCoins.ToString();

            yield return null;
        }
        _coinsText.text = coins.ToString();
        updater.StartCoroutineCounter();
    }


}
