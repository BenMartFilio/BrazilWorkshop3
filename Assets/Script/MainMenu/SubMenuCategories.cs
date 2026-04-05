using System;
using System.Collections;
using TMPro;
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
    [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
    [SerializeField] private AudioType _tabSound;
    private bool coroutineAnim;
    private float animDuration = 0.1f;
    Vector3 actualSize;
    Vector2 actualWidth;

    // Cache des RectTransform pour éviter GetComponent à chaque frame d'animation
    private RectTransform _widthSelectedRect;
    private RectTransform _baseWidthSelectedRect;

    private void Start()
    {
        _widthSelectedRect = WidthSelected != null ? WidthSelected.GetComponent<RectTransform>() : null;

        ToAim(BaseSelected);
        ToAim2D(WidthSelected);
        ToAimText(textSelected);
        ToAimSelection(FondSelected);
    }

    IEnumerator Delay(GameObject toGiveBack, Action<GameObject> callback)
    {
        yield return new WaitForSeconds(animDuration);
        callback?.Invoke(toGiveBack);
    }
    IEnumerator Delay2(TMP_Text toGiveBack)
    {
        yield return new WaitForSeconds(animDuration);
        ToDisplayText(toGiveBack);
    }

    // PARTIE ICONE QUI GROSSIS ---------------------------------------------

    public void ToAim(GameObject aimed)
    {
        ToSizeUp(aimed);
    }

    public void ToSizeUp(GameObject resized)
    {
        if (coroutineAnim == true)
        {
            StartCoroutine(Delay(resized, ToSizeUp));
        }
        else
        {
            actualSize = resized.transform.localScale;

            if (actualSelected != resized)
            {
                _audioEventDispatcher?.PlayAudio(_tabSound);

                StartCoroutine(LerpScale(animDuration, resized));

                if (actualSelected != null)
                    ToSizeDown(actualSelected);
            }
            actualSelected = resized;
        }
    }

    private void ToSizeDown(GameObject oldSelected)
    {
        StartCoroutine(LerpUnscale(animDuration, oldSelected));
    }

    IEnumerator LerpScale(float time, GameObject _SizeToUp)
    {
        coroutineAnim = true;
        float elapsed = 0;
        Vector3 toScale = actualSize * sizeMultiplier;
        while (elapsed < time)
        {
            _SizeToUp.transform.localScale = Vector3.Lerp(actualSize, toScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _SizeToUp.transform.localScale = toScale;
        coroutineAnim = false;
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
        if (coroutineAnim == true)
        {
            StartCoroutine(Delay(resized, ToHighter));
        }
        else 
        {
            resized.transform.localScale = new Vector3(1f, 1f, 1f);
            actualSize = resized.transform.localScale;


            if (baseFondSelected != resized)
            {
                resized.transform.localScale /= 1.5f;
                resized.SetActive(true);
                StartCoroutine(LerpHighter(animDuration, resized));

                if (baseFondSelected != null)
                {
                    ToDisapear(baseFondSelected);
                }
            }
            baseFondSelected = resized;
        }
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



    // PARTIE CASE QUI S'ELARGIS ------------------------------

    public void ToAim2D(GameObject width)
    {
        ToSizeUp2D(width);
    }

    private void ToSizeUp2D(GameObject rewidth)
    {
        if (coroutineAnim == true)
        {
            StartCoroutine(Delay(rewidth, ToSizeUp2D));
        }
        else
        {
            RectTransform rt = GetOrCacheRect(rewidth);
            actualWidth = rt.sizeDelta;
            if (baseWidthSelected != rewidth)
            {
                StartCoroutine(LerpScale2D(animDuration, rewidth, rt));

                if (baseWidthSelected != null)
                    ToSizeDown2D(baseWidthSelected);
            }
            baseWidthSelected = rewidth;
            _baseWidthSelectedRect = rt;
        }
    }

    private void ToSizeDown2D(GameObject oldSelected)
    {
        RectTransform rt = GetOrCacheRect(oldSelected);
        StartCoroutine(LerpUnscale2D(animDuration, oldSelected, rt));
    }

    IEnumerator LerpScale2D(float time, GameObject _WidthToUp, RectTransform rt)
    {
        float elapsed = 0;
        Vector2 toWidth = new Vector2(actualWidth.x * sizeMultiplier, actualWidth.y);
        while (elapsed < time)
        {
            rt.sizeDelta = Vector2.Lerp(actualWidth, toWidth, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.sizeDelta = toWidth;
    }

    IEnumerator LerpUnscale2D(float time, GameObject _SizeToDown, RectTransform rt)
    {
        float elapsed = 0;
        Vector2 oldWidth = rt.sizeDelta;
        Vector2 toScale = new Vector2(oldWidth.x / sizeMultiplier, oldWidth.y);
        while (elapsed < time)
        {
            rt.sizeDelta = Vector2.Lerp(oldWidth, toScale, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.sizeDelta = toScale;
    }

    // Retourne le RectTransform depuis le cache ou via GetComponent (une seule fois)
    private readonly System.Collections.Generic.Dictionary<GameObject, RectTransform> _rectCache
        = new System.Collections.Generic.Dictionary<GameObject, RectTransform>();

    private RectTransform GetOrCacheRect(GameObject go)
    {
        if (!_rectCache.TryGetValue(go, out RectTransform rt))
        {
            rt = go.GetComponent<RectTransform>();
            _rectCache[go] = rt;
        }
        return rt;
    }



    // PARTIE TEXTE QUI APPARA�T ----------------------------------------------

    public void ToAimText(TMP_Text text)
    {
        ToDisplayText(text);
    }

    private void ToDisplayText(TMP_Text text)
    {
        if (coroutineAnim == true)
        {
            StartCoroutine(Delay2(text));
        }
        else
        {
            if (baseTextSelected != text)
            {
                StartCoroutine(LerpShowText(animDuration, text));

                if (baseTextSelected != null)
                {
                    ToTextHide(baseTextSelected);
                }
            }
            baseTextSelected = text;
        }
    }

    private void ToTextHide(TMP_Text oldSelected)
    {
        StartCoroutine(LerpHideText(animDuration, oldSelected));
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
