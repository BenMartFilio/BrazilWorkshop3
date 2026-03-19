using UnityEngine;

public class ScrollingElement : MonoBehaviour
{
    public float speed = 5;
    private bool isMoving = true;

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


    public void StopMoving()
    {
        isMoving = false;
    }

}