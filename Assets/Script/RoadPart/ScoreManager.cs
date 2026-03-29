using System.Collections;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class ScoreManager : MonoBehaviour
{
    protected int score;
    private int bestscore;
    private bool isDriving;
    private Coroutine scoreCoroutine;
    private float speedScore=39f;
    [SerializeField] private TMP_Text textScore;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private GameObject bestScoreParent;
    [SerializeField] private SO_PlayerDatas playerDatas;
    
    IEnumerator ContiniousScore()
    {
        while (isDriving)
        {
            AddToScore(1);
            yield return new WaitForSeconds(Mathf.Clamp(1/speedScore,0.0001f,1));
        }
    }
    IEnumerator ContiniousBestScore()
    {
        bestscore = playerDatas.BestScore;
        while (score < bestscore)
        {
            bestScoreText.text = (bestscore - score).ToString();
            yield return new WaitForSeconds(Mathf.Clamp(1/speedScore,0.0001f,1));
        }
        StartCoroutine(FadeOutBestScore(0.1f));
    }

    IEnumerator FadeOutBestScore(float duration)
    {
        CanvasGroup canvasGroup = bestScoreParent.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = bestScoreParent.AddComponent<CanvasGroup>();

        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        bestScoreParent.SetActive(false);
    }


    public void StopScore()
    {
        isDriving = false;
        StopCoroutine(scoreCoroutine);
    }

    public void StartScore()
    {
        isDriving = true;
        scoreCoroutine = StartCoroutine(ContiniousScore());
        StartCoroutine(ContiniousBestScore());
    }

    public void NewSpeed(float newSpeed)
    {
        speedScore = newSpeed;
    }

    public void AddToSpeed(int toAdd)
    {
        NewSpeed(speedScore + toAdd);
    }

    private void UpdateSpeed()
    {
        AddToSpeed(10);
    }

    public void SetScore(int newScore)
    {
        score = newScore;
        textScore.text = score.ToString("D6");
    }

    public void AddToScore(int toAdd)
    {
        SetScore(score + toAdd);
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>Sauvegarde le score et la vitesse dans les données de session.</summary>
    public void SauvegarderDansSession(DonnéesSession données)
    {
        données.score        = score;
        données.vitesseScore = speedScore;
    }

    /// <summary>Restaure le score et la vitesse depuis les données de session.</summary>
    public void RestaurerDepuisSession(int scoreSauvegardé, float vitesseSauvegardée)
    {
        speedScore = vitesseSauvegardée;
        SetScore(scoreSauvegardé);
    }

    private void Start()
    {
        StartScore();
    }


    public int ReturnScore()
    {
        return score;
    }
}
