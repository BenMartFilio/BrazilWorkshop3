using TMPro;
using UnityEngine;

public class ReviveButtonPremium : MonoBehaviour
{
    [SerializeField] private EndManager _endManager;
    [SerializeField] private TMP_Text _textPrice;
    [SerializeField] private SO_PlayerDatas _playerDatas;
    private int _price = 1;
    private void OnEnable()
    {
        int revive = _endManager.reviveCounter;
        _price = 2 * revive + 1;
        _textPrice.text = _price.ToString();
    }

    public void Revival()
    {
        int monney = _playerDatas.premiumMonney;
        if (monney >= _price)
        {
            _playerDatas.premiumMonney = _playerDatas.premiumMonney - _price;
            _endManager.Revive();
        }
        else
        {
            Debug.Log("Pas d'argent");
            //Afficher boutique
        }

        _playerDatas.SaveDatas();
    }
}
