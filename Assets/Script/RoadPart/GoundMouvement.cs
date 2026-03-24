using UnityEngine;

public class GoundMouvement : MonoBehaviour
{
    public float baseSpeed = 5f;
    public float speed = 0f;
    private float _generalSpeed = 0f;
    public float width;
    public bool started = true;
    [SerializeField] private int maxSpeed = 35;



    private void Start()
    {
        speed = baseSpeed;
    }

    public void UpdateSpeed(float addToNewSpeed)
    {
        speed = Mathf.Clamp(baseSpeed + addToNewSpeed, 0, 30 + baseSpeed);
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
