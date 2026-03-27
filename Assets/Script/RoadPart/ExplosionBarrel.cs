using UnityEngine;

public class ExplosionBarrel : MonoBehaviour
{
    public GameObject fxPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerMovement>() == null)
        {
            return;
        }

        Instantiate(fxPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
