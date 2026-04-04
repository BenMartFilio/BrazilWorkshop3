using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de focus pour l'équipement d'un skin cosmétique.
/// Le GameObject lui-même est activé/désactivé (pas un focusRoot enfant).
/// Après équipement, invoque onEquippedCallback pour que les boutons boutique
/// puissent rafraîchir leur état.
/// </summary>
public class SkinEquipFocus : MonoBehaviour
{
    private const float DUREE_ANIM = 0.3f;
    private const float ALPHA_OVERLAY = 0.6f;
    private const float FACTEUR_ZOOM = 3f;
    private const float DUREE_TREMBLEMENT = 0.45f;
    private const float ANGLE_MAX = 6f;
    private const float FREQUENCE = 28f;

    [Header("Données")]
    [SerializeField] private SO_PlayerDatas playerDatas;

    [Header("Overlay")]
    [SerializeField] private Image fondSombre;
    [SerializeField] private Button boutonFermeture;

    [Header("Focus")]
    [SerializeField] private Image iconeProxy;
    [SerializeField] private Button boutonEquiper;
    [SerializeField] private TMP_Text labelDejaEquipe;   // texte "Équipé" visible quand déjà équipé
    [SerializeField] private TMP_Text labelNom;
    [SerializeField] private TMP_Text labelDescription;

    [Header("Canvas")]
    [SerializeField] private Canvas canvasRacine;

    [Header("Audio")]
    [SerializeField] private AudioEventDispatcher audioEventDispatcher;
    [SerializeField] private AudioType equipSound;

    private Coroutine _coroutineAnim;
    private bool _ouvert = false;
    private Image _iconeSource = null;
    private int _skinIndex = -1;
    private System.Action _onEquippedCallback;

    #region Unity Lifecycle

    private void Awake()
    {
        // Listeners — Awake tourne une seule fois au chargement de la scène
        if (boutonFermeture != null) boutonFermeture.onClick.AddListener(Fermer);
        if (boutonEquiper != null) boutonEquiper.onClick.AddListener(OnEquiperClique);

        // Se désactive après l'init pour ne pas apparaître au démarrage
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (boutonFermeture != null) boutonFermeture.onClick.RemoveListener(Fermer);
        if (boutonEquiper != null) boutonEquiper.onClick.RemoveListener(OnEquiperClique);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Ouvre le panel depuis l'icône source du bouton boutique.
    /// onEquippedCallback : appelé après équipement pour rafraîchir les boutons.
    /// </summary>
    public void Ouvrir(Image iconeSource, int skinIndex, DefinitionCosmetique def,
                       System.Action onEquippedCallback = null)
    {
        if (_ouvert) return;

        _iconeSource = iconeSource;
        _skinIndex = skinIndex;
        _onEquippedCallback = onEquippedCallback;
        _ouvert = true;

        // Active le GO avant tout — nécessaire pour que les coroutines tournent
        gameObject.SetActive(true);

        if (iconeProxy != null) iconeProxy.sprite = def.sprite;
        if (labelNom != null) labelNom.text = def.nomAffichage;
        if (labelDescription != null) labelDescription.text = def.description;

        // Bouton Équiper vs label "Équipé"
        bool dejaEquipe = playerDatas != null && playerDatas.skinEquiped == skinIndex;
        if (boutonEquiper != null) boutonEquiper.gameObject.SetActive(!dejaEquipe);
        if (labelDejaEquipe != null) labelDejaEquipe.gameObject.SetActive(dejaEquipe);

        // Reset overlay
        if (fondSombre != null)
        {
            Color c = fondSombre.color; c.a = 0f;
            fondSombre.color = c;
        }

        PlacerProxySurSource();

        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);
        _coroutineAnim = StartCoroutine(AnimerOuverture());
    }

    /// <summary>Ferme le panel immédiatement sans équiper.</summary>
    public void Fermer()
    {
        if (!_ouvert) return;
        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);

        if (_iconeSource != null) _iconeSource.enabled = true;

