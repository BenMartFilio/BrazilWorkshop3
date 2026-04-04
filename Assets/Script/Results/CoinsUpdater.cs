// CoinsUpdater.cs — cache les RectTransforms + supprime import inutile
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CoinsUpdater : MonoBehaviour
{
    private int _valueCoin;
    private int _valuePremium;

    [SerializeField] private TMP_Text _textCoin;
    [SerializeField] private TMP_Text _textPremium;
    [SerializeField] private TMP_Text _textPseudo;
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private bool waitCoin = false;

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform targetText;
    [SerializeField] private AudioSource coinAudioSource;
    [SerializeField] private AudioClip musicAudioClip;

    private Vector3 _originalScale;
    private List<GameObject> _spawnedCoins = new List<GameObject>(3);
    // RectTransform du spawnPoint — caché une fois
    private RectTransform _spawnPointRT;

    private void Awake()
    {
        _spawnPointRT = spawnPoint != null ? spawnPoint.GetComponent<RectTransform>() : null;
    }

    private void OnEnable()
    {
        if (_playerDatas != null)
            _playerDatas.OnMonneyChanged += RefreshAll;
        SaveGameSystem.OnSaveLoaded += RefreshAll;
    }

    private void OnDisable()
    {
        if (_playerDatas != null)
            _playerDatas.OnMonneyChanged -= RefreshAll;
        SaveGameSystem.OnSaveLoaded -= RefreshAll;
    }

    private void RefreshAll()
    {
        AffichageCoin();
        AffichagePremium();
        AffichagePseudo();
    }

    private void Start()
    {
        if (musicAudioClip != null)
            SoundManager.Instance?.PlayMusicWithLowPass(musicAudioClip);

        _originalScale = _textCoin.transform.localScale;

        if (waitCoin)
            AffichageWaitCoin();
        else
            AffichageCoin();

        AffichagePremium();
        AffichagePseudo();
    }

    private void AffichageWaitCoin()
    {
        _valueCoin = _playerDatas.generalMonney - _playerDatas.actualCoinsNotSaved;
        _textCoin.text = SystemOfChange(_valueCoin);
    }

    public void AffichagePseudo()
    {
        _textPseudo.text = string.IsNullOrEmpty(_playerDatas.Name)
            ? "AnonymousPlayer"
            : _playerDatas.Name;
    }

    public void AffichageCoin()
    {
        _valueCoin = _playerDatas.generalMonney;
        _textCoin.text = SystemOfChange(_valueCoin);
    }

    public void AffichagePremium()
    {
        _valuePremium = _playerDatas.premiumMonney;
        _textPremium.text = SystemOfChange(_valuePremium);
    }

    private string SystemOfChange(int value)
    {
        if (value < 1_000_000)
            return value.ToString("N0").Replace(",", " ");
        if (value < 1_000_000_000)
            return (value / 1_000_000f).ToString("0.000") + " M";
        if (value < 1_000_000_000_000L)
            return (value / 1_000_000_000f).ToString("0.000") + " Md";
        return (value / 1_000_000_000_000f).ToString("0.000") + " B";
    }

    public void StartCoroutineCounter()
    {
        if (_playerDatas.actualCoinsNotSaved == 0) return;
        StartCoroutine(CoinToCounter());
    }

    private IEnumerator CoinToCounter()
    {
        yield return StartCoroutine(SpawnCoins());
        yield return new WaitForSeconds(0.3f);

        if (coinAudioSource != null) coinAudioSource.Play();

        yield return MoveCoin(_spawnedCoins[0]);
        _valueCoin = _playerDatas.generalMonney - Mathf.CeilToInt((_playerDatas.actualCoinsNotSaved / 3) * 2);
        _textCoin.text = SystemOfChange(_valueCoin);
        StartCoroutine(SizeText());

        yield return MoveCoin(_spawnedCoins[1]);
        _valueCoin = _playerDatas.generalMonney - Mathf.CeilToInt(_playerDatas.actualCoinsNotSaved / 3);
        _textCoin.text = SystemOfChange(_valueCoin);
        StartCoroutine(SizeText());

        yield return MoveCoin(_spawnedCoins[2]);
        _valueCoin = _playerDatas.generalMonney;
        _textCoin.text = SystemOfChange(_valueCoin);
        StartCoroutine(SizeText());

        if (coinAudioSource != null) coinAudioSource.Stop();
    }

    private IEnumerator SizeText()
    {
        Vector3 targetScale = _originalScale * 1.3f;
        float duration = 0.15f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            _textCoin.transform.localScale = Vector3.Lerp(_originalScale, targetScale, t / duration);
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _textCoin.transform.localScale = Vector3.Lerp(targetScale, _originalScale, t / duration);
            yield return null;
        }

        _textCoin.transform.localScale = _originalScale;
    }

    private IEnumerator SpawnCoins()
    {
        _spawnedCoins.Clear();

        // Utilise le RectTransform caché dans Awake
        Vector2 basePos = _spawnPointRT != null
            ? _spawnPointRT.anchoredPosition
            : Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            GameObject coin = Instantiate(coinPrefab, spawnPoint.position,
                                          Quaternion.identity, spawnPoint.parent);

            RectTransform rt = coin.GetComponent<RectTransform>(); // ok — une seule fois par pièce
            rt.anchoredPosition = basePos + new Vector2(
                i * 30f + Random.Range(-5f, 5f),
                Random.Range(-10f, 10f));

            coin.transform.SetAsLastSibling();
            rt.localScale = Vector3.zero;

            _spawnedCoins.Add(coin);
            StartCoroutine(BounceCoin(rt));

            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator MoveCoin(GameObject coin)
    {
        RectTransform rt = coin.GetComponent<RectTransform>(); // ok — une seule fois
        Vector3 start = coin.transform.position;
        Vector3 end = targetText.position;
        float duration = Random.Range(0.2f, 0.4f);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / duration);
            coin.transform.position = Vector3.Lerp(start, end, progress)
                                    + Vector3.up * Mathf.Sin(progress * Mathf.PI) * 50f;
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, progress);
            yield return null;
        }

        Destroy(coin);
    }

    private IEnumerator BounceCoin(RectTransform rt)
    {
        float t = 0f;
        float duration1 = 0.15f;

        while (t < duration1)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.3f, t / duration1);
            yield return null;
        }

        t = 0f;
        float duration2 = 0.1f;
        while (t < duration2)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t / duration2);
            yield return null;
        }

        rt.localScale = Vector3.one;
    }
}