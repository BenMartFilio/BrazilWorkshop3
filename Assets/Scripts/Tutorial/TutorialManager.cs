using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Central orchestrator for the MapTuto tutorial flow.
/// Controls panel sequencing, pause/resume, and the final fake-barrage transition to BarrageTuto.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Spawner")]
    [SerializeField] private TutoSpawnerSequential _spawner;

    [Header("Game References")]
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GoundMouvement[] _grounds;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private BarreProgressionBarrage _barreProgression;

    [Header("Tutorial Panels")]
    [SerializeField] private GameObject _panelGoal;         // Panel 1: goal of the game
    [SerializeField] private GameObject _panelSwipe;        // Panel 2: swipe to move
    [SerializeField] private GameObject _panelDocument;     // Panel 3: fichedeposte1
    [SerializeField] private GameObject _panelSliderInfo;   // Panel 4: SliderBarrage explanation

    [Header("Slider Highlight")]
    [Tooltip("Semi-transparent dark overlay that covers the whole screen, shown with the slider info panel.")]
    [SerializeField] private GameObject _sliderHighlightOverlay;

    [Header("Slider Tutorial Animation")]
    [Tooltip("Animates the SliderBarrage fill when PanelSliderInfo is shown.")]
    [SerializeField] private TutoSliderAnimator _tutoSliderAnimator;

    [Header("Transition")]
    [SerializeField] private float _fakeBrrageFadeDuration = 2f;
    [Tooltip("Name of the scene to load after the tutorial.")]
    [SerializeField] private string _barrageSceneName = "BarrageTuto";
    [SerializeField] private Image _fadeImage;              // full-screen black image

    // PaternTuto3 is at index 4 in the tutorialPatterns array (0-based):
    // PaternEmpty(0), PaternEmpty(1), PaternTuto1(2), PaternTuto2(3), PaternTuto3(4)
    private const int PaternTuto3Index = 4;

    private bool _sequenceComplete = false;
    private Coroutine _swipeDelayCoroutine;
    private Coroutine _finalPanelCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        ValidateReferences();

        // Show goal panel and freeze gameplay immediately.
        if (_panelGoal != null)
        {
            _panelGoal.SetActive(true);
        }
        else
        {
            Debug.LogError("[TutorialManager] _panelGoal is null — skipping to gameplay.");
            BeginGameplay();
        }

        // Do NOT start the spawner yet — wait for the player to close the goal panel.
        PauseGame();
    }

    private void OnDestroy()
    {
        if (_spawner != null)
        {
            _spawner.OnPatternAboutToStart -= OnPatternAboutToStart;
            _spawner.OnSequenceComplete    -= OnSequenceComplete;
        }
    }

    // ── Panel close callbacks (wired via Button.onClick in Inspector) ──────────

    /// <summary>Called by the close button on _panelGoal.</summary>
    public void OnGoalPanelClosed()
    {
        if (_panelGoal != null) _panelGoal.SetActive(false);
        BeginGameplay();
    }

    /// <summary>Called by the close button on _panelSwipe.</summary>
    public void OnSwipePanelClosed()
    {
        if (_panelSwipe != null) _panelSwipe.SetActive(false);
        ResumeGame();
    }

    /// <summary>Called by the close button on _panelDocument.</summary>
    public void OnDocumentPanelClosed()
    {
        if (_panelDocument != null) _panelDocument.SetActive(false);
        ResumeGame();
    }

    /// <summary>Called by the close button on _panelSliderInfo.</summary>
    public void OnSliderInfoPanelClosed()
    {
        if (_panelSliderInfo != null) _panelSliderInfo.SetActive(false);
        if (_sliderHighlightOverlay != null) _sliderHighlightOverlay.SetActive(false);
        if (_tutoSliderAnimator != null) _tutoSliderAnimator.StopAnimation();
        StartCoroutine(FakeBarrageTransition());
    }

    // ── Gameplay control ──────────────────────────────────────────────────────

    private void BeginGameplay()
    {
        // Subscribe before ResumeGame so no event is missed when the spawner starts.
        _spawner.OnPatternAboutToStart += OnPatternAboutToStart;
        _spawner.OnSequenceComplete    += OnSequenceComplete;

        ResumeGame();

        _swipeDelayCoroutine = StartCoroutine(PanelSwipeDelayRoutine());
    }

    /// <summary>Pauses spawner, grounds, time and player movement.</summary>
    private void PauseGame()
    {
        if (_swipeDelayCoroutine != null)
        {
            StopCoroutine(_swipeDelayCoroutine);
            _swipeDelayCoroutine = null;
        }

        if (_timeManager != null)  _timeManager.StopTime();
        if (_spawner != null)      _spawner.StopSpawning();

        if (_grounds != null)
        {
            foreach (GoundMouvement ground in _grounds)
            {
                if (ground != null) ground.StopMove();
            }
        }

        if (_playerMovement != null) _playerMovement.StopMove();
    }

    /// <summary>Resumes spawner, grounds, time and player movement.</summary>
    private void ResumeGame(bool resumePlayer = true)
    {
        if (_timeManager != null) _timeManager.StartTime();

        if (_grounds != null)
        {
            foreach (GoundMouvement ground in _grounds)
            {
                if (ground != null) ground.StartMove();
            }
        }

        // resumePlayer = false during FakeBarrageTransition so MoveUp() is not cancelled.
        if (resumePlayer && _playerMovement != null) _playerMovement.StartMove();

        // Only resume the spawner if the sequence is not yet complete.
        if (_spawner != null && !_sequenceComplete)
            _spawner.StartSpawning();
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator PanelSwipeDelayRoutine()
    {
        yield return new WaitForSeconds(5f);
        _swipeDelayCoroutine = null;
        PauseGame();
        if (_panelSwipe != null) _panelSwipe.SetActive(true);
    }

    private IEnumerator FakeBarrageTransition()
    {
        // Resume grounds and time, but do NOT call StartMove() — MoveUp handles the player.
        ResumeGame(resumePlayer: false);

        // Move the player upward during the fade.
        if (_playerMovement != null)
            _playerMovement.MoveUp(_fakeBrrageFadeDuration);

        // Fade to black.
        if (_fadeImage != null)
        {
            _fadeImage.gameObject.SetActive(true);
            Color color = _fadeImage.color;
            color.a = 0f;
            _fadeImage.color = color;

            float elapsed = 0f;
            while (elapsed < _fakeBrrageFadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / _fakeBrrageFadeDuration);
                _fadeImage.color = color;
                yield return null;
            }

            color.a = 1f;
            _fadeImage.color = color;
        }
        else
        {
            yield return new WaitForSeconds(_fakeBrrageFadeDuration);
        }

        SceneManager.LoadScene(_barrageSceneName);
    }

    // ── Event callbacks from TutoSpawnerSequential ────────────────────────────

    private void OnPatternAboutToStart(int index)
    {
        if (index == PaternTuto3Index)
        {
            PauseGame();
            if (_panelDocument != null) _panelDocument.SetActive(true);
        }
    }

    private void OnSequenceComplete()
    {
        _sequenceComplete = true;
        // The looping pattern already required a successful document collection to exit,
        // so we can go straight to the final panel delay without waiting for another collection.
        if (_finalPanelCoroutine != null)
            StopCoroutine(_finalPanelCoroutine);

        _finalPanelCoroutine = StartCoroutine(FinalPanelDelayRoutine());
    }

    private IEnumerator FinalPanelDelayRoutine()
    {
        yield return new WaitForSeconds(3f);
        _finalPanelCoroutine = null;
        PauseGame();
        if (_panelSliderInfo != null)        _panelSliderInfo.SetActive(true);
        if (_sliderHighlightOverlay != null) _sliderHighlightOverlay.SetActive(true);
        if (_tutoSliderAnimator != null)     _tutoSliderAnimator.StartAnimation();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private void ValidateReferences()
    {
        if (_spawner == null)
            Debug.LogError("[TutorialManager] _spawner is not assigned.");
        if (_timeManager == null)
            Debug.LogError("[TutorialManager] _timeManager is not assigned.");
        if (_grounds == null || _grounds.Length == 0)
            Debug.LogError("[TutorialManager] _grounds is not assigned or empty.");
        if (_playerMovement == null)
            Debug.LogError("[TutorialManager] _playerMovement is not assigned.");
        if (_panelGoal == null)
            Debug.LogError("[TutorialManager] _panelGoal is not assigned.");
        if (_panelSwipe == null)
            Debug.LogError("[TutorialManager] _panelSwipe is not assigned.");
        if (_panelDocument == null)
            Debug.LogError("[TutorialManager] _panelDocument is not assigned.");
        if (_panelSliderInfo == null)
            Debug.LogError("[TutorialManager] _panelSliderInfo is not assigned.");
        if (_tutoSliderAnimator == null)
            Debug.LogWarning("[TutorialManager] _tutoSliderAnimator is not assigned — slider will not animate during tutorial.");
        if (_fadeImage == null)
            Debug.LogError("[TutorialManager] _fadeImage is not assigned.");
    }
}
