using UnityEngine;

/// <summary>
/// Marqueur posé sur les obstacles (YellowCircle, RedCircle, BlackCircle, OrangeCircle).
/// Référence optionnelle au prefab d'explosion à instancier lors de la collision avec le joueur.
/// </summary>
public class CollisionObstacle : MonoBehaviour
{
    [Tooltip("Prefab FX_ExplosionCircles à instancier au point de collision.")]
    [SerializeField] private GameObject prefabExplosion;
    [SerializeField] private AudioClip _crashSound;


    /// <summary>
    /// Instancie l'effet d'explosion à la position donnée et le joue.
    /// Appelé depuis PlayerMovement lors de OnTriggerEnter2D.
    /// </summary>
    public void DeclencherExplosion(Vector3 positionCollision)
    {

        if (_crashSound != null)
            AudioSource.PlayClipAtPoint(_crashSound, positionCollision);
        if (prefabExplosion == null) return;

        

        GameObject instance = Instantiate(prefabExplosion, positionCollision, Quaternion.identity);
        ExplosionCircles fx = instance.GetComponent<ExplosionCircles>();
        if (fx != null)
            fx.Jouer(positionCollision);
    }
}
