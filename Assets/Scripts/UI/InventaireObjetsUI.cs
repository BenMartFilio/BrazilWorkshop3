using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ObjetsSpeciaux;

/// <summary>
/// Barre d'objets spéciaux permanente en bas de l'écran pendant la partie.
///
/// Layout attendu :
///   InventaireRoot — ancre bottom-stretch, hauteur fixe, HorizontalLayoutGroup
///   └── [ItemSlot × N] — instancié au Start(), un par objet actif non-passif
///
/// Les slots sont créés une seule fois. Quand un objet atteint quantité 0,
/// son slot est masqué (SetActive false) sans recréer la liste.
/// </summary>
public class InventaireObjetsUI : MonoBehaviour
{
    // ── Identifiants objets ───────────────────────────────────────────────────
    private const string ID_LIASSE        = "LiasseDeBillets";
    private const string ID_PASSE_PARTOUT = "FormulairePasePartout";
    private const string ID_BADGE         = "BadgeDuGouvernement";
    private const string ID_TIRELIRE      = "TirelireCochon";
    private const string ID_GATEAU        = "GateauChinois";
    private const string ID_RADAR         = "RadarObstacles";
    private const string ID_ASPIRATEUR    = "Aspirateur";
    private const string ID_MONTRE        = "MontreAGousset";

    /// <summary>Objets gérés par d'autres systèmes, jamais affichés dans la barre.</summary>
    private static readonly HashSet<string> IDS_EXCLUS
        = new HashSet<string> { ID_BADGE };

    // ── Overlay ───────────────────────────────────────────────────────────────
    private const float OVERLAY_DUREE          = 1.2f;
    private const float OVERLAY_ALPHA_DEPART   = 0.72f;
    private const float OVERLAY_ECHELLE_DEPART = 1.0f;
    private const float OVERLAY_ECHELLE_FIN    = 1.35f;

    // ── Références données ────────────────────────────────────────────────────
    [Header("Données")]
    [SerializeField] private SO_InventaireObjets catalogue;
    [SerializeField] private SO_PlayerDatas donneesJoueur;
    [SerializeField] private EffetsObjetsSpeciaux effets;

    // ── Références UI ─────────────────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] private Transform conteneurSlots;
    [SerializeField] private GameObject prefabSlot;

