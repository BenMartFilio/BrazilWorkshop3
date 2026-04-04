using UnityEngine;
using UnityEngine.Pool;

public class ScrollingElement : MonoBehaviour
{
    private float speed = 0f;
    public float baseSpeed = 5f;
    private bool isMoving = true;
    private SpriteRenderer _sprite;
    [SerializeField] private AudioClip _crashSound;

    /// <summary>
    /// Multiplicateur global applique a toutes les vitesses calculees dans UpdateSpeed.
    /// Mis a 0.5f par EffetsObjetsSpeciaux (Montre a Gousset), restaure a 1f a la fin.
    /// Statique : s'applique automatiquement a tout spawn futur sans snapshot.
    /// </summary>
    public static float FacteurVitesseGlobal = 1f;

    // ── Pool d'explosions (partagé entre toutes les instances) ────────────────
    private static IObjectPool<GameObject> s_fxPool;
    private static GameObject s_fxPrefabRef;
    private static Transform s_fxPoolRoot;

    public void UpdateSpeed(float addToNewSpeed)
    {
        if (inCarcasse)
        {
            speed = Mathf.Clamp((tempbaseSpeed + addToNewSpeed) * FacteurVitesseGlobal, 0, (30 + tempbaseSpeed) * FacteurVitesseGlobal);
        }
        else
        {
            speed = Mathf.Clamp((baseSpeed + addToNewSpeed) * FacteurVitesseGlobal, 0, (30 + baseSpeed) * FacteurVitesseGlobal);
        }
    }

    void FixedUpdate()
    {
        if (!isMoving)
        {
            return;
        }
        if (_rb == null)
        {
            transform.Translate(Vector3.down * speed * 0.8f * Time.fixedDeltaTime);

            if (transform.position.y < -20)
            {
                inCarcasse = false;
                isExploded = false;
                if (tempSprite != null)
                {
                    _sprite.sprite = tempSprite;
                }
                gameObject.SetActive(false);
            }
        }
        else
        {
            _rb.MovePosition(_rb.position + Vector2.down * speed * 0.8f * Time.fixedDeltaTime);

            if (_rb.position.y < -20)
                gameObject.SetActive(false);
        }
    }
    private void Start()
    {
        tempbaseSpeed = -(baseSpeed * carcasseSpeedFactor);
    }

    private void StartToUsed(float addToNewSpeed)
    {
        speed = Mathf.Clamp(baseSpeed + addToNewSpeed, 0, 30 + baseSpeed);
    }


    private Rigidbody2D _rb;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponentInChildren<SpriteRenderer>();

        // Initialise le pool FX une seule fois dès qu'une instance en a besoin
        if (FXExplosion != null && s_fxPool == null)
            InitFxPool(FXExplosion);
    }

    public void StopMoving()
    {
        isMoving = false;
    }
    public void StartMoving()
    {
        isMoving = true;
    }

    public void Dispawn()
    {
        gameObject.SetActive(false);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    private void OnEnable()
    {
        inCarcasse = false;
        isExploded = false;
        if (_sprite == null)
            _sprite = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
        if (tempSprite != null && _sprite != null)
            _sprite.sprite = tempSprite;
    }







    // ---------------- EXPLOSION PARTIE -------------------

    private ScrollingElement coll;
    public bool isExploded = false;
    public GameObject FXExplosion;
    public Sprite carcasse;
    private bool inCarcasse = false;
    [SerializeField] private float carcasseSpeedFactor = 0.4f;
    private float tempbaseSpeed;
    private Sprite tempSprite;
    public bool canExplose = true;

    private static void InitFxPool(GameObject prefab)
    {
        s_fxPrefabRef = prefab;
        s_fxPoolRoot = new GameObject("[Pool] ExplosionFX").transform;
        Object.DontDestroyOnLoad(s_fxPoolRoot.gameObject);

        s_fxPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = Instantiate(s_fxPrefabRef, s_fxPoolRoot);
                go.SetActive(false);
                return go;
            },
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: 4,
            maxSize: 8
        );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        coll = collision.GetComponent<ScrollingElement>();
        if (coll == null)
        {
            return;
        }
        if (coll.canExplose == false)
        {
            return;
        }
        if (carcasse != null) {
            if (!isExploded && coll.TryClaimExplosion())
            {
                isExploded = true;
                Explosion();
            } 
        }
    }

    public bool TryClaimExplosion()
    {
        if (isExploded) return false;
        isExploded = true;
        return true;
    }

    private void Explosion()
    {
        if (_crashSound != null)
            AudioSource.PlayClipAtPoint(_crashSound, transform.position);
        if (FXExplosion != null)
        {
            if (s_fxPool == null) InitFxPool(FXExplosion);
            GameObject fx = s_fxPool.Get();
            fx.transform.position = transform.position;
            fx.GetComponent<ExplosionCircles>()?.Jouer(transform.position);
        }
        if (carcasse != null)
        {
            tempSprite = _sprite.sprite;
            _sprite.sprite = carcasse;
            inCarcasse = true;
            speed = speed + tempbaseSpeed;
        }
        coll.WhenExplosion();
    }

    public void WhenExplosion()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }

}