using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SO_PlayerDatas;

[System.Serializable]

public class PlayerDatas
{
    public string Name = "ErrorName404";
    public int Score1 = 0;
    public int Score2 = 0;
    public int Score3 = 0;
    public int Level = 1;
    public List<MiniGameHighScores> allHighScores = new List<MiniGameHighScores>();
}

public class SaveController
{
    public string GetPath()
    {
        return Application.persistentDataPath + "/save.json";
    }

    public void Save(PlayerDatas datas)
    {
        string json = JsonUtility.ToJson(datas, prettyPrint: true);
        File.WriteAllText(GetPath(), contents: json);
    }

    public PlayerDatas Load()
    {
        string path = GetPath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerDatas>(json);
        }

        return new PlayerDatas();
    }
}
