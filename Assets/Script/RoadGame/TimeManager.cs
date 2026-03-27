using UnityEngine;
using System.Collections;
using System;

public class TimeManager : MonoBehaviour
{
   // [SerializeField] private SO_PlayerDatas playerDatas;

    [SerializeField] public float _timeStepDuration = 1.5f;
    Coroutine coroutineTemps = null;
    public event Action OnTimePassed;
    private float tempTime = 0f;
    private float NotResetTime = 0f;

    IEnumerator SpendingTime()
    {
        while (true)
        {
            tempTime = 0f;
            float stepDuration = _timeStepDuration+NotResetTime;

            while (tempTime < stepDuration)
            {
                tempTime += Time.deltaTime;
                yield return null;
            }

            OnTimePassed?.Invoke();
        }
    }


    public void Start()
    {
        StartTime();
    }

    public void StartTime()
    {
        coroutineTemps = StartCoroutine(SpendingTime());
    }

    public void StopTime()
    {
        StopCoroutine(coroutineTemps);
        NotResetTime = tempTime;
    }

    public void UpdateSpeedTimer(float newTime)
    {
        _timeStepDuration = newTime;
    }
}
