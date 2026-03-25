using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndManager : MonoBehaviour
{
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GameObject _revivePanel;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private GoundMouvement[] _grounds;
    [SerializeField] private SpawnObstacleV2 _spawner;
    [SerializeField] private Image _whiteScreen;
    [SerializeField] private BackToMenu _backToMenu;
    public void OnDeath()
    {
        _timeManager.StopTime();
        _scoreManager.StopScore();
        for (int i = 0; i<_grounds.Length; i++)
        {
            _grounds[i].StopMove();
        }
        _spawner.StopSpawning();
        RevivePanelDisplay();
        _backToMenu.StartTimer();
    }


    public void Revive()
    {
        StartCoroutine(Whiter(0.3f));
        _timeManager.StartTime();
        _scoreManager.StartScore();
        for (int i = 0; i < _grounds.Length; i++)
        {
            _grounds[i].StartMove();
        }
        _spawner.StartSpawning();
        _revivePanel.SetActive(false);
    }

    private void RevivePanelDisplay()
    {
        _revivePanel.SetActive(true);
    }

    IEnumerator Whiter(float duration)
    {
        Color baseColor = _whiteScreen.color;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float value = Mathf.Sin(t * Mathf.PI);

            Color c = baseColor;
            c.a = value;
            _whiteScreen.color = c;

            yield return null;
        }

        Color end = baseColor;
        end.a = 0f;
        _whiteScreen.color = end;
    }
}
