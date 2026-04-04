// ExplosionBarrel.cs — remplace GetComponent par TryGetComponent caché
using UnityEngine;

public class ExplosionBarrel : MonoBehaviour
{
    [SerializeField] private AudioClip _explosionSound;
    [SerializeField] public GameObject fxPrefab;

    // Utilise le layer ou tag plutôt que GetComponent pour la détection
    // Configure le layer "Player" dans Unity et assigne-le au joueur
    private const string PLAYER_TAG = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // CompareTag est beaucoup plus rapide que GetComponent
        if (!collision.CompareTag(PLAYER_TAG)) return;

        if (_explosionSound != null)
            AudioSource.PlayClipAtPoint(_explosionSound, transform.position);

        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            fx.GetComponent<ExplosionCircles>()?.Jouer(transform.position);
        }

        gameObject.SetActive(false);
    }
}