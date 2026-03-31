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
        Debug.Log("Explosion");
        GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
        fx.GetComponent<ExplosionCircles>()?.Jouer(transform.position);

        gameObject.SetActive(false);
    }
}
