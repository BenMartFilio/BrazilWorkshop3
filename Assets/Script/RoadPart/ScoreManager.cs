using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    protected int score;
    private int bestscore;
    private bool isDriving;
    private Coroutine scoreCoroutine;
    private float speedScore = 39f;
    [SerializeField] private TMP_Text textScore;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private GameObject bestScoreParent;
    [SerializeField] private SO_PlayerDatas playerDatas;

    // Cache CanvasGroup pour éviter GetComponent dans la coroutine
    private CanvasGroup _bestScoreCanvasGroup;

    // Guard anti-rebuild : ne met à jour le texte que si la valeur change
    private int _lastDisplayedScore = -1;
    private int _lastDisplayedDelta = -1;

    private void Start()
    {
        if (bestScoreParent != null)
        {
            _bestScoreCanvasGroup = bestScoreParent.GetComponent<CanvasGroup>();
            if (_bestScoreCanvasGroup == null)
                _bestScoreCanvasGroup = bestScoreParent.AddComponent<CanvasGroup>();
        }
        StartScore();
    }

    IEnumerator ContiniousScore()
    {
        while (isDriving)
        {
            AddToScore(1);
            yield return new WaitForSeconds(Mathf.Clamp(1 / speedScore, 0.0001f, 1));
        }
    }

    IEnumerator ContiniousBestScore()
    {
        bestscore = playerDatas.BestScore;
        while (score < bestscore)
        {
            int delta = bestscore - score;
            // Mise à jour du texte seulement si la valeur a changé
            if (delta != _lastDisplayedDelta)
            {
                bestScoreText.SetText("{0}", delta);
                _lastDisplayedDelta = delta;
            }
            yield return new WaitForSeconds(Mathf.Clamp(1 / speedScore, 0.0001f, 1));
        }
        StartCoroutine(FadeOutBestScore(0.1f));
    }

    IEnumerator FadeOutBestScore(float duration)
    {
        if (_bestScoreCanvasGroup == null) yield break;

        float startAlpha = _bestScoreCanvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            _bestScoreCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / duration);
            yield return null;
        }

        _bestScoreCanvasGroup.alpha = 0f;
        bestScoreParent.SetActive(false);
    }

    public void StopScore()
    {
        isDriving = false;
        if (scoreCoroutine != null)
        {
            StopCoroutine(scoreCoroutine);
            scoreCoroutine = null;
        }
    }

    public void StartScore()
    {
        if (isDriving) return;
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
        // SetText avec format numérique évite l'allocation de string ToString()
        if (score != _lastDisplayedScore)
        {
            textScore.SetText("{0:000000}", score);
            _lastDisplayedScore = score;
        }
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

    public int ReturnScore()
    {
        return score;
    }
}
