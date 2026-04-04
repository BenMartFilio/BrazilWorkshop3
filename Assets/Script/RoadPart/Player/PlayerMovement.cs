using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using ObjetsSpeciaux;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform[] m_transforms;
    [SerializeField] private InputPlayerMovement m_inputManager;
    private Rigidbody2D rb;
    private int m_index;
    private int m_moveSpeed = 1;

    [SerializeField] private AudioEventDispatcher _AudioEventDispatcher;
    [SerializeField] private AudioType _MoveAudioType;
    [SerializeField] private AudioType _coinPickup;
    [SerializeField] private AudioType _semiCrash;

    public float moveDuration = 0.1f;
    public float maxLeanAngle = 45f;

    [SerializeField] private TMP_Text _coinsText;
    private int _coinsCount = 0;

    private bool _canMoving = true;
    private Coroutine _coroutine;

    [SerializeField] private EndManager _endManager;

    [Tooltip("Reference optionnelle a EffetsObjetsSpeciaux pour les effets Gateau Chinois et Tirelire Cochon.")]
    [SerializeField] private EffetsObjetsSpeciaux _effets;

    private bool stateOfDeath = false;
    private bool IamAlreadyTouched = false;

    private Quaternion _rotate;

    private void OnEnable()
    {
        m_inputManager.OnMoveLeft += MoveToPreviousPosition;
        m_inputManager.OnMoveRight += MoveToNextPosition;
    }

    private void OnDisable()
    {
        m_inputManager.OnMoveLeft -= MoveToPreviousPosition;
        m_inputManager.OnMoveRight -= MoveToNextPosition;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _rotate = transform.rotation;
        m_index = 1;
        transform.position = m_transforms[m_index].position;
    }


    [SerializeField] private float _collisionCheckRadius = 0.4f;

    /// <summary>Retourne true si un obstacle occupe la lane à la position X cible.</summary>
    private bool IsLaneBlocked(float targetX)
    {
        Vector2 checkPos = new Vector2(targetX, transform.position.y);
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, _collisionCheckRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.TryGetComponent<CollisionObstacle>(out _))
                return true;
        }
        return false;
    }




    public void MoveToNextPosition()
    {
        if (!_canMoving)
        {
            return;
        }
        _AudioEventDispatcher.PlayAudio(_MoveAudioType);
        m_index += m_moveSpeed;
        m_index = Mathf.Clamp(m_index, 0, m_transforms.Length - 1);
        if (IsLaneBlocked(m_transforms[m_index].position.x))
        {
            m_index -= m_moveSpeed;
            OnSemiCollision();
        }

        UpdatePosition();
    }
    public void MoveToPreviousPosition()
    {
        if (!_canMoving)
        {
            return;
        }
        _AudioEventDispatcher.PlayAudio(_MoveAudioType);
        m_index -= m_moveSpeed;
        m_index = Mathf.Clamp(m_index, 0, m_transforms.Length - 1);
        if (IsLaneBlocked(m_transforms[m_index].position.x))
        {
            m_index += m_moveSpeed;
            OnSemiCollision();
        }

        UpdatePosition();
    }
    public void MoveToDirection(int direction) //direction -1 ou 1
    {
        _AudioEventDispatcher.PlayAudio(_MoveAudioType);
        m_index += m_moveSpeed * direction;
        m_index = Mathf.Clamp(m_index, 0, m_transforms.Length - 1);
        UpdatePosition();
    }
    private void UpdatePosition()
    {
        Vector3 newPosition = m_transforms[m_index].position;
        MoveToX(newPosition.x);
        Quaternion actualRotation = transform.rotation;
        transform.rotation = actualRotation;
    }

    public void MoveToX(float targetX)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(SmoothMove(targetX));
    }

    IEnumerator SmoothMove(float targetX)
    {
        float startX = transform.position.x;
        float time = 0f;

        float direction = Mathf.Sign(targetX - startX); // droite = 1, gauche = 1
        stateOfDeath = true;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / moveDuration);

            // même easing pour synchroniser mouvement + rotation
            float easedT = EaseInOut(t);

            // --- POSITION ---
            float newX = Mathf.Lerp(startX, targetX, easedT);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            // --- ROTATION ---
            float leanFactor = Mathf.Sin(easedT * Mathf.PI);
            float angle = leanFactor * maxLeanAngle * direction;

            transform.rotation = Quaternion.Euler(0, 0, -angle);

            yield return null;
        }

        // reset propre
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.rotation = Quaternion.identity;
        stateOfDeath = false;
    }

    float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>Sauvegarde la position et les pièces dans les données de session.</summary>
    public void SauvegarderDansSession(DonnéesSession données)
    {
        données.indexLane = m_index;
        données.pièces    = _coinsCount;
    }

    /// <summary>Restaure la position et les pièces depuis les données de session.</summary>
    public void RestaurerDepuisSession(int indexLane, int pièces)
    {
        m_index     = Mathf.Clamp(indexLane, 0, m_transforms.Length - 1);
        _coinsCount = pièces;
        _coinsText.text = _coinsCount.ToString();
        transform.position = new Vector3(
            m_transforms[m_index].position.x,
            transform.position.y,
            transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CoinsScript a = other.GetComponent<CoinsScript>();
        CollisionObstacle obstacle = other.GetComponent<CollisionObstacle>();

        if (obstacle != null)
        {
            // Effet Gateau Chinois : tenter d'esquiver avant le traitement normal
            if (_effets != null && _effets.TenterEsquiveGateau(obstacle, transform.position))
                return;

            obstacle.DeclencherExplosion(transform.position);

            if (IamAlreadyTouched == false)
            {
                if (stateOfDeath == true)
                {
                    StartCoroutine(Camera.main.GetComponent<ScreenShake>().Shake(0.2f, 0.15f));
                    StartCoroutine(SimpleCollision());
                }
                else
                {
                    StartCoroutine(Camera.main.GetComponent<ScreenShake>().Shake(0.2f, 0.15f));
                    Death();
                }
            }
            else
            {
                StartCoroutine(Camera.main.GetComponent<ScreenShake>().Shake(0.2f, 0.15f));
                Death();
            }
        }
        else if (a != null)
        {
            a.OnCoinRecuperation();
            if(_AudioEventDispatcher)_AudioEventDispatcher.PlayAudio(_coinPickup);
            // Effet Tirelire Cochon : appliquer le multiplicateur de pieces
            _coinsCount += _effets != null ? _effets.ObtenirMultiplicateurPieces() : 1;
            _coinsText.text = _coinsCount.ToString();
        }
    }

    private void OnSemiCollision()
    {

        if (_AudioEventDispatcher != false) _AudioEventDispatcher.PlayAudio(_semiCrash);
       
        if (IamAlreadyTouched == false)
        {

                StartCoroutine(Camera.main.GetComponent<ScreenShake>().Shake(0.2f, 0.15f));
                StartCoroutine(SimpleCollision());
        }
        else
        {
            StartCoroutine(Camera.main.GetComponent<ScreenShake>().Shake(0.2f, 0.15f));
            Death();
        }
    }

    IEnumerator SimpleCollision()
    {
        IamAlreadyTouched = true;
        yield return new WaitForSeconds(1);
        IamAlreadyTouched = false;
    }

    private void Death()
    {
        _endManager.OnDeath();
    }

    public void StopMove()
    {
        _canMoving = false;
        stateOfDeath = false;       
        IamAlreadyTouched = false;  

        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
    }


    public void StartMove()
    {
        _canMoving = true;
        transform.SetPositionAndRotation(m_transforms[m_index].position, _rotate);
    }


    public int ReturnCoins()
    {
        return _coinsCount;
    }


    public void MoveUp(float duration)
    {
        _canMoving = false;
        m_index = 1;
        UpdatePosition();
        StartCoroutine(UpperMoves(duration));
    }

    private IEnumerator UpperMoves(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            rb.MovePosition(rb.position + Vector2.up * 3f * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
