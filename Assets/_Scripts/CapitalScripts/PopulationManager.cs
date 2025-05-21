using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;


public enum Profession
{
    civilian,
    engineer,
    scientist,
    soldier
}

[System.Serializable]
public struct PopulationCharacter
{
    public string firstName;
    public string lastName;
    public Profession profession;
    public int level;
}

public class PopulationManager : MonoBehaviour
{
    [Header("Home Screen Menu UI")]
    [SerializeField] private TextMeshProUGUI populationCountUI;
    public List<PopulationCharacter> allCharacters;

    private string[] firstNames;
    private string[] lastNames;

    private float timer = 1f;

    private void Awake()
    {
        allCharacters = new List<PopulationCharacter>();

        firstNames = LoadNames("first-names.json");
        lastNames = LoadNames("names.json");
    }

    private string[] LoadNames(string _fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, _fileName);

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            return JsonUtilityWrapper.FromJsonArray<string>(jsonContent);
        }
        else
        {
            Debug.LogError("Names File not found: " + filePath);
            return null;
        }
    }

    public void UpdatePopulation()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            for (int i = 0; i < SaveSystem.playerData.civiliansPerSecond; i++)
            {
                PopulationCharacter tempCharacter = new PopulationCharacter()
                {
                    firstName = firstNames[UnityEngine.Random.Range(0, firstNames.Length)],
                    lastName = lastNames[UnityEngine.Random.Range(0, lastNames.Length)],
                    profession = Profession.civilian,
                    level = 1
                };

                allCharacters.Add(tempCharacter);
            }
            SaveSystem.playerData.populationTotal += SaveSystem.playerData.civiliansPerSecond;
            populationCountUI.text = SaveSystem.playerData.populationTotal.ToString();
            timer = 1f;

            Debug.Log(allCharacters[allCharacters.Count - 1].firstName + ":" + allCharacters[allCharacters.Count - 1].lastName + ":"+ allCharacters[allCharacters.Count - 1].profession);
        }
    }

    public void PickTroops(int topX)
    {
        // Sort by level descending, then optionally by profession or name
        List<PopulationCharacter> topCharacters = allCharacters
            .OrderByDescending(c => c.level)
            .ThenBy(c => c.lastName) // optional: tie breaker
            .ThenBy(c => c.firstName)
            .Take(topX)
            .ToList();

        SaveSystem.playerData.committedPopulation = topCharacters;
        SaveSystem.SaveGame();
    }
}


public static class JsonUtilityWrapper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    public static T[] FromJsonArray<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }
}