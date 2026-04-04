using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anime la radio dans l'onglet inventaire :
/// - Pulse (grossit/dégrossit) en boucle via un sinus
/// - Émet des sprites de notes de musique qui dérivent de droite à gauche
/// </summary>
public class RadioAnimation : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────
    private const float SCALE_BASE = 1f;
    private const float SCALE_AMPLITUDE = 0.06f;   // ±6% autour de la taille de base
    private const float PULSE_SPEED = 3.5f;     // fréquence du pulse (rad/s)
    private Vector2 _tempPos;

    // ── Notes de musique ──────────────────────────────────────────────────────
    [Header("Notes de musique")]
    [Tooltip("Sprites de notes à afficher aléatoirement (assigne 1 ou plusieurs).")]
    [SerializeField] private Sprite[] spritesNotes;

    [Tooltip("Parent RectTransform dans lequel les notes seront instanciées (ex : un GameObject 'NotesContainer' enfant d'InventoryMenu).")]
    [SerializeField] private RectTransform conteneurNotes;

    [Tooltip("Nombre de notes en circulation simultanément.")]
    [SerializeField] private int nombreNotes = 4;

    [Tooltip("Taille en pixels d'une note.")]
    [SerializeField] private float tailleNote = 32f;

    [Tooltip("Vitesse de déplacement horizontal des notes (pixels/s, valeur positive = vers la gauche).")]
    [SerializeField] private float vitesseDeplacement = 80f;

    [Tooltip("Oscillation verticale des notes (amplitude en pixels).")]
    [SerializeField] private float amplitudeVerticale = 18f;

    [Tooltip("Intervalle en secondes entre deux émissions de notes.")]
    [SerializeField] private float intervalleEmission = 0.6f;

    // ── Références ────────────────────────────────────────────────────────────
    [Header("Références")]
    [Tooltip("RectTransform de la radio — doit être celui du GameObject portant ce script.")]
    [SerializeField] private RectTransform rectRadio;

    // ── Pool d'objets ─────────────────────────────────────────────────────────
    private readonly List<NoteInstance> _notes = new List<NoteInstance>();
    private float _tempsDepuisEmission = 0f;
    private float _phaseOffset = 0f;

    // Cache du Canvas et de la caméra pour éviter GetComponentInParent dans Update
    private Canvas _canvas;
    private Camera _canvasCamera;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (rectRadio == null)
            rectRadio = GetComponent<RectTransform>();

        _canvas = GetComponentInParent<Canvas>();
        _canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera : null;

        // Pré-crée le pool de notes (désactivées)
        if (conteneurNotes != null && spritesNotes != null && spritesNotes.Length > 0)
            CreerPoolNotes();
    }

    private void OnEnable()
    {
        // Reset phase pour que le pulse reparte proprement
        _phaseOffset = Time.unscaledTime;
        _tempsDepuisEmission = 0f;

        // Remet toutes les notes à l'état inactif
        foreach (NoteInstance note in _notes)
            note.gameObject.SetActive(false);
    }

    private void Update()
    {
        AnimerPulse();
        GererEmissionNotes();
        DeplacerNotes();
    }

    // ── Pulse ─────────────────────────────────────────────────────────────────

    private void AnimerPulse()
    {
        if (rectRadio == null) return;

        float sin = Mathf.Sin((Time.unscaledTime - _phaseOffset) * PULSE_SPEED);
        float scale = SCALE_BASE + sin * SCALE_AMPLITUDE;
        rectRadio.localScale = new Vector3(scale, scale, 1f);
    }

    // ── Émission de notes ─────────────────────────────────────────────────────

    private void GererEmissionNotes()
    {
        if (_notes.Count == 0) return;

        _tempsDepuisEmission += Time.unscaledDeltaTime;
        if (_tempsDepuisEmission < intervalleEmission) return;
        _tempsDepuisEmission = 0f;

        // Cherche une note inactive dans le pool
        foreach (NoteInstance note in _notes)
        {
            if (!note.gameObject.activeSelf)
            {
                EmettreNote(note);
                return;
            }
        }
    }

    private void EmettreNote(NoteInstance note)
    {
        if (spritesNotes == null || spritesNotes.Length == 0) return;

        // Sprite aléatoire
        note.image.sprite = spritesNotes[Random.Range(0, spritesNotes.Length)];

        // Position de départ : côté droit de la radio, hauteur aléatoire
        Vector2 posDepart = ObtenirPositionEmission();
        note.rectTransform.anchoredPosition = posDepart;
        note.posDepart = posDepart;
        note.tempsVie = 0f;
        note.dureeVie = (conteneurNotes.rect.width + tailleNote * 2f) / vitesseDeplacement;
        note.phaseVerticale = Random.Range(0f, Mathf.PI * 2f);

        // Transparence
        Color c = note.image.color;
        c.a = 1f;
        note.image.color = c;

        note.gameObject.SetActive(true);
    }

    // ── Déplacement des notes ─────────────────────────────────────────────────

    private void DeplacerNotes()
    {
        foreach (NoteInstance note in _notes)
        {
            if (!note.gameObject.activeSelf) continue;

            note.tempsVie += Time.unscaledDeltaTime;
            float t = note.tempsVie / note.dureeVie;

            if (t >= 1f)
            {
                note.gameObject.SetActive(false);
                continue;
            }

            // Déplacement horizontal (droite → gauche)
            float x = note.posDepart.x - vitesseDeplacement * note.tempsVie;

            // Ondulation verticale
            float y = note.posDepart.y
                      + Mathf.Sin(note.tempsVie * 2.5f + note.phaseVerticale) * amplitudeVerticale;

            _tempPos.x = x;
            _tempPos.y = y;
            note.rectTransform.anchoredPosition = _tempPos;

            // Fondu en sortie (derniers 30%)
            float alpha = t < 0.7f ? 1f : Mathf.InverseLerp(1f, 0.7f, t);
            Color c = note.image.color;
            c.a = alpha;
            note.image.color = c;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne la position de départ d'une note en coordonnées locales du conteneurNotes.
    /// La note part du côté droit de la radio avec une hauteur légèrement aléatoire.
    /// </summary>
    private Vector2 ObtenirPositionEmission()
    {
        if (rectRadio == null || conteneurNotes == null)
            return Vector2.zero;

        Vector3[] coins = new Vector3[4];
        rectRadio.GetWorldCorners(coins);
        Vector3 bordDroit = (coins[2] + coins[3]) * 0.5f;
        bordDroit.x = coins[2].x;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvasCamera, bordDroit);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            conteneurNotes, screenPoint, _canvasCamera, out Vector2 posLocale);

        posLocale.y += Random.Range(-20f, 30f);
        return posLocale;
    }

    private void CreerPoolNotes()
    {
        for (int i = 0; i < nombreNotes; i++)
        {
            GameObject go = new GameObject($"Note_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(conteneurNotes, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(tailleNote, tailleNote);

            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;

            go.SetActive(false);

            _notes.Add(new NoteInstance
            {
                gameObject = go,
                rectTransform = rt,
                image = img
            });
        }
    }

    // ── Classes internes ──────────────────────────────────────────────────────

    private class NoteInstance
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
        public Vector2 posDepart;
        public float tempsVie;
        public float dureeVie;
        public float phaseVerticale;
    }
}
