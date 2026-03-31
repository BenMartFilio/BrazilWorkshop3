using UnityEngine;

public class ScrollingElement : MonoBehaviour
{
    private float speed = 0f;
    public float baseSpeed = 5f;
    private bool isMoving = true;
    private SpriteRenderer _sprite;

    public void UpdateSpeed(float addToNewSpeed)
    {
        if (inCarcasse)
        {
            speed = Mathf.Clamp(tempbaseSpeed + addToNewSpeed, 0, 30 + tempbaseSpeed);
        }
        else
        {
            speed = Mathf.Clamp(baseSpeed + addToNewSpeed, 0, 30 + baseSpeed);
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
            transform.Translate(Vector3.down * speed * Time.fixedDeltaTime);

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
            _rb.MovePosition(_rb.position + Vector2.down * speed * Time.fixedDeltaTime);

            if (_rb.position.y < -20)
                gameObject.SetActive(false);
        }
    }
    private void Start()
    {
        speed = baseSpeed;
        tempbaseSpeed = 5 - baseSpeed;
    }

    private Rigidbody2D _rb;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
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
        if (tempSprite != null)
        {
            _sprite.sprite = tempSprite;
        }
    }







    // ---------------- EXPLOSION PARTIE -------------------

    private ScrollingElement coll;
    public bool isExploded = false;
    public GameObject FXExplosion;
    public Sprite carcasse;
    private bool inCarcasse = false;
    private float tempbaseSpeed;
    private Sprite tempSprite;
    public bool canExplose = true;

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
        if (FXExplosion != null)
        {
            GameObject fx = Instantiate(FXExplosion, transform.position, Quaternion.identity);
            fx.GetComponent<ExplosionCircles>()?.Jouer(transform.position); ;
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