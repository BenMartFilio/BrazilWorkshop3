using UnityEngine;

public class PlaySubMenu : MonoBehaviour
{
    [SerializeField] private GameObject _playMenu;
    [SerializeField] private GameObject _playBouton;
    private RectTransform _rectBouton;

    [SerializeField] private GameObject _shopMenu;
    [SerializeField] private GameObject _inventoryMenu;

    private void Start()
    {
        _rectBouton = _playBouton.GetComponent<RectTransform>();
        OnPlayDisplay();
    }
    public void OnPlayDisplay()
    {
        _playMenu.SetActive(true);
        _shopMenu.SetActive(false);
        _inventoryMenu.SetActive(false);
    }

    private void StartButtonDisplay()
    {
    }





    public void OnShopDisplay()
    {
        _shopMenu.SetActive(true);
        _playMenu.SetActive(false);
        _inventoryMenu.SetActive(false);
    }
    public void OnInventoryDisplay()
    {
        _inventoryMenu.SetActive(true);
        _shopMenu.SetActive(false);
        _playMenu.SetActive(false);
    }
}
