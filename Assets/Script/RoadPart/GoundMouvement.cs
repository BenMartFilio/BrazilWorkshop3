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

    private Vector3 _repositionOffset;

    /// <summary>
    /// Multiplicateur global applique dans UpdateSpeed.
    /// Partage la meme semantique que ScrollingElement.FacteurVitesseGlobal.
    /// </summary>
    public static float FacteurVitesseGlobal = 1f;

    private void Start()
    {
    //    speed = baseSpeed;
        _repositionOffset = new Vector3(0f, width * 2f, 0f);
    }

    public void UpdateSpeed(float addToNewSpeed)
    {
        speed = Mathf.Clamp(
            (baseSpeed + addToNewSpeed) * FacteurVitesseGlobal,
            0,
            maxSpeed * FacteurVitesseGlobal);
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>Restaure la vitesse du sol depuis les données de session.</summary>
    public void RestaurerDepuisSession(float vitesse)
    {
     //   speed = vitesse;
    }

    void FixedUpdate()
    {
        if (!started)
        {
            return;
        }

        transform.Translate(Vector3.down * speed * 0.8f * Time.fixedDeltaTime);

        if (transform.position.y <= -width)
        {
            transform.position += _repositionOffset;
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
        StartCoroutine(Slower(duration, barrage));
    }

    private IEnumerator Slower(float duration, ScrollingElement barrage)
    {
        float elapsedTime = 0f;
        float startSpeed  = speed;
        barrage.StartMoving();

        while (elapsedTime < duration)
        {
            float t      = elapsedTime / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 5);
            speed = Mathf.Lerp(startSpeed, 0f, easedT);
            barrage.SetSpeed(speed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        speed = 0f;
    }
}