        if (iconeProxy != null)
        {
            iconeProxy.rectTransform.anchoredPosition = Vector2.zero;
            iconeProxy.rectTransform.sizeDelta = Vector2.zero;
            iconeProxy.rectTransform.localRotation = Quaternion.identity;
        }

        _iconeSource = null;
        _skinIndex = -1;
        _onEquippedCallback = null;
        _ouvert = false;

        gameObject.SetActive(false);
    }

    #endregion

    #region Logic

    private void OnEquiperClique()
    {
        if (boutonEquiper != null) boutonEquiper.gameObject.SetActive(false);
        if (labelDejaEquipe != null) labelDejaEquipe.gameObject.SetActive(true);

        if (_coroutineAnim != null) StopCoroutine(_coroutineAnim);
        _coroutineAnim = StartCoroutine(SequenceEquiper());
    }

    private IEnumerator SequenceEquiper()
    {
        yield return StartCoroutine(AnimerTremblement());

        if (playerDatas != null && _skinIndex >= 0)
        {
            playerDatas.skinEquiped = _skinIndex;
            playerDatas.SaveDatas();
        }

        audioEventDispatcher?.PlayAudio(equipSound);


        SkinPurchaseButton.NotifierSkinEquipe();

        Fermer();
    }


    #endregion

    #region Animations

    private void PlacerProxySurSource()
    {
        if (_iconeSource == null || iconeProxy == null) return;

        RectTransform rtSrc = _iconeSource.rectTransform;
        RectTransform rtProxy = iconeProxy.rectTransform;
        RectTransform rtParent = rtProxy.parent as RectTransform;

        rtProxy.anchorMin = new Vector2(0.5f, 0.5f);
        rtProxy.anchorMax = new Vector2(0.5f, 0.5f);
        rtProxy.pivot = new Vector2(0.5f, 0.5f);
        rtProxy.sizeDelta = rtSrc.rect.size;
        rtProxy.localScale = Vector3.one;

        Vector3[] coins = new Vector3[4];
        rtSrc.GetWorldCorners(coins);
        Vector3 centre = (coins[0] + coins[2]) * 0.5f;

        Camera cam = canvasRacine.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvasRacine.worldCamera;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, centre);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rtParent, screen, cam, out Vector2 posLocale);

        rtProxy.anchoredPosition = posLocale;
        _iconeSource.enabled = false;
    }

    private IEnumerator AnimerOuverture()
    {
        if (iconeProxy == null) yield break;

        RectTransform rt = iconeProxy.rectTransform;
        Vector2 posDepart = rt.anchoredPosition;
        Vector2 tailleDepart = rt.sizeDelta;
        Vector2 posCible = Vector2.zero;
        Vector2 tailleCible = tailleDepart * FACTEUR_ZOOM;
        float elapsed = 0f;

        while (elapsed < DUREE_ANIM)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / DUREE_ANIM);
            float e = 1f - Mathf.Pow(1f - t, 3f);

            rt.anchoredPosition = Vector2.Lerp(posDepart, posCible, e);
            rt.sizeDelta = Vector2.Lerp(tailleDepart, tailleCible, e);

            if (fondSombre != null)
            {
                Color c = fondSombre.color;
                c.a = Mathf.Lerp(0f, ALPHA_OVERLAY, e);
                fondSombre.color = c;
            }

            yield return null;
        }

        rt.anchoredPosition = posCible;
        rt.sizeDelta = tailleCible;
    }

    private IEnumerator AnimerTremblement()
    {
        if (iconeProxy == null) yield break;

        RectTransform rt = iconeProxy.rectTransform;
        float elapsed = 0f;

        while (elapsed < DUREE_TREMBLEMENT)
        {
            elapsed += Time.unscaledDeltaTime;
            float attenuation = 1f - elapsed / DUREE_TREMBLEMENT;
            rt.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(elapsed * FREQUENCE) * ANGLE_MAX * attenuation);
            yield return null;
        }

        rt.localRotation = Quaternion.identity;
    }

    #endregion
}