    // ── Cache ─────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, ItemSlotUI> _slotsActifs = new Dictionary<string, ItemSlotUI>();

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        PopulerSlots();
    }

    // ── Population ────────────────────────────────────────────────────────────

    /// <summary>Instancie un slot par objet actif non-passif. Appelé une fois au démarrage.</summary>
    private void PopulerSlots()
    {
        ViderSlots();

        if (donneesJoueur == null)
        {
            Debug.LogError("[InventaireObjetsUI] SO_PlayerDatas non assigné.");
            return;
        }

        foreach (InventoryEntry entree in donneesJoueur.ObtenirInventairePlat())
        {
            if (IDS_EXCLUS.Contains(entree.objectName)) continue;

            DefinitionObjetSpecial definition = catalogue.ObtenirDefinition(entree.objectName);
            if (definition == null || definition.estPassif) continue;

            CreerSlot(definition, entree.quantity);
        }
    }

    private void CreerSlot(DefinitionObjetSpecial definition, int quantite)
    {
        GameObject slotGo = Instantiate(prefabSlot, conteneurSlots);
        ItemSlotUI slot = slotGo.GetComponent<ItemSlotUI>();

        if (slot == null)
        {
            Debug.LogError("[InventaireObjetsUI] Prefab slot sans composant ItemSlotUI.");
            Destroy(slotGo);
            return;
        }

        slot.Initialiser(definition, quantite, OnSlotClique);
        slotGo.SetActive(quantite > 0);
        _slotsActifs[definition.identifiant] = slot;
    }

    private void ViderSlots()
    {
        foreach (Transform enfant in conteneurSlots)
            Destroy(enfant.gameObject);
        _slotsActifs.Clear();
    }

    // ── Utilisation ───────────────────────────────────────────────────────────

    private void OnSlotClique(string identifiant)
    {
        if (effets == null)
        {
            Debug.LogError("[InventaireObjetsUI] EffetsObjetsSpeciaux non assigné.");
            return;
        }

        InventoryEntry entree = TrouverEntree(identifiant);
        if (entree == null || entree.quantity <= 0) return;

        DispatchEffet(identifiant);

        DefinitionObjetSpecial def = catalogue.ObtenirDefinition(identifiant);
        if (def != null)
            AfficherOverlay(def.sprite);

        entree.quantity--;
        donneesJoueur.SaveDatas();

        if (_slotsActifs.TryGetValue(identifiant, out ItemSlotUI slot))
        {
            slot.MettreAJourQuantite(entree.quantity);
            slot.gameObject.SetActive(entree.quantity > 0);
        }
    }

    private void DispatchEffet(string identifiant)
    {
        switch (identifiant)
        {
            case ID_LIASSE:        effets.UtiliserLiasseDeBillets();       break;
            case ID_PASSE_PARTOUT: effets.UtiliserFormulairePasePartout(); break;
            case ID_BADGE:         effets.UtiliserBadgeDuGouvernement();   break;
            case ID_TIRELIRE:      effets.UtiliserTirelireCochon();        break;
            case ID_GATEAU:        effets.UtiliserGateauChinois();         break;
            case ID_RADAR:         effets.UtiliserRadarObstacles();        break;
            case ID_ASPIRATEUR:    effets.UtiliserAspirateur();            break;
            case ID_MONTRE:        effets.UtiliserMontreAGousset();        break;
            default:
                Debug.LogWarning($"[InventaireObjetsUI] Identifiant inconnu : '{identifiant}'");
                break;
        }
    }

    // ── Overlay ───────────────────────────────────────────────────────────────

    private void AfficherOverlay(Sprite sprite)
    {
        if (sprite == null) return;
        StartCoroutine(AnimerOverlay(sprite));
    }

    private IEnumerator AnimerOverlay(Sprite sprite)
    {
        GameObject canvasGo = new GameObject("OverlayObjet");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;

        GameObject imageGo = new GameObject("Sprite");
        imageGo.transform.SetParent(canvasGo.transform, false);
        Image img = imageGo.AddComponent<Image>();
        img.sprite         = sprite;
        img.preserveAspect = true;
        img.raycastTarget  = false;

        RectTransform rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(400f, 400f);

        float elapsed = 0f;
        while (elapsed < OVERLAY_DUREE)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / OVERLAY_DUREE);
            float echelle = Mathf.Lerp(OVERLAY_ECHELLE_DEPART, OVERLAY_ECHELLE_FIN, t);
            imageGo.transform.localScale = new Vector3(echelle, echelle, 1f);
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(OVERLAY_ALPHA_DEPART, 0f, t));
            yield return null;
        }

        Destroy(canvasGo);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private InventoryEntry TrouverEntree(string identifiant)
    {
        if (donneesJoueur == null) return null;
        foreach (InventoryEntry entree in donneesJoueur.ObtenirInventairePlat())
        {
            if (entree.objectName == identifiant)
                return entree;
        }
        return null;
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ajoute 1 exemplaire de chaque objet spécial et rafraîchit la barre immédiatement.
    /// Inspector : clic droit sur le composant → "DEBUG — Donner 1 de chaque objet".
    /// Play mode : touche [I].
    /// </summary>
    [ContextMenu("DEBUG — Donner 1 de chaque objet")]
    public void DebugDonnerUnDeChaque()
    {
        if (donneesJoueur == null)
        {
            Debug.LogError("[InventaireObjetsUI] SO_PlayerDatas non assigné.");
            return;
        }

        string[] tousLesIds =
        {
            ID_LIASSE, ID_PASSE_PARTOUT, ID_BADGE, ID_TIRELIRE,
            ID_GATEAU, ID_RADAR, ID_ASPIRATEUR, ID_MONTRE
        };

        if (donneesJoueur.allObjectInInventory.Count == 0)
            donneesJoueur.allObjectInInventory.Add(new InventoryObject());

        InventoryObject groupe = donneesJoueur.allObjectInInventory[0];

        foreach (string id in tousLesIds)
        {
            bool trouve = false;
            foreach (InventoryEntry e in groupe.highScores)
            {
                if (e.objectName == id) { e.quantity += 1; trouve = true; break; }
            }
            if (!trouve)
                groupe.highScores.Add(new InventoryEntry(id, 1));
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(donneesJoueur);
#endif

        if (Application.isPlaying)
            PopulerSlots();

        Debug.Log("[InventaireObjetsUI] DEBUG : 1 de chaque objet ajouté à l'inventaire.");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
        {
            DebugDonnerUnDeChaque();
        }
    }
#endif
}
