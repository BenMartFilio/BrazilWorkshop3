using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'onglet Inventaire du menu principal :
/// - Affiche la quantité de chaque objet depuis SO_PlayerDatas
/// - Au clic : agrandit l'objet au centre, assombrit le fond, affiche la description
/// </summary>
public class InventaireMenuUI : MonoBehaviour
{
    // ── Constantes animation ──────────────────────────────────────────────────
    private const float DUREE_ANIM = 0.25f;
    private const float SCALE_AGRANDISSEMENT = 3.0f;
    private const float ALPHA_OVERLAY = 0.6f;

    // ── Données ───────────────────────────────────────────────────────────────
    [Header("Données")]
    [SerializeField] private SO_PlayerDatas donneesJoueur;
    [SerializeField] private SO_InventaireObjets catalogue;

    // ── Mapping objets scène ──────────────────────────────────────────────────
    [Header("Objets sur l'étagère")]
    [SerializeField] private List<ObjetEtagere> objetsEtagere;

    // ── UI overlay ────────────────────────────────────────────────────────────
    [Header("UI Overlay")]
    [Tooltip("Image noire semi-transparente qui couvre le fond (FondNoir dans la hiérarchie).")]
    [SerializeField] private Image fondSombre;
    [Tooltip("Panel centré contenant l'icone agrandie et la description.")]
    [SerializeField] private GameObject panelDetail;
    [SerializeField] private Image iconDetail;
    [SerializeField] private TMP_Text texteDescription;
    [Tooltip("Bouton invisible couvrant tout l'écran pour fermer le détail.")]
    [SerializeField] private Button boutonFermeture;

    // ── État ──────────────────────────────────────────────────────────────────
    private Coroutine _coroutineAnim;
    private bool _detailOuvert = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (panelDetail != null)
            panelDetail.SetActive(false);

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

    /// <summary>Met à jour les labels de quantité de tous les objets depuis SO_PlayerDatas.</summary>
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
    /// Appelé par le Button de chaque objet sur l'étagère.
    /// Passe l'identifiant de l'objet pour récupérer sa définition.
    /// </summary>
    public void OnObjetClique(string identifiant)
    {
        if (_detailOuvert) return;

        DefinitionObjetSpecial def = catalogue.ObtenirDefinition(identifiant);
        if (def == null) return;

        int quantite = 0;
        foreach (InventoryEntry entree in donneesJoueur.ObtenirInventairePlat())
        {
            if (entree.objectName == identifiant)
            {
                quantite = entree.quantity;
                break;
            }
        }

        OuvrirDetail(def, quantite);
    }

    // ── Ouverture / Fermeture détail ──────────────────────────────────────────

    private void OuvrirDetail(DefinitionObjetSpecial def, int quantite)
    {
        _detailOuvert = true;

        if (iconDetail != null)
            iconDetail.sprite = def.sprite;

        if (texteDescription != null)
            texteDescription.text = string.IsNullOrEmpty(def.description)
                ? def.nomAffichage
                : def.description;

        if (fondSombre != null)
            fondSombre.gameObject.SetActive(true);

        if (panelDetail != null)
            panelDetail.SetActive(true);

        if (boutonFermeture != null)
            boutonFermeture.gameObject.SetActive(true);

        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);
        _coroutineAnim = StartCoroutine(AnimerOuverture());
    }

    /// <summary>Ferme le panneau de détail.</summary>
    public void FermerDetail()
    {
        if (!_detailOuvert) return;
        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);
        _coroutineAnim = StartCoroutine(AnimerFermeture());
    }

    // ── Coroutines d'animation ────────────────────────────────────────────────

    private IEnumerator AnimerOuverture()
    {
        float elapsed = 0f;
        RectTransform rtPanel = panelDetail != null ? panelDetail.GetComponent<RectTransform>() : null;

        while (elapsed < DUREE_ANIM)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / DUREE_ANIM);
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

            // Fond sombre
            if (fondSombre != null)
            {
                Color c = fondSombre.color;
                c.a = Mathf.Lerp(0f, ALPHA_OVERLAY, easedT);
                fondSombre.color = c;
            }

            // Panel : scale 0.6 → 1
            if (rtPanel != null)
            {
                float s = Mathf.Lerp(0.6f, 1f, easedT);
                rtPanel.localScale = new Vector3(s, s, 1f);
            }

            yield return null;
        }

        if (fondSombre != null)
        {
            Color c = fondSombre.color;
            c.a = ALPHA_OVERLAY;
            fondSombre.color = c;
        }
        if (rtPanel != null)
            rtPanel.localScale = Vector3.one;
    }

    private IEnumerator AnimerFermeture()
    {
        float elapsed = 0f;
        RectTransform rtPanel = panelDetail != null ? panelDetail.GetComponent<RectTransform>() : null;

        while (elapsed < DUREE_ANIM)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / DUREE_ANIM);
            float easedT = t * t; // ease-in

            if (fondSombre != null)
            {
                Color c = fondSombre.color;
                c.a = Mathf.Lerp(ALPHA_OVERLAY, 0f, easedT);
                fondSombre.color = c;
            }

            if (rtPanel != null)
            {
                float s = Mathf.Lerp(1f, 0.6f, easedT);
                rtPanel.localScale = new Vector3(s, s, 1f);
            }

            yield return null;
        }

        if (panelDetail != null)
            panelDetail.SetActive(false);

        if (fondSombre != null)
            fondSombre.gameObject.SetActive(false);

        if (boutonFermeture != null)
            boutonFermeture.gameObject.SetActive(false);

        _detailOuvert = false;
    }
}

// ── Classe de configuration par objet ─────────────────────────────────────────

[System.Serializable]
public class ObjetEtagere
{
    [Tooltip("Identifiant correspondant à DefinitionObjetSpecial.identifiant dans le catalogue.")]
    public string identifiant;

    [Tooltip("Label TMP qui affiche la quantité (le TextMeshProUGUI 'Number' sous Etiquette).")]
    public TMP_Text labelQuantite;
}
