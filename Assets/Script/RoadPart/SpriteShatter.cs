using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brise une Image UI en une grille de morceaux qui tombent jusqu'en bas de l'écran,
/// disparaissent, puis restaurent automatiquement le sprite original.
/// Appelle Shatter() pour déclencher, ResetShatter() pour forcer le reset à tout moment.
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteShatter : MonoBehaviour
{
    [Header("Grille")]
    [Tooltip("Nombre de colonnes de la grille de découpe.")]
    public int columns = 10;

    [Tooltip("Nombre de lignes de la grille de découpe.")]
    public int rows = 10;

    [Header("Physique simulée")]
    [Tooltip("Force d'explosion initiale (pixels/sec).")]
    public float explosionForce = 400f;

    [Tooltip("Gravité simulée (pixels/sec²).")]
    public float gravity = 1800f;

    [Header("Fondu")]
    [Tooltip("Durée du fondu final (secondes), déclenché quand le morceau sort de l'écran.")]
    public float fadeDuration = 0.3f;

    // ── Références ────────────────────────────────────────────────────────────

    private Image _image;
    private RectTransform _rectTransform;
    private Canvas _parentCanvas;
    private bool _shattered = false;
    private readonly List<GameObject> _pieces = new();
    private int _piecesAlive = 0;
    private Coroutine _shatterCoroutine;

    // ── Constantes ────────────────────────────────────────────────────────────

    private const float RotationSpeedMin = -360f;
    private const float RotationSpeedMax = 360f;

    // ─────────────────────────────────────────────────────────────────────────


    [SerializeField] private Aspiration aspi;

    private readonly List<Coroutine> _pieceCoroutines = new List<Coroutine>();

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>Brise l'image en morceaux et les fait tomber jusqu'en bas de l'écran.</summary>
    public void Shatter()
    {
        if (_shattered)
        {
            ResetShatter();
        }
        _shattered = true;

        Sprite sprite = _image.sprite;
        if (sprite == null)
        {
            Debug.LogWarning("[UIShatter] Aucun sprite assigné sur l'Image.");
            return;
        }

        Texture2D texture = sprite.texture;
        Rect spriteRect = sprite.rect;
        Vector2 rectSize = _rectTransform.rect.size;

        float pieceW = rectSize.x / columns;
        float pieceH = rectSize.y / rows;

        // Normalisation UV de la région du sprite dans sa texture
        float uvX = spriteRect.x / texture.width;
        float uvY = spriteRect.y / texture.height;
        float uvPieceW = (spriteRect.width / texture.width) / columns;
        float uvPieceH = (spriteRect.height / texture.height) / rows;

        // Bas de l'écran en coordonnées monde
        float screenBottomY = GetScreenBottomWorld();

        // Pivot bas-gauche du RectTransform en espace local
        Vector2 origin = new Vector2(
            -rectSize.x * _rectTransform.pivot.x,
            -rectSize.y * _rectTransform.pivot.y
        );

        Transform container = transform.parent;
        int sortingOrder = (_parentCanvas != null ? _parentCanvas.sortingOrder : 0) + 1;

        _piecesAlive = columns * rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                // ── RectTransform ────────────────────────────────────────────
                GameObject piece = new GameObject($"Shard_{r}_{c}");
                RectTransform pieceRT = piece.AddComponent<RectTransform>();
                piece.transform.SetParent(container, worldPositionStays: false);
                pieceRT.sizeDelta = new Vector2(pieceW, pieceH);
                pieceRT.pivot = new Vector2(0.5f, 0.5f);

                float localX = origin.x + (c + 0.5f) * pieceW;
                float localY = origin.y + (r + 0.5f) * pieceH;
                pieceRT.position = _rectTransform.TransformPoint(new Vector3(localX, localY, 0f));

                // ── RawImage UV ──────────────────────────────────────────────
                RawImage rawImg = piece.AddComponent<RawImage>();
                rawImg.texture = texture;
                rawImg.color = _image.color;
                rawImg.uvRect = new Rect(
                    uvX + c * uvPieceW,
                    uvY + r * uvPieceH,
                    uvPieceW,
                    uvPieceH
                );

                // ── Canvas override ──────────────────────────────────────────
                Canvas pieceCanvas = piece.AddComponent<Canvas>();
                pieceCanvas.overrideSorting = true;
                pieceCanvas.sortingOrder = sortingOrder;
                piece.AddComponent<GraphicRaycaster>();

                // ── Vélocité initiale ────────────────────────────────────────
                Vector2 fromCenter = new Vector2(localX, localY).normalized;
                if (fromCenter == Vector2.zero)
                    fromCenter = Random.insideUnitCircle.normalized;

                Vector2 velocity = fromCenter * explosionForce * Random.Range(0.5f, 1.5f)
                                 + Vector2.down * Random.Range(200f, 600f);
                float angular = Random.Range(RotationSpeedMin, RotationSpeedMax);

                _pieces.Add(piece);
                _pieceCoroutines.Add(StartCoroutine(SimulatePiece(pieceRT, rawImg, velocity, angular, screenBottomY)));
            }
        }

        _image.enabled = false;
        return;
    }

    /// <summary>
    /// Déplace le morceau jusqu'à ce qu'il sorte en bas de l'écran,
    /// applique un fondu, le détruit, puis déclenche le reset si c'est le dernier.
    /// </summary>
    private IEnumerator SimulatePiece(RectTransform pieceRT, RawImage rawImg,
                                       Vector2 velocity, float angularVelocity,
                                       float screenBottomY)
    {
        // ── Chute jusqu'à la limite basse ────────────────────────────────────
        while (pieceRT != null && pieceRT.position.y > screenBottomY)
        {
            velocity += Vector2.down * gravity * Time.deltaTime;
            pieceRT.position += (Vector3)(velocity * Time.deltaTime);
            pieceRT.Rotate(0f, 0f, angularVelocity * Time.deltaTime);
            yield return null;
        }

        if (pieceRT == null) yield break;

        // ── Fondu rapide en dessous du bord ───────────────────────────────────
        float elapsed = 0f;
        Color c = rawImg.color;

        while (elapsed < fadeDuration && rawImg != null)
        {
            velocity += Vector2.down * gravity * Time.deltaTime;
            pieceRT.position += (Vector3)(velocity * Time.deltaTime);

            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            rawImg.color = c;
            yield return null;
        }

        if (rawImg != null)
            Destroy(rawImg.gameObject);

        // ── Dernier morceau → reset automatique ───────────────────────────────
        _piecesAlive--;
        if (_piecesAlive <= 0)
        {
           // RestoreSprite();
            aspi.OnDestroyingUI();
        }
    }

    /// <summary>
    /// Reset immédiat : stoppe toutes les coroutines, détruit les morceaux en vol,
    /// et remet le sprite à la normale. Peut être appelé à tout moment.
    /// </summary>
    public void ResetShatter()
    {
        foreach (Coroutine c in _pieceCoroutines)
            if (c != null) StopCoroutine(c);
        _pieceCoroutines.Clear();

        foreach (GameObject p in _pieces)
            if (p != null) Destroy(p);

        _pieces.Clear();
        _piecesAlive = 0;

        RestoreSprite();
    }

    /// <summary>Remet l'image originale visible et réinitialise l'état interne.</summary>
    private void RestoreSprite()
    {
        _pieces.Clear();
        _piecesAlive = 0;
        _shattered = false;

        if (_image != null)
            _image.enabled = true;
    }

    /// <summary>Retourne la coordonnée Y monde correspondant au bas de l'écran.</summary>
    private float GetScreenBottomWorld()
    {
        if (_parentCanvas == null) return -Screen.height;

        // Canvas en Screen Space – Overlay : coordonnées = pixels écran
        if (_parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return -Screen.height * 0.5f - 100f;

        // Canvas en Screen Space – Camera
        if (_parentCanvas.worldCamera != null)
        {
            Ray ray = _parentCanvas.worldCamera.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
            return ray.origin.y - 100f;
        }

        return -Screen.height;
    }
}
