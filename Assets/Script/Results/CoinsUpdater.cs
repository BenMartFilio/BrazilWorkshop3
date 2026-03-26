using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CoinsUpdater : MonoBehaviour
{
    private int value;
    [SerializeField] private TMP_Text _textCoin;
    [SerializeField] private SO_PlayerDatas _playerDatas;

    private void Start()
    {
        Affichage();
    }
    private void Affichage()
    {
        value = _playerDatas.generalMonney;
        _textCoin.text = SystemOfChange(value);
    }

    private string SystemOfChange(int value)
    {
        if (value < 1_000_000)
        {
            return value.ToString("N0").Replace(",", " ");
        }
        else if (value < 1_000_000_000)
        {
            float v = value / 1_000_000f;
            return v.ToString("0.000") + " M";
        }
        else if (value < 1_000_000_000_000)
        {
            float v = value / 1_000_000_000f;
            return v.ToString("0.000") + " Md";
        }
        else
        {
            float v = value / 1_000_000_000_000f;
            return v.ToString("0.000") + " B"; // billion FR (optionnel)
        }
    }
}
