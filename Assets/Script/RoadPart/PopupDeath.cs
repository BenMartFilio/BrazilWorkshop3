using System.Collections;
using UnityEngine;

public class PopupDeath : MonoBehaviour
{
    Vector3 aScale;

    private void Start()
    {
    }
    private void OnEnable()
    {
        aScale = transform.localScale;
        StartCoroutine(LerpScale(0.1f));
    }
    IEnumerator LerpScale(float time)
    {
        Debug.Log("ScaleAgain");
        float elapsed = 0;
        Vector3 toScale = aScale * 0.85f;
        while (elapsed < time)
        {
            transform.localScale = Vector3.Lerp(toScale, aScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = aScale;
    }
}
