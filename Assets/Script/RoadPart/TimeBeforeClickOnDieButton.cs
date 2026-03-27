using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TimeBeforeClickOnDieButton : MonoBehaviour
{
    [SerializeField] private Button buttonToEnable;
    private Coroutine coroutine;
    private void OnEnable()
    {
        buttonToEnable.interactable = false;
        coroutine = StartCoroutine(TimeBefore());
    }

    private void OnDisable()
    {
        StopCoroutine(coroutine);
        buttonToEnable.interactable = false;
    }

    IEnumerator TimeBefore()
    {
        yield return new WaitForSeconds(1);
        EnableButton();
    }

    private void EnableButton()
    {
        buttonToEnable.interactable = true;
    }
}
