using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class MiniGameHighScores
{
    public List<HighScoreEntry> highScores = new List<HighScoreEntry>();
}
[System.Serializable]
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
        // donc je dois cr�er un playerdatas � partir de mon so
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
        // j'envoie �a � la fonction save de savesystem
        saveSystem.Save(datas);
    }

    /// <summary>Retourne la liste plate de toutes les InventoryEntry regroupées dans allObjectInInventory.</summary>
    public List<InventoryEntry> ObtenirInventairePlat()
    {
        List<InventoryEntry> plat = new List<InventoryEntry>();
        foreach (InventoryObject groupe in allObjectInInventory)
        {
            if (groupe != null)
                plat.AddRange(groupe.highScores);
        }
        return plat;
    }

    private void CheckSaveSystem()
    {
        // v�rifier si savesystem contient un objet du type savesystem
        if (saveSystem == null)
        {
            // s'il n'y a rien, j'en cr�e (instancie) un.
            saveSystem = new SaveController();
        }
    }
}
