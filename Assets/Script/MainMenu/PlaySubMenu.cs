// PlaySubMenu.cs — cache les RectTransforms dans Awake
using System.Collections;
using UnityEngine;

public class PlaySubMenu : MonoBehaviour
{
    [SerializeField] private GameObject _playMenu;
    [SerializeField] private GameObject _playBouton;
    [SerializeField] private GameObject _optionBouton;
    [SerializeField] private GameObject _tutoBouton;
    [SerializeField] private GameObject _shopMenu;
    [SerializeField] private GameObject _inventoryMenu;

    // RectTransforms cachés — plus de GetComponent dans les coroutines
    private RectTransform _rtPlayBouton;
    private RectTransform _rtOptionBouton;
    private RectTransform _rtTutoBouton;

    private Vector3 _posPlayBouton;
    private Vector3 _posOptionBouton;
    private Vector3 _posTutoBouton;

    private Coroutine _animPlay;
    private Coroutine _animOption;
    private Coroutine _animTuto;

    private void Awake()
    {
    }

    private void Start() 
    {  
        _rtPlayBouton = _playBouton.GetComponent<RectTransform>();
        _rtOptionBouton = _optionBouton.GetComponent<RectTransform>();
        _rtTutoBouton = _tutoBouton.GetComponent<RectTransform>();
        _posPlayBouton = _rtPlayBouton.localPosition;
        _posOptionBouton = _rtOptionBouton.localPosition;
        _posTutoBouton = _rtTutoBouton.localPosition;
        OnPlayDisplay();
    }

    public void OnPlayDisplay()
    {
        if (_playMenu.activeInHierarchy) return;

        _playMenu.SetActive(true);
        _playBouton.SetActive(true);
        _shopMenu.SetActive(false);
        _inventoryMenu.SetActive(false);

        StartAnim(ref _animPlay, _rtPlayBouton, _posPlayBouton, new Vector3(0, -200, 0));
        StartAnim(ref _animOption, _rtOptionBouton, _posOptionBouton, new Vector3(-150, 0, 0));
        StartAnim(ref _animTuto, _rtTutoBouton, _posTutoBouton, new Vector3(150, 0, 0));
    }

    public void OnShopDisplay()
    {
        if (_shopMenu.activeInHierarchy) return;
        _shopMenu.SetActive(true);
        _playMenu.SetActive(false);
        _playBouton.SetActive(false);
        _inventoryMenu.SetActive(false);
    }

    public void OnInventoryDisplay()
    {
        if (_inventoryMenu.activeInHierarchy) return;
        _inventoryMenu.SetActive(true);
        _shopMenu.SetActive(false);
        _playMenu.SetActive(false);
        _playBouton.SetActive(false);
    }

    private void StartAnim(ref Coroutine slot, RectTransform rt, Vector3 targetPos, Vector3 offset)
    {
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(LerpPosition(0.2f, rt, targetPos, offset));
    }

    private IEnumerator LerpPosition(float duration, RectTransform rt, Vector3 endPos, Vector3 offset)
    {
        Vector3 startPos = endPos + offset;
        rt.localPosition = startPos;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rt.localPosition = Vector3.Lerp(startPos, endPos, EaseOutCubic(elapsed / duration));
            yield return null;
        }
        rt.localPosition = endPos;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}