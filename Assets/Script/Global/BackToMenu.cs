// BackToMenu.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToMenu : MonoBehaviour
{
    [SerializeField] private Image _toFill;

    private bool _inScore = false;
    public bool _watchinAds = false;
    private Coroutine _timer;

    public void DisplayEndScore()
    {
        // null check — peut être appelé avant StartTimer
        if (_timer != null) StopCoroutine(_timer);
        _inScore = true;
        SceneManager.LoadScene(3);
    }

    public void StartTimer()
    {
        _inScore = false; // reset au cas où réutilisé
        _timer = StartCoroutine(Timer(9f));
    }

    public void OnRevival()
    {
        if (_timer != null) StopCoroutine(_timer);

        _watchinAds = false;
    }

    private IEnumerator Timer(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && !_inScore)
        {
            elapsed += Time.deltaTime;
            _toFill.fillAmount = Mathf.InverseLerp(duration, 0f, elapsed);
            yield return null;
        }

        if (!_inScore && !_watchinAds)
            DisplayEndScore();
    }
}