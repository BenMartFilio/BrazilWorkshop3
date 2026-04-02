using Barrage.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles immediate return to the main menu when the player dies or loses in MapTuto or BarrageTuto.
/// - In MapTuto : subscribes to <see cref="EndManager.OnPlayerDied"/> (player collision death).
/// - In BarrageTuto : subscribes to <see cref="BarrePatience.OnPatienceEpuisée"/> (patience runs out).
/// Does not interfere with any existing behaviour — it simply loads the menu scene first.
/// Only active in whichever tutorial scene this GameObject lives in.
/// </summary>
public class TutoDeathHandler : MonoBehaviour
{
    [Tooltip("Name of the main menu scene to load on death/lose.")]
    [SerializeField] private string _menuSceneName = "MainMenu";

    [Header("BarrageTuto only")]
    [Tooltip("Assign the BarrePatience in BarrageTuto. Leave empty in MapTuto.")]
    [SerializeField] private BarrePatience _barrePatience;

    private bool _handled = false;

    private void OnEnable()
    {
        EndManager.OnPlayerDied += OnDeath;

        if (_barrePatience != null)
            _barrePatience.OnPatienceEpuisée += OnDeath;
    }

    private void OnDisable()
    {
        EndManager.OnPlayerDied -= OnDeath;

        if (_barrePatience != null)
            _barrePatience.OnPatienceEpuisée -= OnDeath;
    }

    /// <summary>Immediately loads the menu scene. Guarded so it fires only once.</summary>
    private void OnDeath()
    {
        if (_handled) return;
        _handled = true;

        SceneManager.LoadScene(_menuSceneName);
    }
}
