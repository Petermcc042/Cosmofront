using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        Debug.Log(saveFilePath);
    }

    public void SaveGame(SaveData saveData)
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game saved to " + saveFilePath);
    }

    public SaveData LoadGame()
    {
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