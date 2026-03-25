using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackToMenu : MonoBehaviour
{
    private bool inScore = false;
    private Coroutine _timer;
    [SerializeField] private Image _toFill;
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
            _toFill.fillAmount = Mathf.InverseLerp(time, 0f, elapsed);
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
