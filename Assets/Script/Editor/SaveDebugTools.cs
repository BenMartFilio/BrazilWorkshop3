#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class SaveDebugTools
{
    [MenuItem("Debug/Reset complet (local + cloud)")]
    public static void FullReset()
    {
        // 1. Supprime le fichier local
        string path = Application.persistentDataPath + "/save.json";
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save locale supprimée.");
        }

        // 2. Supprime le cloud
        if (SaveGameSystem.Instance != null)
        {
            _ = SaveGameSystem.Instance.CloudDeleteAsync();
        }
        else
        {
            Debug.LogWarning("SaveGameSystem introuvable — lance le jeu avant de reset le cloud.");
        }

        // 3. Supprime les PlayerPrefs au cas où
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("Reset complet effectué.");
    }

    [MenuItem("Debug/Ouvrir le dossier de save")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif