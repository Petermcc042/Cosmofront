using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int playerLevel;
    public float generatorHealthIncrease;
    public int turretDamageIncrease;
    public Vector3 playerPosition;
    public List<string> inventoryItems = new List<string>();
}

public class PlayerUpgrades : MonoBehaviour
{
    [SerializeField] private SaveSystem saveSystem;
    private SaveData playerData;

    [SerializeField] private TextMeshProUGUI generatorHealthUI;
    [SerializeField] private TextMeshProUGUI turretDamageUI;

    private void Start()
    {
        // Example: Loading data
        playerData = saveSystem.LoadGame();

        if (playerData != null)
        {
            Debug.Log("Loaded Data");
        }
        else
        {
            SaveData saveData = new SaveData
            {
                playerLevel = 5,
                generatorHealthIncrease = 100.0f,
                playerPosition = new Vector3(1, 2, 3),
                inventoryItems = new List<string> { "Sword", "Shield" }
            };
            saveSystem.SaveGame(saveData);

            playerData = saveSystem.LoadGame();
        }

        UpdateAllUI();
    }

    public enum PlayerUpgradesEnum
    {
        PlayerLevel, GeneratorHealth, AttackSpeed, TurretDamage
    }

    public void SaveChanges()
    {
        saveSystem.SaveGame(playerData);
    }

    private void UpdateAllUI()
    {
        generatorHealthUI.text = playerData.generatorHealthIncrease.ToString();
        turretDamageUI.text = playerData.turretDamageIncrease.ToString()+"%";
    }


    private int GenHealthChange = 10;
    private int turretDamageChange = 5;

    public void UpgradePlayerAddition(string upgrade)
    {
        int direction = 1;

        if (Enum.TryParse(upgrade, true, out PlayerUpgradesEnum upgradeType))
        {
            Debug.Log(upgradeType);
            switch (upgradeType)
            {
                case PlayerUpgradesEnum.GeneratorHealth:
                    playerData.generatorHealthIncrease += GenHealthChange * direction;
                    break;
                case PlayerUpgradesEnum.TurretDamage:
                    playerData.turretDamageIncrease += turretDamageChange * direction;
                    break;
            }


            UpdateAllUI();
        }
        else
        {
            Debug.LogError($"Invalid round type: {upgradeType}");
        }
    }

    public void UpgradePlayerSubtraction(string upgrade)
    {
        int direction = -1;

        if (Enum.TryParse(upgrade, true, out PlayerUpgradesEnum upgradeType))
        {
            Debug.Log(upgradeType);
            switch (upgradeType)
            {
                case PlayerUpgradesEnum.GeneratorHealth:
                    playerData.generatorHealthIncrease += GenHealthChange * direction;
                    break;
                case PlayerUpgradesEnum.TurretDamage:
                    playerData.turretDamageIncrease += turretDamageChange * direction;
                    break;
            }


            UpdateAllUI();
        }
        else
        {
            Debug.LogError($"Invalid round type: {upgradeType}");
        }
    }
}
