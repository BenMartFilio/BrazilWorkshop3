using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôleur d'un slot d'objet spécial dans le panneau d'inventaire.
/// Configuré via <see cref="Initialiser"/> lors de la création dynamique du slot.
/// Supporte un anneau radial de timer superposé sur l'image de l'objet.
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    private const float ALPHA_GRISE  = 0.40f;
    private const float TAILLE_TEXTE = 38f;
    private const float EPAISSEUR    = 8f;   // épaisseur de l'anneau timer en pixels

    [SerializeField] private Image            imageFond;
    [SerializeField] private Image            imageSprite;
    [SerializeField] private TextMeshProUGUI  texteQuantite;
    [SerializeField] private Button           bouton;

    // ── Timer ring ────────────────────────────────────────────────────────────
    private GameObject _timerRoot;
    private Image      _timerArc;
    private float      _timerDuree;
    private float      _timerRestant;
    private bool       _timerActif;
    private Sprite     _spriteCircle;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (texteQuantite != null)
        {
            texteQuantite.fontSize     = TAILLE_TEXTE;
            texteQuantite.fontWeight   = FontWeight.Bold;
            texteQuantite.outlineWidth = 0.2f;
            texteQuantite.outlineColor = new Color32(0, 0, 0, 255);
        }
    }

    private void Update()
    {
        if (!_timerActif) return;

        _timerRestant = Mathf.Max(0f, _timerRestant - Time.deltaTime);

        if (_timerArc != null)
            _timerArc.fillAmount = _timerDuree > 0f ? _timerRestant / _timerDuree : 0f;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Configure le slot avec les données de l'objet et enregistre le callback de clic.</summary>
    public void Initialiser(DefinitionObjetSpecial definition, int quantite,
                            System.Action<string> onClic, Sprite spriteCircle = null)
    {
        if (definition == null)
        {
            Debug.LogError("[ItemSlotUI] Définition nulle passée à Initialiser.");
            return;
        }

        _spriteCircle      = spriteCircle;
        imageSprite.sprite = definition.sprite;
        texteQuantite.text = "x" + quantite;

        bouton.onClick.RemoveAllListeners();

        if (!definition.estPassif)
            bouton.onClick.AddListener(() => onClic(definition.identifiant));

        AppliquerEtatQuantite(quantite, definition.estPassif);
    }

    /// <summary>Met à jour uniquement le label de quantité et l'état interactif du bouton.</summary>
    public void MettreAJourQuantite(int quantite, bool estPassif = false)
    {
        texteQuantite.text = "x" + quantite;
        AppliquerEtatQuantite(quantite, estPassif);
    }

    // ── Timer ring ────────────────────────────────────────────────────────────

    /// <summary>
    /// True quand la quantité est à 0 (bouton désactivé).
    /// Utilisé par <see cref="EffetsDureeUI"/> pour savoir si le slot doit être supprimé
    /// à la fin du timer.
    /// </summary>
    public bool EstEpuise => bouton != null && !bouton.interactable;

    /// <summary>
    /// Superpose un anneau radial de durée sur l'image de l'objet.
    /// Appelé par <see cref="EffetsDureeUI"/> quand l'effet démarre.
    /// </summary>
    public void DemarrerTimer(Color couleur, float duree)
    {
        ArreterTimer(supprimerSlot: false);

        RectTransform rtSprite = imageSprite.rectTransform;

        _timerRoot = new GameObject("TimerRing");
        _timerRoot.transform.SetParent(rtSprite, false);

        RectTransform rtRoot = _timerRoot.AddComponent<RectTransform>();
        rtRoot.anchorMin     = Vector2.zero;
        rtRoot.anchorMax     = Vector2.one;
        rtRoot.sizeDelta     = Vector2.zero;
        rtRoot.offsetMin     = Vector2.zero;
        rtRoot.offsetMax     = Vector2.zero;

        // Fond sombre semi-transparent
        ConstruireImage(_timerRoot.transform, "Fond", _spriteCircle,
            new Color(0f, 0f, 0f, 0.55f), Image.Type.Simple, Vector2.zero, Vector2.zero);

        // Arc coloré radial
        GameObject arcGo      = new GameObject("Arc");
        arcGo.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtArc   = arcGo.AddComponent<RectTransform>();
        EtirerFull(rtArc, Vector2.zero, Vector2.zero);
        _timerArc             = arcGo.AddComponent<Image>();
        _timerArc.sprite      = _spriteCircle;
        _timerArc.color       = couleur;
        _timerArc.type        = Image.Type.Filled;
        _timerArc.fillMethod  = Image.FillMethod.Radial360;
        _timerArc.fillOrigin  = (int)Image.Origin360.Top;
        _timerArc.fillClockwise = true;
        _timerArc.fillAmount  = 1f;
        _timerArc.raycastTarget = false;

        // Disque intérieur qui creuse l'anneau
        float inset = EPAISSEUR;
        ConstruireImage(_timerRoot.transform, "Interieur", _spriteCircle,
            new Color(0f, 0f, 0f, 0.55f), Image.Type.Simple,
            new Vector2(inset, inset), new Vector2(-inset, -inset));

        _timerDuree   = duree;
        _timerRestant = duree;
        _timerActif   = true;
    }

    /// <summary>
    /// Stoppe et retire l'anneau timer.
    /// Si <paramref name="supprimerSlot"/> est vrai et que la quantité est 0,
    /// détruit l'ensemble du slot (dernière utilisation, effet terminé).
    /// </summary>
    public void ArreterTimer(bool supprimerSlot)
    {
        _timerActif = false;
        _timerArc   = null;

        if (_timerRoot != null)
        {
            Destroy(_timerRoot);
            _timerRoot = null;
        }

        if (supprimerSlot)
            Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AppliquerEtatQuantite(int quantite, bool estPassif = false)
    {
        bool disponible     = quantite > 0 && !estPassif;
        bouton.interactable = disponible;

        if (imageFond != null)
        {
            Color c = imageFond.color;
            c.a     = disponible ? 1f : ALPHA_GRISE;
            imageFond.color = c;
        }
    }

    private static void ConstruireImage(Transform parent, string nom, Sprite sprite,
        Color couleur, Image.Type type, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go    = new GameObject(nom);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        EtirerFull(rt, offsetMin, offsetMax);
        Image img        = go.AddComponent<Image>();
        img.sprite       = sprite;
        img.color        = couleur;
        img.type         = type;
        img.raycastTarget = false;
    }

    private static void EtirerFull(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
