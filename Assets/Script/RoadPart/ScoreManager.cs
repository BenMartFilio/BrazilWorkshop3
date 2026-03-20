using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int score;
    private bool isDriving;
    private Coroutine scoreCoroutine;
    public float speedScore=1.5f;
    [SerializeField] private TMP_Text textScore;

    public void ModifyScore(int toAdd)
    {
        score = score + toAdd;
    }

    public void SetScore(int newScore)
    {
        score = newScore;
    }

    IEnumerator ContiniousScore()
    {
        while (isDriving)
        {
            AddToScore(1);
            yield return new WaitForSeconds(Mathf.Clamp(1/speedScore,0.0001f,1));
        }
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
    }

    public void NewSpeed(float newSpeed)
    {
        speedScore = newSpeed;
    }

    public void AddToSpeed(int toAdd)
    {
        NewSpeed(speedScore + toAdd);
    }

    public void NewScore(int newScore)
    {
        score = newScore;
        textScore.text = score.ToString();
    }

    public void AddToScore(int toAdd)
    {
        NewScore(score + toAdd);
    }

    private void Start()
    {
        StartScore();
    }
}
