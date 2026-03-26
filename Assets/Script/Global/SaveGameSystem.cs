using UnityEngine;

public class SaveGameSystem : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas playerDatas;
    private static SaveGameSystem instance;
    private static readonly object lockObj = new object(); 
    public static event System.Action OnSaveLoaded;

    public static SaveGameSystem Instance
    {
        get
        {
            lock (lockObj)
            {
                return instance;
            }
        }
    }
    private void Awake()
    {
        Application.targetFrameRate = 60;
        lock (lockObj)
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    private void Start()
    {
        LoadSaveGame();
        OnSaveLoaded?.Invoke();
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
