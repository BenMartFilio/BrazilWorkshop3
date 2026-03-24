using UnityEngine;

public class GoundMouvement : MonoBehaviour
{
    public float speed = 0f;
    private float baseSpeed = 5f;
    private float _generalSpeed = 0f;
    public float width;
    public bool started = true;
    [SerializeField] private int maxSpeed = 35;

    [SerializeField] private TimeManager _timeManager;


    private void OnEnable()
    {
        _timeManager.OnTimePassed += Acceleration;
    }

    private void OnDisable()
    {
        _timeManager.OnTimePassed -= Acceleration;
    }

    public void Acceleration()
    {
        _generalSpeed = Mathf.Clamp(_generalSpeed + 1, 0, 30);
        speed = Mathf.Clamp(baseSpeed + _generalSpeed, 0, 30 + baseSpeed);
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>Restaure la vitesse du sol depuis les données de session.</summary>
    public void RestaurerDepuisSession(float vitesse)
    {
        speed = vitesse;
    }

    void Update()
    {
        if (!started)
        {
            return;
        }

        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y <= -width)
        {
            transform.position += new Vector3(0,width * 2f, 0);
        }
    }

    public void StartMove()
    {
        started = true;
    }

    public void StopMove()
    {
        started = false;
    }
}
