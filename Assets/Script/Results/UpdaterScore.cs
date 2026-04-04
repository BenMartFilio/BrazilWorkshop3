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

    private IEnumerator ScoreScaler(float duration)
    {
        float time = 0f;
        int score = _playerDatas.actualScoreNotSaved;

        while (time < duration)
        {
            time += Time.deltaTime;
            int current = Mathf.RoundToInt(Mathf.Lerp(0, score, time / duration));
            _scoreText.SetText("{0}", current); // évite ToString() qui alloue
            yield return null;
        }
        _scoreText.SetText("{0}", score);

        yield return new WaitForSeconds(1f);
        StartCoroutine(CoinsScaler(duration));
    }

    private IEnumerator CoinsScaler(float duration)
    {
        float time = 0f;
        int coins = _playerDatas.actualCoinsNotSaved;

        while (time < duration)
        {
            time += Time.deltaTime;
            int current = Mathf.RoundToInt(Mathf.Lerp(0, coins, time / duration));
            _coinsText.SetText("{0}", current);
            yield return null;
        }
        _coinsText.SetText("{0}", coins);
        updater.StartCoroutineCounter();
    }


}
