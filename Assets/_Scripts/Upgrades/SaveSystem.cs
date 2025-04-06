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
    public static SaveData playerData;

    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        playerData = new SaveData();
        LoadGame();
    }

    public static void SaveGame()
    {
        string saveFilePath = Application.persistentDataPath + "/savefile.json";
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game file saved to " + saveFilePath);
        Debug.Log(playerData.shieldHealthIncrease);
    }

    public static void LoadGame()
    {
        string saveFilePath = Application.persistentDataPath + "/savefile.json";
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("loading file from " + saveFilePath);
            playerData = saveData;
        }
        else
        {
            InitData();
            Debug.LogWarning("Save file not found! Adding Data aaa");
        }
    }

    public static void InitData() {

        SaveData saveData = new SaveData
        {
            playerLevel = 0,
            generatorHealthIncrease = 0f,
            shieldHealthIncrease = 10,
            playerPosition = new Vector3(1, 2, 3),
            inventoryItems = new List<string> { "Sword", "Shield" }
        };
        playerData = saveData;
    }
}