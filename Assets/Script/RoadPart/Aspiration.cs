using System.Collections;
using ObjetsSpeciaux;
using UnityEngine;
using UnityEngine.UI;

public class Aspiration : MonoBehaviour
{
    [SerializeField] private GameObject _feedbackLogo;
    [SerializeField] private InputPlayerMovement _input;
    [SerializeField] private SpriteShatter shatter;

    [Tooltip("Référence optionnelle à EffetsObjetsSpeciaux pour étendre la fenêtre d'aspiration (item Aspirateur).")]
    [SerializeField] private EffetsObjetsSpeciaux _effets;

    private ChangeSkin _documents;

    public bool canAspire = false;
    public bool isDead = false;
    public bool finished = true;

    // Heure d'entrée du véhicule dans la zone trigger (pour calculer l'extension).
    private float _tempsEntreeDansZone;
    private Coroutine _coroutineExtension;

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
        int doc = _documents.OnAspiration();
        Debug.Log(doc);
    }

    private void FeedbackClicked()
    {
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
        if (collision.GetComponent<ChangeSkin>() == null) return;

        // Annuler toute extension en cours d'un véhicule précédent.
        AnnulerExtension();

        _documents            = collision.GetComponent<ChangeSkin>();
        _tempsEntreeDansZone  = Time.time;
        canAspire             = true;
        finished = true;
        _feedbackLogo.SetActive(true);
        shatter.ResetShatter();
        Debug.Log("Enabled");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (finished == false) return;
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
            _feedbackLogo.SetActive(false);
            Debug.Log("Disabled");
        }
    }

    /// <summary>Maintient la fenêtre d'aspiration ouverte après la sortie du trigger.</summary>
    private IEnumerator FenetreEtendue(float duree)
    {
        yield return new WaitForSeconds(duree);
        canAspire = false;
        _feedbackLogo.SetActive(false);
        _coroutineExtension = null;
        Debug.Log("Fenêtre étendue terminée");
    }

    private void AnnulerExtension()
    {
        if (_coroutineExtension == null) return;
        StopCoroutine(_coroutineExtension);
        _coroutineExtension = null;
    }
}
