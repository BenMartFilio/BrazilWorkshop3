using System.Collections;
using UnityEngine;

public class PlaySubMenu : MonoBehaviour
{
    [SerializeField] private GameObject _playMenu;
    [SerializeField] private GameObject _playBouton;
    private RectTransform _rectBouton;
    private Vector3 _boutonPosition;

    [SerializeField] private GameObject _shopMenu;
    [SerializeField] private GameObject _inventoryMenu;

    private void Start()
    {
        _rectBouton = _playBouton.GetComponent<RectTransform>();
        _boutonPosition = _rectBouton.localPosition;
        OnPlayDisplay();
    }
    public void OnPlayDisplay()
    {
        if (_playMenu.activeInHierarchy == true)
        {
            return;
        }
        _playMenu.SetActive(true);
        _playBouton.SetActive(true);
        _shopMenu.SetActive(false);
        _inventoryMenu.SetActive(false);
        StartButtonDisplay();
    }

    private void StartButtonDisplay()
    {
        StartCoroutine(LerpPosition(0.2f));
    }


    IEnumerator LerpPosition(float time)
    {
        float elapsed = 0;
        Vector3 newPosition = new Vector3 (_boutonPosition.x, _boutonPosition.y-200, _boutonPosition.z);
        
        _rectBouton.localPosition = newPosition;
        
        while (elapsed < time)
        {
            _rectBouton.localPosition = Vector3.Lerp(newPosition, _boutonPosition, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _rectBouton.localPosition = _boutonPosition;
    }


    public void OnShopDisplay()
    {
        if (_shopMenu.activeInHierarchy == true)
        {
            return;
        }
        _shopMenu.SetActive(true);
        _playMenu.SetActive(false);
        _playBouton.SetActive(false);
        _inventoryMenu.SetActive(false);
    }
    public void OnInventoryDisplay()
    {
        if ( _inventoryMenu.activeInHierarchy == true)
        {
            return;
        }
        _inventoryMenu.SetActive(true);
        _shopMenu.SetActive(false);
        _playMenu.SetActive(false);
        _playBouton.SetActive(false);
    }
}
