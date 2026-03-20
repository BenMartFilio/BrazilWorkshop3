using System.Collections;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int score;
    private bool isDriving;
    private Coroutine scoreCoroutine;
    private int speedScore=10;

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

    public void NewSpeed(int newSpeed)
    {
        speedScore = newSpeed;
    }

    public void AddToSpeed(int toAdd)
    {
        NewSpeed(speedScore + toAdd);
    }
}
