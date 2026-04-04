using UnityEngine;

public class ExplosionBarrel : MonoBehaviour
{
    [SerializeField] private AudioClip _explosionSound;
    public GameObject fxPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerMovement>() == null)
        {
            return;
        }
        if (_explosionSound != null)
            AudioSource.PlayClipAtPoint(_explosionSound, transform.position);
        Debug.Log("Explosion");
        GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
        fx.GetComponent<ExplosionCircles>()?.Jouer(transform.position);

        gameObject.SetActive(false);
    }
}
