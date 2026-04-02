using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Barrage.Tutorial
{
    /// <summary>
    /// Shows a congratulations panel when the player presses the Valider button
    /// in the BarrageTuto scene, then loads the main menu when the panel is closed.
    ///
    /// Wire Valider.Button.onClick → OnValiderPressed().
    /// Wire PanelFelicitations/BoutonFermer.Button.onClick → OnPanelFermé().
    /// </summary>
    public class BarrageTutoEndController : MonoBehaviour
    {
        [Header("Panel de félicitations")]
        [Tooltip("Root GameObject of the congratulations panel. Must start inactive.")]
        [SerializeField] private GameObject _panelFelicitations;

        [Header("Navigation")]
        [Tooltip("Name of the main menu scene to load when the player closes the panel.")]
        [SerializeField] private string _nomScèneMenu = "MainMenu";

        [Tooltip("Delay in seconds before loading the menu after the panel is closed.")]
        [SerializeField, Min(0f)] private float _délaiAvantMenu = 0.5f;

        /// <summary>
        /// Called by the Valider button's onClick.
        /// Freezes time and shows the congratulations panel.
        /// </summary>
        public void OnValiderPressed()
        {
            if (_panelFelicitations == null)
            {
                Debug.LogError("[BarrageTutoEndController] _panelFelicitations not assigned — loading menu directly.");
                StartCoroutine(ChargerMenuRoutine());
                return;
            }

            Time.timeScale = 0f;
            _panelFelicitations.SetActive(true);
        }

        /// <summary>
        /// Called by the panel's close button onClick.
        /// Restores time and loads the main menu.
        /// </summary>
        public void OnPanelFermé()
        {
            if (_panelFelicitations != null)
                _panelFelicitations.SetActive(false);

            Time.timeScale = 1f;
            StartCoroutine(ChargerMenuRoutine());
        }

        private IEnumerator ChargerMenuRoutine()
        {
            if (_délaiAvantMenu > 0f)
                yield return new WaitForSeconds(_délaiAvantMenu);

            SceneManager.LoadScene(_nomScèneMenu);
        }

        private void OnDestroy()
        {
            // Safety: always restore time scale if the scene unloads mid-panel.
            Time.timeScale = 1f;
        }
    }
}
