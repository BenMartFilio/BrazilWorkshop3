using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Aspiration : MonoBehaviour
{
    [SerializeField] private GameObject _feedbackLogo;
    [SerializeField] private InputPlayerMovement _input;
    [SerializeField] private SpriteShatter shatter;

    private ChangeSkin _documents;

    public bool canAspire = false;
    public bool isDead = false;

    public bool finished = true;

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

    public void Die()
    {
        isDead = true;
        shatter.ResetShatter();
        _feedbackLogo.SetActive(false);
    }

    public void UnDie()
    {
        isDead = false;
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
        finished = false;
        shatter.Shatter();
    }

    public void OnDestroyingUI()
    {
        finished = true;
        _feedbackLogo.SetActive(false);
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
            _feedbackLogo.SetActive(true);
            shatter.ResetShatter();
            Debug.Log("Enabled");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (finished == false)
        {
            return;
        }
        if (other.GetComponent<ChangeSkin>() == _documents)
        {
            canAspire = false;
            _feedbackLogo.SetActive(false);
            Debug.Log("Disabled");
        }
    }

}
