using UnityEngine;

public class SaveGameSystem : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas playerDatas;

    private void Start()
    {
        LoadSaveGame();
    }
    public void LoadSaveGame()
    {
        playerDatas.LoadDatas();
    }

    public void SaveGame()
    {
        playerDatas.SaveDatas();
    }



    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveGame();
        }
    }
        
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveGame();
    }
    private void OnApplicationQuit()
    {
        SaveGame();
    }

    //APPELER LES SAVE QUAND : achat monnaie (nouvelle valeur), quand changement monnaie in game (fin de niveau), quand achat object (nouvelle monnaie, et nouveau inventaire)
}
