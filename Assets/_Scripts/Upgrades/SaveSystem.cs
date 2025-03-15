using System.IO;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int playerLevel;
    public float generatorHealthIncrease;
    public int shieldHealthIncrease;

    public int turretDamageIncrease;
    public Vector3 playerPosition;
    public List<string> inventoryItems = new List<string>();
}

public static class SaveSystem
{   
    private static string saveFilePath;
    public static SaveData playerData;

    public static void SaveGame()
    {
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game file saved to " + saveFilePath);
    }

    public static void LoadGame()
    {
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("loading file from " + saveFilePath);
            playerData = saveData;
        }
        else
        {
            Debug.LogWarning("Save file not found!");
        }
    }

    public static void InitData() {
        SaveData saveData = new SaveData
        {
            playerLevel = 0,
            generatorHealthIncrease = 0f,
            playerPosition = new Vector3(1, 2, 3),
            inventoryItems = new List<string> { "Sword", "Shield" }
        };
        SaveSystem.SaveGame();
    }
}