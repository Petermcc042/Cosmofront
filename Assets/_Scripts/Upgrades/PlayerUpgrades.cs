using System;
using System.Collections.Generic;
using UnityEngine;
public enum PlayerUpgradesEnum
{
    PlayerLevel, GeneratorHealth, ShieldHealth, AttackSpeed, TurretDamage
}


public class PlayerUpgrades : MonoBehaviour
{
    private int GenHealthChange = 10;
    private int turretDamageChange = 5;


    public void SavePlayerData()
    {
        SaveSystem.SaveGame();
    }

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
            }
        }
        else
        {
            Debug.LogError($"Invalid round type: {upgradeType}");
        }
    }
}
