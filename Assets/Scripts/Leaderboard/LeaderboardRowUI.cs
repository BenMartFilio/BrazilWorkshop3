using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Représente une ligne dans le panneau de classement.
/// À placer sur le prefab de ligne du leaderboard.
/// </summary>
public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private Image _backgroundImage;

    [Header("Fond de ligne")]
    [SerializeField] private Color _defaultRowColor = new Color(1f, 1f, 1f, 0.04f);
    [SerializeField] private Color _playerHighlightColor = new Color(1f, 0.9f, 0.2f, 0.25f);

    /// <summary>Remplit la ligne avec les données d'une entrée du classement.</summary>
    public void Populate(int rank, string playerName, double score, bool isCurrentPlayer, Color tierColor)
    {
        _rankText.SetText("#{0}", rank);
        _playerNameText.text = string.IsNullOrEmpty(playerName) ? "—" : playerName;
        _scoreText.SetText("{0:0}", (int)score);

        _rankText.color = tierColor;
        _scoreText.color = tierColor;

        if (_backgroundImage != null)
            _backgroundImage.color = isCurrentPlayer ? _playerHighlightColor : _defaultRowColor;
    }
}
