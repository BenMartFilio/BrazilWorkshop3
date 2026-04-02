using UnityEngine;
using Barrage.UI;

namespace Barrage.Tutorial
{
    /// <summary>
    /// Shows a one-shot tutorial panel when BarrageTuto loads.
    /// The panel pauses the game (Time.timeScale = 0) until the player dismisses it,
    /// then hands control back to the normal barrage flow (BarrePatience unfreezes via
    /// MainDuGardeUI.Start which already calls Dégeler()).
    ///
    /// Attach to any persistent GameObject in BarrageTuto.
    /// Wire _panelRoot to the tutorial panel GameObject (set inactive in the scene).
    /// </summary>
    [DefaultExecutionOrder(100)]   // runs after MainDuGardeUI (-50) so patience is already handled
    public class BarrageTutoPanelController : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("Root GameObject of the tutorial panel. Must start inactive in the scene.")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Patience reference")]
        [Tooltip("BarrePatience to keep frozen while the panel is shown, " +
                 "and to unfreeze when the player closes it.")]
        [SerializeField] private BarrePatience _barrePatience;

        private void Start()
        {
            if (_panelRoot == null)
            {
                Debug.LogError("[BarrageTutoPanelController] _panelRoot not assigned — tutorial panel skipped.");
                return;
            }

            // Freeze time and patience while the panel is up.
            Time.timeScale = 0f;
            if (_barrePatience != null)
                _barrePatience.Geler();

            _panelRoot.SetActive(true);
        }

        /// <summary>
        /// Called by the panel's close button (wired via Button.onClick in the Inspector).
        /// Resumes time and unfreezes patience so the barrage begins normally.
        /// </summary>
        public void OnPanelClosed()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            Time.timeScale = 1f;

            if (_barrePatience != null)
                _barrePatience.Dégeler();
        }

        private void OnDestroy()
        {
            // Safety: always restore time scale if the scene is unloaded mid-panel.
            Time.timeScale = 1f;
        }
    }
}
