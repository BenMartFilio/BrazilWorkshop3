using UnityEngine;

public class CoinsScript : MonoBehaviour
{
    public void OnCoinRecuperation()
    {
        Destroy(gameObject);
    }
}
