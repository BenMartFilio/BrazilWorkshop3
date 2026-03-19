using UnityEngine;

public class GoundMouvement : MonoBehaviour
{
    public float speed = 5f;
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
        speed = Mathf.Clamp(speed + 1, 0, maxSpeed);
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
