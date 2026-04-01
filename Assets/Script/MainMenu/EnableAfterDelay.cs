using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnableAfterDelay : MonoBehaviour
{
    private Button btn;
    [SerializeField] private float delayBTN = 1;
    private void OnEnable()
    {
        btn = GetComponent<Button>();
        btn.enabled = false;
        StartCoroutine(Waiter());
    }

    IEnumerator Waiter()
    {
        yield return new WaitForSeconds(delayBTN);
        btn.enabled = true;
    }
}
