using System.Collections;
using UnityEngine;

public class WhenAspirationLogo : MonoBehaviour
{
    private Coroutine blinking;
    private void OnEnable()
    {
        blinking = StartCoroutine(CoroutineBlink());
    }

    private void OnDisable()
    {
        if (blinking != null)
        {
            StopCoroutine(blinking);
        }
    }

    IEnumerator CoroutineBlink()
    {
        yield return null;
    }

    public void AnimationWhenTouched()
    {

    }
}
