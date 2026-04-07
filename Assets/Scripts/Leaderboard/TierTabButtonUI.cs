using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bouton onglet représentant un tier dans le panneau de classement.
/// </summary>
public class TierTabButtonUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _buttonBackground;
    [SerializeField] private Image _selectionIndicator;

    [Header("Couleurs d'état")]
    [SerializeField] private Color _activeBackgroundColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color _inactiveBackgroundColor = new Color(1f, 1f, 1f, 0.04f);

    [Header("Animation icône")]
    [SerializeField] private float _activeIconScale = 1.25f;
    [SerializeField] private float _inactiveIconScale = 1f;
    [SerializeField] private float _iconScaleDuration = 0.15f;

    private Coroutine _scaleCoroutine;

    /// <summary>Configure le bouton à partir d'un TierDefinition.</summary>
    public void Setup(TierDefinition tier)
    {
        if (_icon != null)
        {
            _icon.sprite = tier.icon;
            _icon.color = tier.color;
            _icon.gameObject.SetActive(tier.icon != null);
        }

        if (_selectionIndicator != null)
            _selectionIndicator.color = tier.color;
    }

    /// <summary>Met à jour l'apparence selon l'état actif/inactif.</summary>
    public void SetActive(bool isActive)
    {
        if (_buttonBackground != null)
            _buttonBackground.color = isActive ? _activeBackgroundColor : _inactiveBackgroundColor;

        if (_selectionIndicator != null)
            _selectionIndicator.gameObject.SetActive(isActive);

        if (_icon != null)
        {
            float targetScale = isActive ? _activeIconScale : _inactiveIconScale;
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(AnimateIconScale(targetScale));
        }
    }

    private IEnumerator AnimateIconScale(float targetScale)
    {
        Vector3 fromScale = _icon.rectTransform.localScale;
        Vector3 toScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < _iconScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / _iconScaleDuration);
            _icon.rectTransform.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        _icon.rectTransform.localScale = toScale;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}
