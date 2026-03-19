using UnityEngine;

public class ScrollingElement : MonoBehaviour
{
    private float speed = 0;
    public float baseSpeed = 5;
    private bool isMoving = true;

    public void UpdateSpeed(float addToNewSpeed)
    {
        speed = Mathf.Clamp(baseSpeed+addToNewSpeed, 0, 30+baseSpeed);
    }

    void Update()
    {
        if (!isMoving)
        {
            return;
        }
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < -20)
        {
            gameObject.SetActive(false);
        }
    }
    private void Start()
    {
        speed = baseSpeed;
    }

    public void StopMoving()
    {
        isMoving = false;
    }

}