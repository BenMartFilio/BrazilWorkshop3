using System.Collections;
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
    // ── Mise en page ──────────────────────────────────────────────────────────
    private const float ALPHA_GRISE      = 0.40f;
    private const float TAILLE_TEXTE     = 38f;
    private const float EPAISSEUR        = 8f;      // épaisseur de l'anneau timer en pixels
    private const float TIMER_TEXTE_SIZE = 20f;

    // ── Urgency feedback ──────────────────────────────────────────────────────
    private const float BLINK_INTERVAL  = 0.125f;   // ~4 Hz
    private const float SEUIL_BLINK     = 0.25f;    // ratio restant < 25 % → blink arc
    private const float SEUIL_PULSE     = 0.10f;    // ratio restant < 10 % → pulse slot scale
    private const float PULSE_AMPLITUDE = 0.05f;    // ±5 %

    // ── Punch animation ───────────────────────────────────────────────────────
    private const float PUNCH_SCALE     = 1.25f;
    private const float PUNCH_DUREE_OUT = 0.08f;
    private const float PUNCH_DUREE_IN  = 0.08f;

    // ── Badge pulse ───────────────────────────────────────────────────────────
    private const float BADGE_SCALE_MAX = 1.8f;
    private const float BADGE_DUREE     = 0.20f;

    // ── Annulation flash ──────────────────────────────────────────────────────
    private const float ANNULATION_FLASH_DUREE = 0.10f;
    private const float ANNULATION_FADE_DUREE  = 0.30f;

    // ── Apparition ────────────────────────────────────────────────────────────
    private const float APPARITION_DUREE = 0.30f;

    [SerializeField] private Image            imageFond;
    [SerializeField] private Image            imageSprite;
    [SerializeField] private TextMeshProUGUI  texteQuantite;
    [SerializeField] private Button           bouton;

    // ── Timer ring ────────────────────────────────────────────────────────────
    private GameObject      _timerRoot;
    private Image           _timerArc;
    private TextMeshProUGUI _timerTexte;
    private float           _timerDuree;
    private float           _timerRestant;
    private bool            _timerActif;
    private Sprite          _spriteCircle;

    // ── Urgency state ─────────────────────────────────────────────────────────
    private Color _couleurEffet;
    private float _blinkTimer;
    private bool  _blinkEtat;   // true = couleur normale, false = blanc

    // ── Tutorial lock ─────────────────────────────────────────────────────────
    private bool _verrouillePourTutoriel = false;

    // ── Animation state ───────────────────────────────────────────────────────
    private Coroutine _coroutinePunch;
    private Coroutine _coroutineFlash;

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
        float ratio   = _timerDuree > 0f ? _timerRestant / _timerDuree : 0f;

        // ── Arc fill ──────────────────────────────────────────────────────────
        if (_timerArc != null)
            _timerArc.fillAmount = ratio;

        // ── Seconds label : visible uniquement dans la seconde moitié ─────────
        if (_timerTexte != null)
        {
            bool afficher = ratio < 0.5f;
            _timerTexte.gameObject.SetActive(afficher);
            if (afficher)
                _timerTexte.text = Mathf.CeilToInt(_timerRestant).ToString();
        }

        // ── Urgency : blink arc à ~4 Hz quand < 25 % restant ─────────────────
        if (ratio < SEUIL_BLINK && _timerArc != null)
        {
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= BLINK_INTERVAL)
            {
                _blinkTimer -= BLINK_INTERVAL;
                _blinkEtat   = !_blinkEtat;
                _timerArc.color = _blinkEtat ? _couleurEffet : Color.white;
            }

            // ── Pulse scale ±5 % quand < 10 % restant ────────────────────────
            if (ratio < SEUIL_PULSE && _coroutinePunch == null)
            {
                float pulse      = 1f + Mathf.Sin(Time.time * Mathf.PI / BLINK_INTERVAL) * PULSE_AMPLITUDE;
                transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        else if (_timerArc != null)
        {
            // Rétablir la couleur canonique hors zone d'urgence.
            _timerArc.color = _couleurEffet;
        }
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

        // Réinitialiser toute échelle résiduelle d'un cycle de pool précédent.
        transform.localScale = Vector3.one;

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
    /// à la fin du timer. Jamais vrai en mode tutoriel (le slot est verrouillé, pas épuisé).
    /// </summary>
    public bool EstEpuise => !_verrouillePourTutoriel && bouton != null && !bouton.interactable;

    /// <summary>
    /// Superpose un anneau radial de durée sur l'image de l'objet.
    /// Appelé par <see cref="EffetsDureeUI"/> quand l'effet démarre.
    /// </summary>
    public void DemarrerTimer(Color couleur, float duree)
    {
        // If a cancel-flash is running on this slot, stop it before rebuilding the ring.
        if (_coroutineFlash != null)
        {
            StopCoroutine(_coroutineFlash);
            _coroutineFlash = null;
        }

        ArreterTimer(supprimerSlot: false);

        _couleurEffet = couleur;
        _blinkTimer   = 0f;
        _blinkEtat    = true;

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
        GameObject arcGo        = new GameObject("Arc");
        arcGo.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtArc     = arcGo.AddComponent<RectTransform>();
        EtirerFull(rtArc, Vector2.zero, Vector2.zero);
        _timerArc               = arcGo.AddComponent<Image>();
        _timerArc.sprite        = _spriteCircle;
        _timerArc.color         = couleur;
        _timerArc.type          = Image.Type.Filled;
        _timerArc.fillMethod    = Image.FillMethod.Radial360;
        _timerArc.fillOrigin    = (int)Image.Origin360.Top;
        _timerArc.fillClockwise = true;
        _timerArc.fillAmount    = 1f;
        _timerArc.raycastTarget = false;

        // Disque intérieur qui creuse l'anneau
        float inset = EPAISSEUR;
        ConstruireImage(_timerRoot.transform, "Interieur", _spriteCircle,
            new Color(0f, 0f, 0f, 0.55f), Image.Type.Simple,
            new Vector2(inset, inset), new Vector2(-inset, -inset));

        // Label secondes centré (masqué jusqu'à < 50 % de durée restante)
        GameObject texteGo        = new GameObject("TimerTexte");
        texteGo.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtTexte     = texteGo.AddComponent<RectTransform>();
        EtirerFull(rtTexte, Vector2.zero, Vector2.zero);
        _timerTexte               = texteGo.AddComponent<TextMeshProUGUI>();
        _timerTexte.fontSize      = TIMER_TEXTE_SIZE;
        _timerTexte.fontWeight    = FontWeight.Bold;
        _timerTexte.color         = Color.white;
        _timerTexte.alignment     = TextAlignmentOptions.Center;
        _timerTexte.outlineWidth  = 0.25f;
        _timerTexte.outlineColor  = new Color32(0, 0, 0, 200);
        _timerTexte.raycastTarget = false;
        texteGo.SetActive(false);

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
        if (_coroutineFlash != null)
        {
            StopCoroutine(_coroutineFlash);
            _coroutineFlash = null;
        }

        _timerActif = false;
        _timerArc   = null;
        _timerTexte = null;

        // Réinitialiser toute échelle d'urgence résiduelle.
        transform.localScale = Vector3.one;

        if (_timerRoot != null)
        {
            Destroy(_timerRoot);
            _timerRoot = null;
        }

        if (supprimerSlot)
            Destroy(gameObject);
    }

    // ── Animations publiques ──────────────────────────────────────────────────

    /// <summary>
    /// Plays a flash-to-white then fade-out on the timer arc to signal that the effect
    /// was cancelled by a new one. Stops the tick so the fill stays frozen during the animation.
    /// Calls <paramref name="onComplete"/> when finished so EffetsDureeUI can clean up the slot.
    /// Safe to call even if no timer is active — calls onComplete immediately in that case.
    /// </summary>
    public void FlasherAnnulation(Color couleurOrigine, System.Action onComplete)
    {
        if (_timerArc == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Stop any previous flash so its stale onComplete never fires.
        if (_coroutineFlash != null)
            StopCoroutine(_coroutineFlash);

        _coroutineFlash = StartCoroutine(CoroutineFlashAnnulation(couleurOrigine, onComplete));
    }

    /// <summary>
    /// Animate the slot icon scale 1 → 1.25 → 1 to confirm activation.
    /// Called by InventaireObjetsUI on click.
    /// </summary>
    public void JouerPunchAnimation()
    {
        if (_coroutinePunch != null)
            StopCoroutine(_coroutinePunch);

        _coroutinePunch = StartCoroutine(CoroutinePunch());
    }

    /// <summary>
    /// Pulse le badge de quantité (scale 1.8 → 1.0) pour signaler un changement de stock.
    /// </summary>
    public void PulserBadge()
    {
        StartCoroutine(CoroutinePulseBadge());
    }

    /// <summary>
    /// Anime l'apparition du fond (alpha 0 → 1) pour les nouveaux slots créés en cours de partie.
    /// </summary>
    public void AnimerApparition()
    {
        StartCoroutine(CoroutineApparition());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator CoroutinePunch()
    {
        float elapsed = 0f;
        while (elapsed < PUNCH_DUREE_OUT)
        {
            elapsed += Time.unscaledDeltaTime;
            float s  = Mathf.Lerp(1f, PUNCH_SCALE, elapsed / PUNCH_DUREE_OUT);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < PUNCH_DUREE_IN)
        {
            elapsed += Time.unscaledDeltaTime;
            float s  = Mathf.Lerp(PUNCH_SCALE, 1f, elapsed / PUNCH_DUREE_IN);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        _coroutinePunch = null;
    }

    private IEnumerator CoroutinePulseBadge()
    {
        if (texteQuantite == null) yield break;

        float elapsed = 0f;
        while (elapsed < BADGE_DUREE)
        {
            elapsed += Time.unscaledDeltaTime;
            float s  = Mathf.Lerp(BADGE_SCALE_MAX, 1f, elapsed / BADGE_DUREE);
            texteQuantite.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        texteQuantite.transform.localScale = Vector3.one;
    }

    private IEnumerator CoroutineFlashAnnulation(Color couleurOrigine, System.Action onComplete)
    {
        if (_timerArc == null) { onComplete?.Invoke(); _coroutineFlash = null; yield break; }

        // Freeze the tick so fillAmount stays locked during the animation.
        _timerActif = false;

        // Flash arc to white.
        float elapsed = 0f;
        while (elapsed < ANNULATION_FLASH_DUREE)
        {
            if (_timerArc == null) break;
            elapsed += Time.unscaledDeltaTime;
            _timerArc.color = Color.Lerp(couleurOrigine, Color.white, Mathf.Clamp01(elapsed / ANNULATION_FLASH_DUREE));
            yield return null;
        }

        // Fade white arc to transparent.
        elapsed = 0f;
        while (elapsed < ANNULATION_FADE_DUREE)
        {
            if (_timerArc == null) break;
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / ANNULATION_FADE_DUREE));
            _timerArc.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        _coroutineFlash = null;
        onComplete?.Invoke();
    }

    private IEnumerator CoroutineApparition()
    {
        // Forcer alpha à 0 immédiatement pour garantir un fondu depuis le noir,
        // quelle que soit la valeur laissée par AppliquerEtatQuantite.
        if (imageFond != null)
        {
            Color c = imageFond.color;
            c.a     = 0f;
            imageFond.color = c;
        }

        float elapsed = 0f;
        while (elapsed < APPARITION_DUREE)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / APPARITION_DUREE);
            if (imageFond != null)
            {
                Color c = imageFond.color;
                c.a     = t;
                imageFond.color = c;
            }
            yield return null;
        }

        if (imageFond != null)
        {
            Color c = imageFond.color;
            c.a     = 1f;
            imageFond.color = c;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Greys out this slot and removes all click listeners for the tutorial.
    /// The item remains visible but cannot be used.
    /// Sets a persistent flag so AppliquerEtatQuantite never overrides the lock.
    /// </summary>
    public void VerrouillerPourTutoriel()
    {
        _verrouillePourTutoriel = true;

        bouton.onClick.RemoveAllListeners();
        bouton.interactable = false;

        if (imageFond != null)
        {
            Color c = imageFond.color;
            c.a = ALPHA_GRISE;
            imageFond.color = c;
        }

        if (imageSprite != null)
        {
            Color c = imageSprite.color;
            c.a = ALPHA_GRISE;
            imageSprite.color = c;
        }
    }

    private void AppliquerEtatQuantite(int quantite, bool estPassif = false)
    {
        // Never override a tutorial lock.
        if (_verrouillePourTutoriel) return;

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
