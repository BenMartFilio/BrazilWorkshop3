using UnityEngine;

public class EndManager : MonoBehaviour
{
    [SerializeField] private TimeManager _timeManager;
    public void OnDeath()
    {
        _timeManager.StopTime();
    }


    public void Revive()
    {
        _timeManager.StartTime();
    }
}
