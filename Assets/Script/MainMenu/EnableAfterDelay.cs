// EnableAfterDelay.cs — cache le WaitForSeconds
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnableAfterDelay : MonoBehaviour
{
    [SerializeField] private float delayBTN = 1f;

    private Button _btn;
    private WaitForSeconds _wait; // alloué une seule fois

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _wait = new WaitForSeconds(delayBTN);
    }

    private void OnEnable()
    {
        _btn.enabled = false;
        StartCoroutine(Waiter());
    }

    private IEnumerator Waiter()
    {
        yield return _wait;
        _btn.enabled = true;
    }
}