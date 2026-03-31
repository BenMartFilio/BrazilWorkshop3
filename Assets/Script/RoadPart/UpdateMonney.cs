using TMPro;
using UnityEngine;

public class UpdateMonney : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private TMP_Text _textMonney;

    private void OnEnable()
    {
        Updater();
    }
    public void Updater()
    {
        _textMonney.text = _playerDatas.premiumMonney.ToString();
    }
}
