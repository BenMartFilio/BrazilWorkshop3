using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'onglet Inventaire du menu principal :
/// - Affiche la quantité de chaque objet depuis SO_PlayerDatas
/// - Au clic : une icône proxy se place sur l'originale, masque l'originale,
///   puis glisse et grossit jusqu'au centre. Fermeture : téléportation instantanée.
/// </summary>
public class InventaireMenuUI : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────
    private const float DUREE_ANIM = 0.3f;
    private const float ALPHA_OVERLAY = 0.6f;
    private const float FACTEUR_ZOOM = 3f;

    // ── Données ───────────────────────────────────────────────────────────────
    [Header("Données")]
    [SerializeField] private SO_PlayerDatas donneesJoueur;
    [SerializeField] private SO_InventaireObjets catalogue;

    // ── Objets sur l'étagère ──────────────────────────────────────────────────
    [Header("Objets sur l'étagère")]
    [SerializeField] private List<ObjetEtagere> objetsEtagere;

    // ── Overlay sombre ────────────────────────────────────────────────────────
    [Header("Overlay")]
    [Tooltip("Image noire sous FondNoir — couvre le fond.")]
    [SerializeField] private Image fondSombre;
    [Tooltip("Bouton invisible plein écran pour capter le clic de fermeture.")]
    [SerializeField] private Button boutonFermeture;

    // ── ObjectFocus ───────────────────────────────────────────────────────────
    [Header("Object Focus")]
    [Tooltip("GameObject désactivé par défaut — s'active à l'ouverture du détail.")]
    [SerializeField] private GameObject objectFocus;
    [Tooltip("Image enfant d'ObjectFocus qui sera animée (PAS enfant de l'icône originale).")]
    [SerializeField] private Image iconeProxy;
    [Tooltip("TextMeshProUGUI de description — enfant direct d'ObjectFocus, PAS de iconeProxy.")]
    [SerializeField] private TMP_Text texteDescription;

    // ── Canvas ────────────────────────────────────────────────────────────────
    [Header("Canvas")]
    [Tooltip("Canvas racine — nécessaire pour la conversion des positions.")]
    [SerializeField] private Canvas canvasRacine;

    // ── État interne ──────────────────────────────────────────────────────────
    private Coroutine _coroutineAnim;
    private bool _detailOuvert = false;
    private Image _iconeOriginale = null;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (objectFocus != null)
            objectFocus.SetActive(false);

        if (fondSombre != null)
        {
            Color c = fondSombre.color;
            c.a = 0f;
            fondSombre.color = c;
            fondSombre.gameObject.SetActive(false);
        }

        if (boutonFermeture != null)
        {
            boutonFermeture.onClick.AddListener(FermerDetail);
            boutonFermeture.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        RafraichirQuantites();
    }

    // ── Quantités ─────────────────────────────────────────────────────────────

    /// <summary>Met à jour les labels de quantité depuis SO_PlayerDatas.</summary>
    public void RafraichirQuantites()
    {
        if (donneesJoueur == null || catalogue == null) return;

        List<InventoryEntry> inventaire = donneesJoueur.ObtenirInventairePlat();

        foreach (ObjetEtagere objet in objetsEtagere)
        {
            if (objet.labelQuantite == null) continue;

            int quantite = 0;
            foreach (InventoryEntry entree in inventaire)
            {
                if (entree.objectName == objet.identifiant)
                {
                    quantite = entree.quantity;
                    break;
                }
            }

            objet.labelQuantite.text = quantite.ToString();
        }
    }

    // ── Clic sur un objet ─────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par le Button de chaque ObjectXxx sur l'étagère.
    /// iconeSurEtagere : Image du GameObject "Icone" enfant de l'objet.
    /// identifiant : clé dans SO_InventaireObjets.
    /// </summary>
    public void OnObjetClique(Image iconeSurEtagere, string identifiant)
    {
        if (_detailOuvert) return;

        DefinitionObjetSpecial def = catalogue.ObtenirDefinition(identifiant);
        if (def == null) return;

        _iconeOriginale = iconeSurEtagere;
        OuvrirDetail(def);
    }

    // ── Ouverture ─────────────────────────────────────────────────────────────

    private void OuvrirDetail(DefinitionObjetSpecial def)
    {
        _detailOuvert = true;

        // Description
        if (texteDescription != null)
            texteDescription.text = string.IsNullOrEmpty(def.description)
                ? def.nomAffichage
                : def.description;

        // Sprite sur le proxy
        if (iconeProxy != null)
            iconeProxy.sprite = def.sprite;

        // Place le proxy sur l'originale et masque l'originale
        PlacerProxySurOriginale();

        // Active l'overlay et ObjectFocus
        if (fondSombre != null)
            fondSombre.gameObject.SetActive(true);

        if (boutonFermeture != null)
            boutonFermeture.gameObject.SetActive(true);

        if (objectFocus != null)
            objectFocus.SetActive(true);

        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);
        _coroutineAnim = StartCoroutine(AnimerOuverture());
    }

    private void PlacerProxySurOriginale()
    {
        if (_iconeOriginale == null || iconeProxy == null) return;

        RectTransform rtOriginale = _iconeOriginale.rectTransform;
        RectTransform rtProxy = iconeProxy.rectTransform;
        RectTransform rtParent = rtProxy.parent as RectTransform;

        // Ajuste les ancres pour un positionnement absolu
        rtProxy.anchorMin = new Vector2(0.5f, 0.5f);
        rtProxy.anchorMax = new Vector2(0.5f, 0.5f);
        rtProxy.pivot = new Vector2(0.5f, 0.5f);
        rtProxy.sizeDelta = rtOriginale.rect.size;
        rtProxy.localScale = Vector3.one;

        // Convertit le centre monde de l'originale en position canvas du parent du proxy
        Vector3[] coins = new Vector3[4];
        rtOriginale.GetWorldCorners(coins);
        Vector3 centreMondeOriginale = (coins[0] + coins[2]) * 0.5f;

        Camera cam = canvasRacine.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvasRacine.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, centreMondeOriginale);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rtParent,
            screenPoint,
            cam,
            out Vector2 posLocale
        );

        rtProxy.anchoredPosition = posLocale;

        // Cache l'originale
        _iconeOriginale.enabled = false;
    }

    // ── Fermeture (instantanée) ───────────────────────────────────────────────

    /// <summary>Ferme le détail immédiatement — pas d'animation retour.</summary>
    public void FermerDetail()
    {
        if (!_detailOuvert) return;
        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);

        // Restaure l'icône originale
        if (_iconeOriginale != null)
            _iconeOriginale.enabled = true;

        // Reset proxy
        if (iconeProxy != null)
        {
            RectTransform rtProxy = iconeProxy.rectTransform;
            rtProxy.anchoredPosition = Vector2.zero;
            rtProxy.sizeDelta = Vector2.zero;
        }

        // Désactive tout
        if (objectFocus != null)
            objectFocus.SetActive(false);

        if (fondSombre != null)
        {
            Color c = fondSombre.color;
            c.a = 0f;
            fondSombre.color = c;
            fondSombre.gameObject.SetActive(false);
        }

        if (boutonFermeture != null)
            boutonFermeture.gameObject.SetActive(false);

        _iconeOriginale = null;
        _detailOuvert = false;
    }

    // ── Animation ouverture ───────────────────────────────────────────────────

    private IEnumerator AnimerOuverture()
    {
        if (iconeProxy == null) yield break;

        RectTransform rtProxy = iconeProxy.rectTransform;

        Vector2 posDepart = rtProxy.anchoredPosition;
        Vector2 tailleDepart = rtProxy.sizeDelta;
        Vector2 tailleCible = tailleDepart * FACTEUR_ZOOM;
        // Cible = centre du parent (0,0 si l'ancre est bien centrée sur l'écran)
        Vector2 posCible = Vector2.zero;

        float elapsed = 0f;

        while (elapsed < DUREE_ANIM)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / DUREE_ANIM);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

            rtProxy.anchoredPosition = Vector2.Lerp(posDepart, posCible, eased);
            rtProxy.sizeDelta = Vector2.Lerp(tailleDepart, tailleCible, eased);

            if (fondSombre != null)
            {
                Color c = fondSombre.color;
                c.a = Mathf.Lerp(0f, ALPHA_OVERLAY, eased);
                fondSombre.color = c;
            }

            yield return null;
        }

        rtProxy.anchoredPosition = posCible;
        rtProxy.sizeDelta = tailleCible;

        if (fondSombre != null)
        {
            Color c = fondSombre.color;
            c.a = ALPHA_OVERLAY;
            fondSombre.color = c;
        }
    }
}

// ── Config par objet (Inspector) ──────────────────────────────────────────────

[System.Serializable]
public class ObjetEtagere
{
    [Tooltip("Identifiant dans SO_InventaireObjets.")]
    public string identifiant;

    [Tooltip("TextMeshProUGUI 'Number' sous Etiquette — affiche la quantité.")]
    public TMP_Text labelQuantite;
}
