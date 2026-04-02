using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class SaveGameSystem : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas playerDatas;
    private static SaveGameSystem instance;
    private static readonly object lockObj = new object();
    public static event Action OnSaveLoaded;

    public static SaveGameSystem Instance
    {
        get
        {
            lock (lockObj) { return instance; }
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

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticateAsync();

            LoadSaveGame(); // local d'abord

            await CloudLoadAsync(); // écrase si cloud plus récent
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"Erreur d'authentification : {e.Message}");
            return;
        }
        catch (CloudSaveException e)
        {
            Debug.LogWarning($"Cloud save indisponible, save locale conservée : {e.Message}");
            // on continue — la save locale est déjà chargée
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur inattendue : {e.Message}");
            return;
        }

        OnSaveLoaded?.Invoke();
    }

    // --- Auth ---

    private async System.Threading.Tasks.Task AuthenticateAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Récupère le pseudo plateforme (Game Center, Google Play, etc.)
        string playerName = AuthenticationService.Instance.PlayerName;
        if (!string.IsNullOrEmpty(playerName))
            playerDatas.Name = playerName; 
    }

    // --- Cloud Save ---

    private async System.Threading.Tasks.Task CloudLoadAsync()
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAllAsync();

        if (data.TryGetValue("playerDatas", out var item))
        {
            string json = item.Value.GetAsString();
            JsonUtility.FromJsonOverwrite(json, playerDatas);
            Debug.Log("Cloud save chargée.");
        }
        else
        {
            Debug.Log("Aucune cloud save trouvée, save locale conservée.");
        }
    }

    public async System.Threading.Tasks.Task CloudSaveAsync()
    {
        var data = new Dictionary<string, object>
        {
            { "playerDatas", JsonUtility.ToJson(playerDatas) }
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        Debug.Log("Cloud save effectuée.");
    }

    // --- Local Save ---

    public void LoadSaveGame() => playerDatas.LoadDatas();
    public void SaveGame() => playerDatas.SaveDatas();

    // --- Lifecycle ---

    // --- Lifecycle ---
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveGame();              // local immédiat (synchrone, safe)
            _ = CloudSaveAsync();   // cloud best-effort (fire & forget)
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            SaveGame();
            _ = CloudSaveAsync();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame(); // uniquement local ici — l'app coupe trop vite pour attendre le cloud
    }

    //APPELER LES SAVE QUAND : achat monnaie (nouvelle valeur), quand changement monnaie in game (fin de niveau), quand achat object (nouvelle monnaie, et nouveau inventaire)
}