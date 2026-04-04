using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SO_PlayerDatas;
using System.Security.Cryptography;
using System.Text;


[System.Serializable]
public class CosmetiqueEntry
{
    public string identifiant;
    public bool estAchete;

    public CosmetiqueEntry(string identifiant, bool estAchete = false)
    {
        this.identifiant = identifiant;
        this.estAchete = estAchete;
    }
}


[System.Serializable]
public class PlayerDatas
{
    public string Name = "Player";
    public int BestScore = 0;
    public int Level = 1;
    public List<MiniGameHighScores> allHighScores = new List<MiniGameHighScores>();
    public List<InventoryObject> allObjectInInventory = new List<InventoryObject>();
    public float generalVolume = 1f;
    public float musicVolume = 1f;
    public float SFXVolume = 1f;
    public int generalMonney = 10000;
    public int premiumMonney = 100;
    public int skinEquiped = 0;
    public List<CosmetiqueEntry> cosmetiquesInventaire = new List<CosmetiqueEntry>();

    public string hash;
}

public class SaveController
{
    private const string SECRET_KEY = "Workshop_3_Pr0jectMobile";
    private static readonly string AES_KEY = "12345678901234567890123456789012"; // 32 chars
    private static readonly string AES_IV = "1234567890123456"; // 16 chars

    public string GetPath()
    {
        return Application.persistentDataPath + "/save.json";
    }

    public void Save(PlayerDatas datas)
    {
        datas.hash = GenerateHash(datas);

        string json = JsonUtility.ToJson(datas);

        string encrypted = Encrypt(json);

        File.WriteAllText(GetPath(), encrypted);
    }

    public PlayerDatas Load()
    {
        string path = GetPath();

        if (!File.Exists(path))
            return new PlayerDatas();

        try
        {
            string encrypted = File.ReadAllText(path);

            string json = Decrypt(encrypted);

            PlayerDatas data = JsonUtility.FromJson<PlayerDatas>(json);

            string expectedHash = GenerateHash(data);

            if (data.hash != expectedHash)
            {
                Debug.LogWarning("Cheat détecté !");
                return new PlayerDatas();
            }

            return data;
        }
        catch
        {
            Debug.LogWarning("Save corrompue !");
            return new PlayerDatas();
        }
    }

    private string GenerateHash(PlayerDatas data)
    {
        // On clone pour ne PAS inclure le hash lui-même
        PlayerDatas clone = JsonUtility.FromJson<PlayerDatas>(JsonUtility.ToJson(data));
        clone.hash = "";

        string json = JsonUtility.ToJson(clone);

        string finalString = SECRET_KEY + json;

        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(finalString));

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }
    }


    private string Encrypt(string plainText)
    {
        using (var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = System.Text.Encoding.UTF8.GetBytes(AES_KEY);
            aes.IV = System.Text.Encoding.UTF8.GetBytes(AES_IV);

            var encryptor = aes.CreateEncryptor();

            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return System.Convert.ToBase64String(encrypted);
        }
    }

    private string Decrypt(string cipherText)
    {
        using (var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = System.Text.Encoding.UTF8.GetBytes(AES_KEY);
            aes.IV = System.Text.Encoding.UTF8.GetBytes(AES_IV);

            var decryptor = aes.CreateDecryptor();

            byte[] cipherBytes = System.Convert.FromBase64String(cipherText);
            byte[] decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return System.Text.Encoding.UTF8.GetString(decrypted);
        }
    }
}


