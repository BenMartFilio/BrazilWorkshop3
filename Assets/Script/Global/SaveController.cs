using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SO_PlayerDatas;

[System.Serializable]

public class PlayerDatas
{
    public string Name = "ErrorName404";
    public int BestScore = 0;
    public int Score2 = 0;
    public int Score3 = 0;
    public int Level = 1;
    public List<MiniGameHighScores> allHighScores = new List<MiniGameHighScores>();
    public List<InventoryObject> allObjectInInventory = new List<InventoryObject>();
    public float generalVolume;
    public float musicVolume;
    public float SFXVolume;
    public int generalMonney;
    public int premiumMonney;

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
