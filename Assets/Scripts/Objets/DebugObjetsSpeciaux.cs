using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ObjetsSpeciaux;

/// <summary>
/// Panel de debug pour tester les 8 objets speciaux en jeu.
/// Cree son propre Canvas overlay en Awake -- aucune configuration Inspector requise
/// excepte la reference a EffetsObjetsSpeciaux.
/// Desactiver ou retirer ce composant avant un build de release.
/// </summary>
public class DebugObjetsSpeciaux : MonoBehaviour
{
    [SerializeField] private EffetsObjetsSpeciaux effets;

    // ── Raccourci clavier pour afficher/masquer le panel ─────────────────────
    private const KeyCode TOGGLE_KEY = KeyCode.F1;

    // ── Mise en page ──────────────────────────────────────────────────────────
    private const int   PANEL_WIDTH      = 480;
    private const int   BUTTON_HEIGHT    = 72;
    private const int   BUTTON_SPACING   = 10;
    private const int   PADDING          = 20;
    private const int   FONT_SIZE_BTN    = 24;
    private const int   FONT_SIZE_STATUS = 20;
    private const int   FONT_SIZE_TITLE  = 26;
    private const float PANEL_ALPHA      = 0.92f;

    // ── Etiquettes des boutons ────────────────────────────────────────────────
    private static readonly (string label, string method)[] BOUTONS_PASSIFS =
    {
        ("Liasse de Billets (+50% patience)", nameof(UtiliserLiasse)),
        ("Formulaire Passe-Partout",           nameof(UtiliserPassePartout)),
        ("Badge du Gouvernement",              nameof(UtiliserBadge)),
    };

    private static readonly (string label, string method)[] BOUTONS_ACTIFS =
    {
        ("Tirelire Cochon (x2 pieces)",    nameof(UtiliserTirelire)),
        ("Gateau Chinois (50% esquive)",   nameof(UtiliserGateau)),
        ("Radar a Obstacles",              nameof(UtiliserRadar)),
        ("Aspirateur (x2 aspiration)",     nameof(UtiliserAspirateur)),
        ("Montre a Gousset (ralenti)",     nameof(UtiliserMontre)),
    };

    // ── References UI generees ────────────────────────────────────────────────
    private GameObject _panelRoot;
    private TextMeshProUGUI _statusText;
    private bool _visible = true;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (effets == null)
            effets = FindFirstObjectByType<EffetsObjetsSpeciaux>();

        BuildUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(TOGGLE_KEY))
            ToggleVisibility();
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas dedié pour ne pas polluer MainCanvas
        GameObject canvasGo = new GameObject("DebugObjetsCanvas");
        DontDestroyOnLoad(canvasGo);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel de fond
        int totalButtons = BOUTONS_PASSIFS.Length + BOUTONS_ACTIFS.Length;
        int panelHeight = PADDING * 2
            + FONT_SIZE_TITLE + BUTTON_SPACING
            + totalButtons * (BUTTON_HEIGHT + BUTTON_SPACING)
            + BUTTON_SPACING           // separateur entre passifs et actifs
            + FONT_SIZE_STATUS + BUTTON_SPACING  // ligne de statut
            + FONT_SIZE_BTN + BUTTON_SPACING;    // hint F1

        _panelRoot = CreatePanel(canvasGo.transform, panelHeight);

        // Contenu vertical
        int currentY = -(PADDING);
        currentY = AddTitle(_panelRoot.transform, "DEBUG -- Objets Speciaux", currentY);
        currentY -= BUTTON_SPACING * 2;

        // Section passifs
        currentY = AddSectionLabel(_panelRoot.transform, "PASSIFS (Barrage)", currentY);
        foreach (var (label, method) in BOUTONS_PASSIFS)
        {
            currentY = AddButton(_panelRoot.transform, label, method, currentY, new Color(0.25f, 0.45f, 0.25f));
        }

        currentY -= BUTTON_SPACING * 2;

        // Section actifs
        currentY = AddSectionLabel(_panelRoot.transform, "ACTIFS (Route)", currentY);
        foreach (var (label, method) in BOUTONS_ACTIFS)
        {
            currentY = AddButton(_panelRoot.transform, label, method, currentY, new Color(0.25f, 0.30f, 0.50f));
        }

        currentY -= BUTTON_SPACING * 2;

        // Ligne statut
        _statusText = CreateText(_panelRoot.transform, "Pret.", FONT_SIZE_STATUS, new Color(0.9f, 0.9f, 0.5f));
        PositionElement(_statusText.rectTransform, PADDING, currentY, PANEL_WIDTH - PADDING * 2, FONT_SIZE_STATUS + 4);

        // Hint clavier
        currentY -= FONT_SIZE_STATUS + BUTTON_SPACING;
        TextMeshProUGUI hint = CreateText(_panelRoot.transform, $"[{TOGGLE_KEY}] Afficher/Masquer", FONT_SIZE_STATUS - 1, new Color(0.5f, 0.5f, 0.5f));
        PositionElement(hint.rectTransform, PADDING, currentY, PANEL_WIDTH - PADDING * 2, FONT_SIZE_STATUS + 4);
    }

    private GameObject CreatePanel(Transform parent, int height)
    {
        GameObject go = new GameObject("Panel");
        go.transform.SetParent(parent, false);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, PANEL_ALPHA);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
        rt.sizeDelta = new Vector2(PANEL_WIDTH, height);

        return go;
    }

    private int AddTitle(Transform parent, string text, int y)
    {
        TextMeshProUGUI tmp = CreateText(parent, text, FONT_SIZE_TITLE, Color.white);
        tmp.fontStyle = FontStyles.Bold;
        PositionElement(tmp.rectTransform, PADDING, y, PANEL_WIDTH - PADDING * 2, FONT_SIZE_TITLE + 4);
        return y - (FONT_SIZE_TITLE + 4 + BUTTON_SPACING);
    }

    private int AddSectionLabel(Transform parent, string text, int y)
    {
        TextMeshProUGUI tmp = CreateText(parent, text, FONT_SIZE_STATUS, new Color(0.7f, 0.7f, 0.7f));
        tmp.fontStyle = FontStyles.Bold;
        PositionElement(tmp.rectTransform, PADDING, y, PANEL_WIDTH - PADDING * 2, FONT_SIZE_STATUS + 4);
        return y - (FONT_SIZE_STATUS + 4 + BUTTON_SPACING);
    }

    private int AddButton(Transform parent, string label, string methodName, int y, Color color)
    {
        GameObject go = new GameObject($"Btn_{methodName}");
        go.transform.SetParent(parent, false);

        Image bg = go.AddComponent<Image>();
        bg.color = color;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = color;
        cb.highlightedColor = color * 1.25f;
        cb.pressedColor     = color * 0.75f;
        cb.selectedColor    = color;
        btn.colors          = cb;

        PositionElement(go.GetComponent<RectTransform>(), PADDING, y, PANEL_WIDTH - PADDING * 2, BUTTON_HEIGHT);

        TextMeshProUGUI tmp = CreateText(go.transform, label, FONT_SIZE_BTN, Color.white);
        tmp.rectTransform.anchorMin        = Vector2.zero;
        tmp.rectTransform.anchorMax        = Vector2.one;
        tmp.rectTransform.offsetMin        = new Vector2(6, 0);
        tmp.rectTransform.offsetMax        = new Vector2(-6, 0);
        tmp.alignment                      = TextAlignmentOptions.MidlineLeft;

        // Stocker le nom de methode dans le listener via closure
        string captured = methodName;
        btn.onClick.AddListener(() => OnButtonClicked(captured));

        return y - (BUTTON_HEIGHT + BUTTON_SPACING);
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, Color color)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = content;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        return tmp;
    }

    private void PositionElement(RectTransform rt, int x, int y, int w, int h)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    private void ToggleVisibility()
    {
        _visible = !_visible;
        if (_panelRoot != null)
            _panelRoot.SetActive(_visible);
    }

    // ── Dispatch des boutons ──────────────────────────────────────────────────

    private void OnButtonClicked(string methodName)
    {
        if (effets == null)
        {
            SetStatus("ERREUR : EffetsObjetsSpeciaux non trouve !", Color.red);
            return;
        }

        // Appel par reflexion pour eviter un switch/case verbeux
        var method = typeof(DebugObjetsSpeciaux).GetMethod(
            methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
            method.Invoke(this, null);
        else
            SetStatus($"Methode introuvable : {methodName}", Color.red);
    }

    private void SetStatus(string msg, Color? color = null)
    {
        if (_statusText == null) return;
        _statusText.text  = msg;
        _statusText.color = color ?? new Color(0.9f, 0.9f, 0.5f);
        Debug.Log($"[DebugObjetsSpeciaux] {msg}");
    }

    // ── Appels vers EffetsObjetsSpeciaux ─────────────────────────────────────

    private void UtiliserLiasse()
    {
        effets.UtiliserLiasseDeBillets();
        SetStatus("Liasse : +50% patience appliquee.");
    }

    private void UtiliserPassePartout()
    {
        effets.UtiliserFormulairePasePartout();
        SetStatus("Passe-Partout : prochain formulaire valide.");
    }

    private void UtiliserBadge()
    {
        effets.UtiliserBadgeDuGouvernement();
        SetStatus("Badge : barrage entier valide.");
    }

    private void UtiliserTirelire()
    {
        effets.UtiliserTirelireCochon();
        SetStatus($"Tirelire : x{effets.ObtenirMultiplicateurPieces()} pieces -- 20s.");
    }

    private void UtiliserGateau()
    {
        effets.UtiliserGateauChinois();
        SetStatus("Gateau Chinois : 50% esquive -- 20s.");
    }

    private void UtiliserRadar()
    {
        effets.UtiliserRadarObstacles();
        SetStatus("Radar : indicateurs obstacles -- 20s.");
    }

    private void UtiliserAspirateur()
    {
        effets.UtiliserAspirateur();
        SetStatus($"Aspirateur : x{effets.ObtenirMultiplicateurTimingAspiration()} timing -- 20s.");
    }

    private void UtiliserMontre()
    {
        effets.UtiliserMontreAGousset();
        SetStatus("Montre : ralenti 50% + NB -- 20s.");
    }
}
