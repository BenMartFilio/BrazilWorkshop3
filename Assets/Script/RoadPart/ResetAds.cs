using UnityEngine;

public class ResetAds : MonoBehaviour
{
    [SerializeField] private ReviveButtonAd ads;

    private void Start()
    {
        if (ads == null)
        {
            return;
        }

        ads.ResetReviveCount();
    }
}
