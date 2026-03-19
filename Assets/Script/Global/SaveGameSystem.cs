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

    //s'inscrire à quand l'application se ferme, pour sauvegarder à ce moment là
}
