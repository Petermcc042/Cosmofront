using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private string saveFilePath;

    public void SaveGame(SaveData saveData)
    {
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game saved to " + saveFilePath);
    }

    public SaveData LoadGame()
    {
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game loaded from " + saveFilePath);
            return saveData;
        }
        else
        {
            Debug.LogWarning("Save file not found!");
            return null;
        }
    }
}