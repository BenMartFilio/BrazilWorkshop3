using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private GameObject coinFeedbackPrefab;

    public void OnCoinRecuperation()
    {
        SpawnFeedback();
        gameObject.SetActive(false);
    }

    private void SpawnFeedback()
    {
        if (coinFeedbackPrefab == null) return;

        GameObject fx = Instantiate(coinFeedbackPrefab, transform.position, Quaternion.identity);

        // Hérite de la scale du parent pour rester cohérent avec la taille de la pièce
        fx.transform.localScale = transform.localScale;
    }
}