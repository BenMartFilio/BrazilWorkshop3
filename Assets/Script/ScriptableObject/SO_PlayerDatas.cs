using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class MiniGameHighScores
{
    public List<HighScoreEntry> highScores = new List<HighScoreEntry>();
}
public class InventoryObject
{
    public List<InventoryEntry> highScores = new List<InventoryEntry>();
}

[CreateAssetMenu(fileName = "SO_PlayerDatas", menuName = "Scriptable Objects/SO_PlayerDatas")]
public class SO_PlayerDatas : ScriptableObject
{
    public string Name;
    public int BestScore;
    public int Level;
    public float generalVolume;
    public float musicVolume;
    public float SFXVolume;
    public int generalMonney;
    public int premiumMonney;
    public int actualScoreNotSaved;
    public int actualCoinsNotSaved;
    public bool isAnHighScore;


    public List<MiniGameHighScores> allHighScores = new List<MiniGameHighScores>();
    public List<InventoryObject> allObjectInInventory = new List<InventoryObject>();



    private SaveController saveSystem;

    public void LoadDatas()
    {
        CheckSaveSystem();
        // utiliser la fonction de savesystem pour load les datas
        // cette fonction renvoie des playersdatas 
        PlayerDatas datas = saveSystem.Load();
        // donc je dois les affecter aux variables de mon scriptable object
        Name = datas.Name;
        BestScore = datas.BestScore;
        Level = datas.Level;
        allHighScores = datas.allHighScores;
        allObjectInInventory = datas.allObjectInInventory;
        generalVolume = datas.generalVolume;
        musicVolume = datas.musicVolume;
        SFXVolume = datas.SFXVolume;
        generalMonney = datas.generalMonney;
        premiumMonney = datas.premiumMonney;
    }

    public void SaveDatas()
    {
        CheckSaveSystem();
        // pour utiliser la fonction save de savesystem j'ai besoin de playerdatas
        // donc je dois créer un playerdatas à partir de mon so
        PlayerDatas datas = new PlayerDatas();
        datas.Name = Name;
        datas.BestScore = BestScore;
        datas.Level = Level;
        datas.allHighScores = allHighScores;
        datas.allObjectInInventory = allObjectInInventory;
        datas.generalVolume = generalVolume;
        datas.musicVolume = musicVolume;
        datas.SFXVolume = SFXVolume;
        datas.generalMonney = generalMonney;
        datas.premiumMonney = premiumMonney;
        // j'envoie ça à la fonction save de savesystem
        saveSystem.Save(datas);
    }

    private void CheckSaveSystem()
    {
        // vérifier si savesystem contient un objet du type savesystem
        if (saveSystem == null)
        {
            // s'il n'y a rien, j'en crée (instancie) un.
            saveSystem = new SaveController();
        }
    }
}
