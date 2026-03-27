using UnityEngine;

public class ObstacleCollisions : MonoBehaviour
{
    private ObstacleCollisions coll;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        coll = collision.GetComponent<ObstacleCollisions>();

    }
}
