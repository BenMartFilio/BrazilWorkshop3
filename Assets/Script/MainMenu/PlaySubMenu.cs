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

    private void Start()
    {
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
        StartButtonOptionDisplay();
        StartButtonTutoDisplay();
    }

    private void StartButtonDisplay()
    {
        StartCoroutine(LerpPosition(0.2f,_playBouton, new Vector3(0,-200,0)));
    }
    private void StartButtonOptionDisplay()
    {
        StartCoroutine(LerpPosition(0.2f,_optionBouton, new Vector3(-150,0,0)));
    }
    private void StartButtonTutoDisplay()
    {
        StartCoroutine(LerpPosition(0.2f,_tutoBouton, new Vector3(150,0,0)));
    }


    IEnumerator LerpPosition(float time, GameObject Bouton, Vector3 newDirection)
    {
        RectTransform _rectBouton = Bouton.GetComponent<RectTransform>();
        Vector3 _boutonPosition = _rectBouton.localPosition;
        float elapsed = 0;
        Vector3 newPosition = _boutonPosition+newDirection;
        
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
