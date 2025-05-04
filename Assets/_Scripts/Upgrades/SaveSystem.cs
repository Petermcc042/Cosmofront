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
    public List<string> unlockedUpgradeNames = new List<string>();
    public List<string> unlockedBuildingNames = new List<string>();

    public int attaniumTotal;
    public int marcumTotal;
    public int imearTotal;

    public int populationTotal;
}

public static class SaveSystem
{
#pragma warning disable UDR0001 // Domain Reload Analyzer
    public static SaveData playerData;
#pragma warning restore UDR0001 // Domain Reload Analyzer


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

    public static void SaveGame()
    {
        string saveFilePath = Application.persistentDataPath + "/savefile.json";
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game file saved to " + saveFilePath);
    }

    public static void InitData() {

        SaveData saveData = new SaveData
        {
            playerLevel = 0,
            generatorHealthIncrease = 0f,
            shieldHealthIncrease = 10,
            playerPosition = new Vector3(1, 2, 3),
            inventoryItems = new List<string> { "Sword", "Shield" },
            unlockedUpgradeNames = new List<string> { "Damage", "Targeting range", "Targeting Rate", "Fire Rate" },
            unlockedBuildingNames = new List<string> { "Command Centre" },
            attaniumTotal = 50,
            marcumTotal = 50,
            imearTotal = 50,
            populationTotal = 50
        };

        playerData = saveData;

        SaveGame();
    }
}