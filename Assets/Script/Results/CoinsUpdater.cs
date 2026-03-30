using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CoinsUpdater : MonoBehaviour
{
    private int valueCoin;
    private int valuePremium;
    [SerializeField] private TMP_Text _textCoin;
    [SerializeField] private TMP_Text _textPremium;
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private bool waitCoin = false;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = _textCoin.transform.localScale;
        if (waitCoin)
        {
            AffichageWaitCoin();
        }
        else
        {
            AffichageCoin();
        }
        AffichagePremium();
    }

    private void AffichageWaitCoin()
    {
        valueCoin = _playerDatas.generalMonney - _playerDatas.actualCoinsNotSaved;
        _textCoin.text = SystemOfChange(valueCoin);
    }

    private void AffichageCoin()
    {
        valueCoin = _playerDatas.generalMonney;
        _textCoin.text = SystemOfChange(valueCoin);
    }

    private void AffichagePremium()
    {
        valuePremium = _playerDatas.premiumMonney;
        _textPremium.text = SystemOfChange(valuePremium);
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


    public void StartCoroutineCounter()
    {
        StartCoroutine(CoinToCounter());
    }

    IEnumerator CoinToCounter()
    {
        //faire spawn pièce
        yield return new WaitForSeconds(1);
        // faire monter le premier (1/3 des pièces)
        valueCoin = _playerDatas.generalMonney - Mathf.CeilToInt((_playerDatas.actualCoinsNotSaved/3)*2);
        _textCoin.text = SystemOfChange(valueCoin);
        StartCoroutine(SizeText());
        yield return new WaitForSeconds(0.7f);
        // faire monter le deuxième (2/3 des pièces)
        valueCoin = _playerDatas.generalMonney - Mathf.CeilToInt((_playerDatas.actualCoinsNotSaved/3));
        _textCoin.text = SystemOfChange(valueCoin);
        StartCoroutine(SizeText());
        yield return new WaitForSeconds(0.7f);
        // faire monter le dernier (3/3 des pièces)
        valueCoin = _playerDatas.generalMonney;
        _textCoin.text = SystemOfChange(valueCoin);
        StartCoroutine(SizeText());

    }

    IEnumerator SizeText()
    {
        Vector3 targetScale = originalScale * 1.3f; // agrandir à 130%

        float duration = 0.15f;
        float t = 0;

        // Agrandir
        while (t < duration)
        {
            t += Time.deltaTime;
            _textCoin.transform.localScale = Vector3.Lerp(originalScale, targetScale, t / duration);
            yield return null;
        }

        t = 0;

        // Rétrécir
        while (t < duration)
        {
            t += Time.deltaTime;
            _textCoin.transform.localScale = Vector3.Lerp(targetScale, originalScale, t / duration);
            yield return null;
        }

        _textCoin.transform.localScale = originalScale;
    }
}
