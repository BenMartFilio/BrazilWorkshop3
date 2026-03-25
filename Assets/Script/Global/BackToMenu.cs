using System.Collections;
using UnityEngine;

public class BackToMenu : MonoBehaviour
{
    private bool inScore = false;
    private Coroutine _timer;
    public void DisplayEndScore()
    {
        Debug.Log("Display end");
        StopCoroutine(_timer);
        inScore = true;
        Time.timeScale = 0f;
    }

    public void StartTimer()
    {
        _timer = StartCoroutine(Timer(9f));
    }

    IEnumerator Timer(float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            if (inScore)
            {
                yield return null;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (!inScore)
        {
            DisplayEndScore();
        }
    }


    public void OnRevival()
    {
        StopCoroutine(_timer);
    }
}
