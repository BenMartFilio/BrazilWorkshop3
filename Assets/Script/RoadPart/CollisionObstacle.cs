// CollisionObstacle.cs — cache GetComponent dans Awake
using UnityEngine;

public class CollisionObstacle : MonoBehaviour
{
    [SerializeField] private GameObject prefabExplosion;
    [SerializeField] private AudioClip _crashSound;

    // Caché dans Awake — plus de GetComponent à chaque collision
    private ExplosionBarrel _explosionBarrel;

    private void Awake()
    {
        _explosionBarrel = GetComponent<ExplosionBarrel>();
    }

    public void DeclencherExplosion(Vector3 positionCollision)
    {
        if (_explosionBarrel != null) return;

        if (_crashSound != null)
            AudioSource.PlayClipAtPoint(_crashSound, positionCollision);

        if (prefabExplosion == null) return;

        GameObject instance = Instantiate(prefabExplosion, positionCollision, Quaternion.identity);
        ExplosionCircles fx = instance.GetComponent<ExplosionCircles>();
        fx?.Jouer(positionCollision);
    }
}