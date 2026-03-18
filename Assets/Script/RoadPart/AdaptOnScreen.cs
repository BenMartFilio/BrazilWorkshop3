using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class AdaptOnScreen : MonoBehaviour
{
    private Camera cam;
    private SpriteRenderer sr;

    void Awake()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (sr == null || sr.sprite == null || cam == null) return;

        Stretch();
    }

    void Stretch()
    {
        // Taille visible de la caméra (orthographique)
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        // Scale indépendant (étirement libre)
        float scaleX = worldWidth / spriteSize.x;
        float scaleY = worldHeight / spriteSize.y;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);

       
    }
}