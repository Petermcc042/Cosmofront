using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    private void Start()
    {
        // Example: Loading data
        SaveSystem.LoadGame();

        if (SaveSystem.playerData != null)
        {
            Debug.Log("Loaded Data");
        }
        else
        {
            SaveData saveData = new SaveData
            {
                playerLevel = 0,
                generatorHealthIncrease = 0f,
                playerPosition = new Vector3(1, 2, 3),
                inventoryItems = new List<string> { "Sword", "Shield" }
            };
            SaveSystem.SaveGame(saveData);

            SaveSystem.LoadGame();

            if (SaveSystem.playerData == null)
            {
                Debug.Log("FAILED LOAD");
            }
        }
    }

    public enum PlayerUpgradesEnum
    {
        PlayerLevel, GeneratorHealth, ShieldHealth, AttackSpeed, TurretDamage
    }

    public void SaveChanges()
    {
        SaveSystem.SaveGame();
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
                    SaveSystem.playerData.generatorHealthIncrease += GenHealthChange * direction;
                    Debug.Log(SaveSystem.playerData.generatorHealthIncrease);
                    break;
                case PlayerUpgradesEnum.TurretDamage:
                    SaveSystem.playerData.turretDamageIncrease += turretDamageChange * direction;
                    break;
            }
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
                    SaveSystem.playerData.generatorHealthIncrease += GenHealthChange * direction;
                    break;
                case PlayerUpgradesEnum.TurretDamage:
                    SaveSystem.playerData.turretDamageIncrease += turretDamageChange * direction;
                    break;
            }
        }
        else
        {
            Debug.LogError($"Invalid round type: {upgradeType}");
        }
    }
}
