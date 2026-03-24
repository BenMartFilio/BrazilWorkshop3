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
    [SerializeField] private GameObject _whiteScreen;
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
    }


    public void Revive()
    {
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

    IEnumerator Whiter()
    {
        float t = 0;
        float a = 0;
        Image blanc = _whiteScreen.GetComponent<Image>();
        Color tempColor = blanc.color;
        while (t < 1)
        {
            t += Time.deltaTime;
            a = Mathf.Lerp(0, 1, t);
            tempColor.a = a;
            blanc.color = tempColor;
            yield return null;
        }
        
    }
}
