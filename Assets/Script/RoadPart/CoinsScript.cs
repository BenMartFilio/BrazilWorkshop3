using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    public void OnCoinRecuperation()
    {
        gameObject.SetActive(false);
    }
}
