using System.Collections;
using UnityEngine;

public class GlobalMenu : MonoBehaviour
{
    [SerializeField] private GameObject _SizeToUp;
    [SerializeField] private GameObject _unHideIt;
    Vector3 aScale;
    private void Start()
    {
        aScale = _SizeToUp.transform.localScale;
    }
    public void OpenMenu()
    {
        StartToPrint();
        SizeUp();
    }

    public void CloseMenu()
    {
        StartToHide();
    }


    public void SizeUp()
    {
        StartCoroutine(LerpPosition(0.1f));

    }

    IEnumerator LerpPosition(float time)
    {
        float elapsed = 0;
        Vector3 toScale = aScale * 0.85f;
        while (elapsed < time)
        {
            _SizeToUp.transform.localScale = Vector3.Lerp(toScale, aScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _SizeToUp.transform.localScale = aScale;
    }

    public void StartToPrint()
    {
        _unHideIt.SetActive(true);
    }

    public void StartToHide()
    {
        _unHideIt.SetActive(false);
    }
}
