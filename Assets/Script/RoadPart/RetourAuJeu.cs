// GlobalMenu.cs — stop coroutine avant relance
using System.Collections;
using UnityEngine;

public class RetourAuJeu : MonoBehaviour
{
    [SerializeField] private GameObject _SizeToUp;
    [SerializeField] private GameObject _unHideIt;
    [SerializeField] private EndManager _endManager;

    private Vector3 _originalScale;
    private Coroutine _scaleCoroutine;

    private void Start() => _originalScale = _SizeToUp.transform.localScale;

    public void OpenMenu()
    {
        _unHideIt.SetActive(true);
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(LerpScale(0.1f));
    }

    public void PauseGame()
    {
        _endManager.StopGame();
    }

    public void UnPauseGame()
    {
        _endManager.RestartGame();
    }

    public void CloseMenu() => _unHideIt.SetActive(false);

    private IEnumerator LerpScale(float duration)
    {
        Vector3 fromScale = _originalScale * 0.85f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _SizeToUp.transform.localScale = Vector3.Lerp(
                fromScale, _originalScale, EaseOutCubic(elapsed / duration));
            yield return null;
        }
        _SizeToUp.transform.localScale = _originalScale;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}