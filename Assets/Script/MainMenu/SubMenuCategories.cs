using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class SubMenuCategories : MonoBehaviour
{
    private float sizeMultiplier = 1.2f;
    private GameObject actualSelected;
    private GameObject baseWidthSelected;
    private GameObject baseFondSelected;
    private TMP_Text baseTextSelected;
    [SerializeField] private GameObject BaseSelected;
    [SerializeField] private GameObject WidthSelected;
    [SerializeField] private GameObject FondSelected;
    [SerializeField] private TMP_Text textSelected;
    Vector3 actualSize;
    Vector2 actualWidth;

    private void Start()
    {
        ToAim(BaseSelected);
        ToAim2D(WidthSelected);
        ToAimText(textSelected);
        ToAimSelection(FondSelected);
    }


    // PARTIE ICONE QUI GROSSIS ---------------------------------------------

    public void ToAim(GameObject aimed)
    {
        ToSizeUp(aimed);
    }

    public void ToSizeUp(GameObject resized)
    {
        actualSize = resized.transform.localScale;

        if (actualSelected != resized)
        {
            StartCoroutine(LerpScale(0.1f, resized));

            if (actualSelected != null)
            {
                ToSizeDown(actualSelected);
            }
        }
        actualSelected = resized;
    }

    private void ToSizeDown(GameObject oldSelected)
    {
        StartCoroutine(LerpUnscale(0.1f, oldSelected));
    }

    IEnumerator LerpScale(float time, GameObject _SizeToUp)
    {
        float elapsed = 0;
        Vector3 toScale = actualSize * sizeMultiplier;
        while (elapsed < time)
        {
            _SizeToUp.transform.localScale = Vector3.Lerp(actualSize, toScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _SizeToUp.transform.localScale = toScale;
    }

    IEnumerator LerpUnscale(float time, GameObject _SizeToDown)
    {
        float elapsed = 0;
        Vector3 oldSize = _SizeToDown.transform.localScale;
        Vector3 toScale = _SizeToDown.transform.localScale / sizeMultiplier;
        while (elapsed < time)
        {
            _SizeToDown.transform.localScale = Vector3.Lerp(oldSize, toScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _SizeToDown.transform.localScale = toScale;
    }


    // PARTIE ZONE SELECTION ---------------------------------------------

    public void ToAimSelection(GameObject aimed)
    {
        ToHighter(aimed);
    }

    private void ToHighter(GameObject resized)
    {
        resized.transform.localScale = new Vector3(1f, 1f, 1f);
        actualSize = resized.transform.localScale;
        

        if (baseFondSelected != resized)
        {
            resized.transform.localScale /= 1.5f;
            resized.SetActive(true);
            StartCoroutine(LerpHighter(0.1f, resized));

            if (baseFondSelected != null)
            {
                ToDisapear(baseFondSelected);
            }
        }
        baseFondSelected = resized;
    }

    private void ToDisapear(GameObject oldSelected)
    {
        oldSelected.gameObject.SetActive(false);
    }

    IEnumerator LerpHighter(float time, GameObject _SizeToUp)
    {
        float elapsed = 0;
        Vector3 toScale = new Vector3(actualSize.x, actualSize.y / 1.5f, actualSize.z);
        while (elapsed < time)
        {
            _SizeToUp.transform.localScale = Vector3.Lerp(toScale, actualSize, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _SizeToUp.transform.localScale = new Vector3(1f,1f,1f);
    }



    // PARTIE CASE QUI S'ÉLARGIS ------------------------------

    public void ToAim2D(GameObject width)
    {
        ToSizeUp2D(width);
    }

    private void ToSizeUp2D(GameObject rewidth)
    {
        actualWidth = rewidth.GetComponent<RectTransform>().sizeDelta;
        if (baseWidthSelected != rewidth)
        {
            StartCoroutine(LerpScale2D(0.1f, rewidth));

            if (baseWidthSelected != null)
            {
                ToSizeDown2D(baseWidthSelected);
            }
        }
        baseWidthSelected = rewidth;
    }

    private void ToSizeDown2D(GameObject oldSelected)
    {
        StartCoroutine(LerpUnscale2D(0.1f, oldSelected));
    }

    IEnumerator LerpScale2D(float time, GameObject _WidthToUp)
    {
        float elapsed = 0;
        Vector2 toWidth = new Vector2(actualWidth.x * sizeMultiplier, actualWidth.y);
        while (elapsed < time)
        {
            _WidthToUp.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(actualWidth, toWidth, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _WidthToUp.GetComponent<RectTransform>().sizeDelta = toWidth;
    }

    IEnumerator LerpUnscale2D(float time, GameObject _SizeToDown)
    {
        float elapsed = 0;
        Vector3 oldWidth = _SizeToDown.GetComponent<RectTransform>().sizeDelta;
        Vector3 toScale = new Vector2(oldWidth.x / sizeMultiplier, oldWidth.y);
        while (elapsed < time)
        {
            _SizeToDown.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(oldWidth, toScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _SizeToDown.GetComponent<RectTransform>().sizeDelta = toScale;
    }



    // PARTIE TEXTE QUI APPARAÎT ----------------------------------------------

    public void ToAimText(TMP_Text text)
    {
        ToDisplayText(text);
    }

    private void ToDisplayText(TMP_Text text)
    {
        if (baseTextSelected != text)
        {
            StartCoroutine(LerpShowText(0.1f, text));

            if (baseTextSelected != null)
            {
                ToTextHide(baseTextSelected);
            }
        }
        baseTextSelected = text;
    }

    private void ToTextHide(TMP_Text oldSelected)
    {
        StartCoroutine(LerpHideText(0.1f, oldSelected));
    }

    IEnumerator LerpShowText(float time, TMP_Text text)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            text.alpha = Mathf.Lerp(0f, 1f, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.alpha = 1f;
    }

    IEnumerator LerpHideText(float time, TMP_Text text)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            text.alpha = Mathf.Lerp(1f, 0f, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.alpha = 0f;
    }

    

}
