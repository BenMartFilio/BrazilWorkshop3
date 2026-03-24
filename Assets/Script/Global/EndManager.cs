using UnityEngine;

public class EndManager : MonoBehaviour
{
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GameObject _revivePanel;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private GoundMouvement[] _grounds;
    [SerializeField] private SpawnObstacleV2 _spawner;
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
    }

    private void RevivePanelDisplay()
    {
        _revivePanel.SetActive(true);
    }
}
