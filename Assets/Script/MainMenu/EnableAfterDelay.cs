using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnableAfterDelay : MonoBehaviour
{
    [SerializeField] private float delayBTN = 1;

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _btn.enabled = false;
        StartCoroutine(Waiter());
    }

    IEnumerator Waiter()
    {
        yield return new WaitForSeconds(delayBTN);
        _btn.enabled = true;
    }
}
