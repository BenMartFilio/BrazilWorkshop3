using UnityEngine;
using UnityEngine.Pool;

public class CoinsScript : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private GameObject coinFeedbackPrefab;

    // Pool partagé entre toutes les pièces de la scène (statique)
    private static IObjectPool<CoinFeedbackEffect> s_pool;
    private static CoinFeedbackEffect s_prefabRef;
    private static Transform s_poolRoot;

    private void Awake()
    {
        if (s_pool != null || coinFeedbackPrefab == null) return;

        if (!coinFeedbackPrefab.TryGetComponent<CoinFeedbackEffect>(out CoinFeedbackEffect prefabEffect))
        {
            Debug.LogError("[CoinsScript] Le prefab n'a pas de composant CoinFeedbackEffect.");
            return;
        }

        s_prefabRef = prefabEffect;
        s_poolRoot = new GameObject("[Pool] CoinFeedback").transform;
        DontDestroyOnLoad(s_poolRoot.gameObject);

        s_pool = new ObjectPool<CoinFeedbackEffect>(
            createFunc: CreateFeedback,
            actionOnGet: fx => fx.gameObject.SetActive(true),
            actionOnRelease: fx => fx.gameObject.SetActive(false),
            actionOnDestroy: fx => Destroy(fx.gameObject),
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 16
        );
    }

    public void OnCoinRecuperation()
    {
        SpawnFeedback();
        gameObject.SetActive(false);
    }

    private void SpawnFeedback()
    {
        if (s_pool == null) return;
        CoinFeedbackEffect fx = s_pool.Get();
        fx.Play(transform.position, transform.localScale);
    }

    private static CoinFeedbackEffect CreateFeedback()
    {
        CoinFeedbackEffect fx = Instantiate(s_prefabRef, s_poolRoot);
        fx.gameObject.SetActive(false);
        fx.SetPool(s_pool);
        return fx;
    }
}