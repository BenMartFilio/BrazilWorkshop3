using System.Collections;
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


    public void Ralentissement(float duration, ScrollingElement barrage)
    {
        StartCoroutine(Slower1(duration, barrage));
    }

    private IEnumerator Slower(float duration, ScrollingElement barrage)
    {
        float elapsedTime = 0f;
        float startSpeed = speed;
        barrage.StartMoving();

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 5); // puissance 4 = freinage fort

            speed = Mathf.Lerp(startSpeed, 0f, easedT);
            barrage.SetSpeed(speed);

            elapsedTime += Time.deltaTime;
            yield return null;

        }
        speed = 0f;
    }
    private IEnumerator Slower1(float duration, ScrollingElement barrage)
    {
        float elapsedTime = 0f;
        float startSpeed = speed;
        barrage.StartMoving();

        while (elapsedTime < 0.2f)
        {
            float t = elapsedTime / 0.2f;
            float easedT = 1f - Mathf.Pow(1f - t, 5);

            speed = Mathf.Lerp(startSpeed, 5f, easedT);
            barrage.SetSpeed(speed);

            elapsedTime += Time.deltaTime;
            yield return null;

        }
        speed = 5f;
        StartCoroutine(Slower(duration-0.2f, barrage));
    }
}
