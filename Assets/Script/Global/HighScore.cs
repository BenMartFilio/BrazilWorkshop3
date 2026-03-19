using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HighScoreEntry
{
    public string playerName;
    public int score;
    public HighScoreEntry(string name, int score)
    {
        this.playerName = name;
        this.score = score;
    }
}

[System.Serializable]
public class HighScoreList
{
    public List<HighScoreEntry> highScores = new List<HighScoreEntry>();
}

public enum MiniGameType
{
    GameAndWatch,
    PizzaTime,
    DeliveryTime
}
