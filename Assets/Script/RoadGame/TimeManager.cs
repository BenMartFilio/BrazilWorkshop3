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


    [Tooltip("Si false, le TimeManager ne démarre pas automatiquement dans Start(). " +
             "Utile quand un système externe (ex : MapRoadSessionBridge) contrôle le démarrage.")]
    [SerializeField] private bool démarrerAutomatiquement = true;

    public void Start()
    {
        if (démarrerAutomatiquement)
            StartTime();
    }

    public void StartTime()
    {
        if (coroutineTemps != null)
            StopCoroutine(coroutineTemps);

        coroutineTemps = StartCoroutine(SpendingTime());
    }

    public void StopTime()
    {
        if (coroutineTemps == null) return;

        StopCoroutine(coroutineTemps);
        coroutineTemps = null;
        NotResetTime   = tempTime;
    }

    public void UpdateSpeedTimer(float newTime)
    {
        _timeStepDuration = newTime;
    }


    public void Reupdate()
    {
        OnTimePassed?.Invoke();
    }
}
