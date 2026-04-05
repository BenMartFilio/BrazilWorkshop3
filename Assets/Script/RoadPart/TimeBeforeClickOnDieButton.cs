// TimeBeforeClickOnDieButton.cs — supprime import inutile + cache WaitForSeconds
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimeBeforeClickOnDieButton : MonoBehaviour
{
    [SerializeField] private Button buttonToEnable;
    [SerializeField] private float delay = 1f;

    private Coroutine _coroutine;
    private WaitForSeconds _wait;

    private void Awake() => _wait = new WaitForSeconds(delay);

    private void OnEnable()
    {
        buttonToEnable.interactable = false;
        _coroutine = StartCoroutine(TimeBefore());
    }

    private void OnDisable()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        buttonToEnable.interactable = false;
    }

    private IEnumerator TimeBefore()
    {
        yield return _wait;
        buttonToEnable.interactable = true;
    }
}