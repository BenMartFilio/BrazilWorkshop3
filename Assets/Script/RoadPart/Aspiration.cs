using System;
using System.Collections;
using Barrage.Formulaires;
using ObjetsSpeciaux;
using UnityEngine;

public class Aspiration : MonoBehaviour
{
    /// <summary>Fired whenever the player successfully collects a document from a colored car.</summary>
    public static event Action OnDocumentCollected;

    [SerializeField] private GameObject _feedbackLogo;
    [SerializeField] private InputPlayerMovement _input;
    [SerializeField] private SpriteShatter shatter;
    [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
    
    [SerializeField] private AudioType _shatter;

    [Header("Inventaire")]
    [Tooltip("ScriptableObject inventaire dans lequel ajouter le formulaire aspiré.")]
    [SerializeField] private FormulaireInventaire inventaire;

    [Tooltip("Référence optionnelle à EffetsObjetsSpeciaux pour étendre la fenêtre d'aspiration (item Aspirateur).")]
    [SerializeField] private EffetsObjetsSpeciaux _effets;

    private ChangeSkin _documents;

    public bool canAspire = false;
    public bool isDead    = false;
    public bool finished  = true;

    private float     _tempsEntreeDansZone;
    private Coroutine _coroutineExtension;


    [Header("Effet grossissement")]
    [Tooltip("Amplitude maximale du grossissement en scale local (ex: 0.3 = +30%).")]
    [SerializeField] private float _grossissementAmplitude = 0.3f;

    [Tooltip("Durée totale de l'animation de grossissement en secondes.")]
    [SerializeField] private float _grossissementDuree = 0.35f;

    [Tooltip("Courbe de grossissement : montée rapide puis descente douce. X = temps normalisé [0,1], Y = valeur scale normalisée [0,1].")]
    [SerializeField]
    private AnimationCurve _grossissementCourbe = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 8f),
        new Keyframe(0.25f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -2f, 0f)
    );

    [SerializeField] private GameObject _player;

    private Coroutine _coroutineGrossissement;

    private void Start()
    {
        AdaptOnScreen();
    }

    private void AdaptOnScreen()
    {
        float worldScreenHeight = Camera.main.orthographicSize * 2;
        float worldScreenWidth  = worldScreenHeight / Screen.height * Screen.width;
        GetComponent<BoxCollider2D>().size = new Vector2(worldScreenWidth, 2);
    }

    private void OnEnable()
    {
        _input.OnTapScreen += AspirationAttempt;
    }

    private void OnDisable()
    {
        _input.OnTapScreen -= AspirationAttempt;
    }

    public void Die()
    {
        isDead = true;
        AnnulerExtension();
        shatter.ResetShatter();
        _feedbackLogo.SetActive(false);
    }

    public void UnDie()
    {
        isDead = false;
    }

    private void AspirationAttempt()
    {
        if (!canAspire || isDead) return;

        // La fenêtre est consommée dès que le joueur tape (normale ou étendue).
        canAspire = false;
        AnnulerExtension();
        FeedbackClicked();
        

        FormulaireType? type = _documents != null ? _documents.ObtenirType() : null;

        if (type.HasValue)
        {
            inventaire?.Ajouter(type.Value);
            Debug.Log($"[Aspiration] +1 {type.Value} → total : {inventaire?.ObtenirQuantité(type.Value)}");
            OnDocumentCollected?.Invoke();
            if (_player != null)
                LancerGrossissement(_player.transform);
        }
        else
        {
            Debug.LogWarning("[Aspiration] Véhicule aspiré sans mapping FormulaireType valide — inventaire non modifié.");
        }
    }

    private void FeedbackClicked()
    {
        if(_audioEventDispatcher != null) _audioEventDispatcher.PlayAudio(_shatter);
        finished = false;

        shatter.Shatter();
    }

    public void OnDestroyingUI()
    {
        finished = true;
        _feedbackLogo.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ChangeSkin skin = collision.GetComponent<ChangeSkin>();
        if (skin == null) return;

        // Annuler toute extension en cours d'un véhicule précédent.
        AnnulerExtension();

        _documents            = skin;
        _tempsEntreeDansZone  = Time.time;
        canAspire             = true;
        finished = true;
        _feedbackLogo.SetActive(true);
        shatter.ResetShatter();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (finished == false) return;

        // Le Collider2D ou son GameObject peut avoir été détruit par le pool entre
        // l'entrée et la sortie du trigger. L'opérateur == d'Unity détecte les fake-null.
        if (other == null || other.gameObject == null) return;

        if (other.GetComponent<ChangeSkin>() != _documents) return;

        float dureePassage   = Time.time - _tempsEntreeDansZone;
        float multiplicateur = _effets != null ? _effets.ObtenirMultiplicateurTimingAspiration() : 1f;
        float extension      = dureePassage * (multiplicateur - 1f);

        if (extension > 0.05f)
        {
            // Fenêtre étendue : garder canAspire et le logo actifs pendant la durée calculée.
            _coroutineExtension = StartCoroutine(FenetreEtendue(extension));
            Debug.Log($"Aspiration -- fenêtre étendue de {extension:F2}s (x{multiplicateur})");
        }
        else
        {
            canAspire = false;
            if (_feedbackLogo != null) _feedbackLogo.SetActive(false);
            Debug.Log("Disabled");
        }
    }

    /// <summary>Maintient la fenêtre d'aspiration ouverte après la sortie du trigger.</summary>
    private IEnumerator FenetreEtendue(float duree)
    {
        yield return new WaitForSeconds(duree);
        canAspire = false;
        if (_feedbackLogo != null) _feedbackLogo.SetActive(false);
        _coroutineExtension = null;
        Debug.Log("Fenêtre étendue terminée");
    }

    private void AnnulerExtension()
    {
        if (_coroutineExtension == null) return;
        StopCoroutine(_coroutineExtension);
        _coroutineExtension = null;
    }

    private void LancerGrossissement(Transform cible)
    {
        if (_coroutineGrossissement != null)
            StopCoroutine(_coroutineGrossissement);
        _coroutineGrossissement = StartCoroutine(AnimerGrossissement(cible));
    }

    /// <summary>Anime un grossissement puis retour à la taille originale sur la cible, via une AnimationCurve.</summary>
    private IEnumerator AnimerGrossissement(Transform cible)
    {
        if (cible == null) yield break;

        Vector3 scaleInitiale = cible.localScale;
        float elapsed = 0f;

        while (elapsed < _grossissementDuree)
        {
            if (cible == null) yield break;

            float t = elapsed / _grossissementDuree;
            float valeurCourbe = _grossissementCourbe.Evaluate(t);
            cible.localScale = scaleInitiale * (1f + valeurCourbe * _grossissementAmplitude);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Garantit le retour exact à la taille initiale
        if (cible != null)
            cible.localScale = scaleInitiale;

        _coroutineGrossissement = null;
    }

}
