using UnityEngine;
using UnityEngine.UI;

public class Aspiration : MonoBehaviour
{
    [SerializeField] private Image _feedbackLogo;
    [SerializeField] private InputPlayerMovement _input;

    private ChangeSkin _documents;

    public bool canAspire = false;
    public bool isDead = false;


    private void Start()
    {
        AdaptOnScreen();
    }

    private void AdaptOnScreen()
    {
        float worldScreenHeight = Camera.main.orthographicSize * 2;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        float scaleX = worldScreenWidth;
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

    private void AspirationAttempt()
    {
        if (canAspire)
        {
            if (isDead) 
            {
                return;
            }
            canAspire = false;
            FeedbackClicked();
            int doc = _documents.OnAspiration();
            Debug.Log(doc);
        }
    }

    private void FeedbackClicked()
    {

    }

    private void GettingDocuments()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ChangeSkin>() != null) 
        { 
            _documents = collision.GetComponent<ChangeSkin>();
            canAspire = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<ChangeSkin>() == _documents)
        {
            canAspire = false;
        }
    }
}
