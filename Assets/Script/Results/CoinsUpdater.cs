using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CoinsUpdater : MonoBehaviour
{
    private int valueCoin;
    private int valuePremium;
    [SerializeField] private TMP_Text _textCoin;
    [SerializeField] private TMP_Text _textPremium;
    [SerializeField] private TMP_Text _textPseudo;
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private bool waitCoin = false;
    private Vector3 originalScale;

    [SerializeField] GameObject coinPrefab;
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform targetText;

    List<GameObject> spawnedCoins = new List<GameObject>();

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
        AffichagePseudo();
    }

    private void AffichageWaitCoin()
    {
        valueCoin = _playerDatas.generalMonney - _playerDatas.actualCoinsNotSaved;
        _textCoin.text = SystemOfChange(valueCoin);
    }

    public void AffichagePseudo()
    {
        if (_playerDatas.Name != null)
        {
            _textPseudo.text = _playerDatas.Name;
        }
        else
        {
            _textPseudo.text = "AnonymousPlayer";
        }
    }

    public void AffichageCoin()
    {
        valueCoin = _playerDatas.generalMonney;
        _textCoin.text = SystemOfChange(valueCoin);
    }

    public void AffichagePremium()
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
        if (_playerDatas.actualCoinsNotSaved == 0)
        {
            return;
        }
        StartCoroutine(CoinToCounter());
    }

    IEnumerator CoinToCounter()
    {
        //faire spawn pièce
        yield return StartCoroutine(SpawnCoins());

        yield return new WaitForSeconds(0.3f);
        // faire monter le premier (1/3 des pièces)
        yield return MoveCoin(spawnedCoins[0]);
        valueCoin = _playerDatas.generalMonney - Mathf.CeilToInt((_playerDatas.actualCoinsNotSaved/3)*2);
        _textCoin.text = SystemOfChange(valueCoin);
        StartCoroutine(SizeText());

    //    yield return new WaitForSeconds(0.1f);
        yield return MoveCoin(spawnedCoins[1]);
        // faire monter le deuxième (2/3 des pièces)
        valueCoin = _playerDatas.generalMonney - Mathf.CeilToInt((_playerDatas.actualCoinsNotSaved/3));
        _textCoin.text = SystemOfChange(valueCoin);
        StartCoroutine(SizeText());

    //    yield return new WaitForSeconds(0.1f);
        yield return MoveCoin(spawnedCoins[2]);
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



    IEnumerator SpawnCoins()
    {
        spawnedCoins.Clear();
        Vector2 basePos = spawnPoint.GetComponent<RectTransform>().anchoredPosition;

        for (int i = 0; i < 3; i++)
        {
            GameObject coin = Instantiate(coinPrefab, spawnPoint.position, Quaternion.identity, spawnPoint.parent);
            RectTransform rt = coin.GetComponent<RectTransform>();
            float randomY = Random.Range(-10f, 10f);
            float randomX = Random.Range(-5f, 5f);   

            rt.anchoredPosition = basePos + new Vector2(i * 30f + randomX, randomY);

            coin.transform.SetAsLastSibling();
            rt.localScale = Vector3.zero;

            spawnedCoins.Add(coin);

            StartCoroutine(BounceCoin(rt));

            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator MoveCoin(GameObject coin)
    {
        RectTransform rt = coin.GetComponent<RectTransform>();

        Vector3 start = coin.transform.position;
        Vector3 end = targetText.position;

        float duration = Random.Range(0.2f, 0.4f); 
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, t / duration);

            coin.transform.position = Vector3.Lerp(start, end, progress)
                                    + Vector3.up * Mathf.Sin(progress * Mathf.PI) * 50f;

            float scale = Mathf.Lerp(1f, 0.7f, progress);
            rt.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(coin);
    }

    IEnumerator BounceCoin(RectTransform rt)
    {
        Vector3 normalScale = Vector3.one;
        Vector3 smallScale = Vector3.zero;
        Vector3 bigScale = Vector3.one * 1.3f;

        float t = 0;

        float duration1 = 0.15f;
        while (t < duration1)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(smallScale, bigScale, t / duration1);
            yield return null;
        }

        t = 0;

        float duration2 = 0.1f;
        while (t < duration2)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(bigScale, normalScale, t / duration2);
            yield return null;
        }

        rt.localScale = normalScale;
    }
}
