using UnityEngine;
using Unity.Services.LevelPlay;

public class AdvertismentsIOS : MonoBehaviour
{
    public void Start()
    {
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        // SDK init
        LevelPlay.Init("25b40871d");
    }

    /// <summary>Called when the LevelPlay SDK initialization succeeds.</summary>
    private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully.");
    }

    /// <summary>Called when the LevelPlay SDK initialization fails.</summary>
    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay SDK initialization failed: {error}");
    }

}
