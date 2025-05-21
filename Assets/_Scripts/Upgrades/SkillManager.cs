using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics.Geometry;
using UnityEngine;

public class SkillManager: MonoBehaviour
{ 
    public int updatedBuildingID;
    public string updatedSkillName;
    public int updatedBuildingPath;

    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private GameObject rightPanelUI;
    [SerializeField] private TextMeshProUGUI turretNameUI;
    [SerializeField] private TextMeshProUGUI bulletDamageUI;
    [SerializeField] private TextMeshProUGUI killCountUI;
    [SerializeField] private TextMeshProUGUI fireRateUI;
    [SerializeField] private TextMeshProUGUI turretLevelUI;
    [SerializeField] private TextMeshProUGUI fireRateUpgradeLevel;
    [SerializeField] private TextMeshProUGUI damageUpgradeLevel;
    [SerializeField] private TextMeshProUGUI targetRateUpgradeLevel;
    [SerializeField] private TextMeshProUGUI targetRangeUpgradeLevel;
    [SerializeField] private List<TextMeshProUGUI> buttonsText;

    private List<string> unlockedUpgradeNames;

#pragma warning disable UDR0001 // Domain Reload Analyzer
    private static List<IUpgradeOption> smallUpgrades = new List<IUpgradeOption>();
    private static List<IUpgradeOption> largeUpgrades = new List<IUpgradeOption>();
    private static List<IUpgradeOption> optionUpgrades = new List<IUpgradeOption>();
    private Turret currentTurret;
    private int currentPosition;

#pragma warning restore UDR0001 // Domain Reload Analyzer

    private void Awake()
    {
        for (int i = 0; i < 5; i++)
        {
            smallUpgrades.Add(new FireRateUpgrade(i/4));
            smallUpgrades.Add(new TargetRangeUpgrade(i));
            smallUpgrades.Add(new DamageUpgrade(i));
            smallUpgrades.Add(new TargetingRateUpgrade(i / 10));
        }

        for (int i = 0; i < 4; i++)
        {
            largeUpgrades.Add(new OrbitalStrikeUpgrade());
            largeUpgrades.Add(new MeteorShowerUpgrade());
            largeUpgrades.Add(new FirestormUpgrade());
            largeUpgrades.Add(new TimewarpUpgrade());
        }
    }

    // Define upgrade types for better code readability
    public enum UpgradeType
    {
        First,
        Second,
        Third,
        Fourth
    }

    public void GetUpgradeOptions(Turret _turret, UpgradeType upgradeType = UpgradeType.First)
    {
        gameManager.InvertGameState();
        currentTurret = _turret;

        List<IUpgradeOption> availableUpgrades = new List<IUpgradeOption>();

        switch (upgradeType)
        {
            case UpgradeType.First:
                LevelOneUpgradeOptions(_turret);
                return;

            case UpgradeType.Second:
                CheckForUpgrades(availableUpgrades, 1);
                break;

            case UpgradeType.Third:
                CheckForUpgrades(availableUpgrades, 2);
                break;

            case UpgradeType.Fourth:
                availableUpgrades.AddRange(largeUpgrades);
                break;
        }

        // Select 3 random upgrades from the filtered list
        optionUpgrades = GetRandomUpgradeOptions(3, availableUpgrades);

        if (gameManager.autoUpgrade)
        {
            UpgradeOptions(0);
            return;
        }

        OpenRightPanel(_turret);
    }

    public void LevelOneUpgradeOptions(Turret _turret)
    {
        currentTurret = _turret;

        optionUpgrades = GetRandomUpgradeOptions(3, smallUpgrades);

        // to do decide if we want small upgrades
        if (gameManager.autoUpgrade || true)
        {
            UpgradeOptions(0);
            return;
        }

        OpenRightPanel(_turret);
    }

    // Helper methods to separate logic
    private void CheckForUpgrades(List<IUpgradeOption> _availableUpgrades, int _level)
    {
        foreach (IUpgradeOption upgrade in currentTurret.unlockedUpgradeList)
        {
            IUpgradeOption[] options = upgrade.NextUpgradeOption();
            if (options != null)
            {
                CheckUpgrades(options, _availableUpgrades);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            _availableUpgrades.Add(smallUpgrades[UnityEngine.Random.Range(0, smallUpgrades.Count)]);
        }
    }


    public void CheckUpgrades(IUpgradeOption[] _options, List<IUpgradeOption> _availableUpgrades)
    {        
        foreach (IUpgradeOption upgrade in _options) // Go through each upgrade in the big list of all upgrades
        {
            string upgradeName = upgrade.GetName(); // Get the name of the current upgrade we're looking at

            if (SaveSystem.playerData.unlockedUpgradeNames.Contains(upgradeName) || gameManager.allUpgrades) // Check if the name of this upgrade is in our list of unlocked names
            {
                if (upgrade.GetLevel() > currentTurret.currentUpgradeLevel)
                {
                    Debug.Log("checking level " + currentTurret.currentUpgradeLevel + " vs upgrade " + upgrade.GetName() + " level: " + upgrade.GetLevel());
                    _availableUpgrades.Add(upgrade); // If it is, then this is an upgrade you've unlocked, so add it to our new list
                }
            }
        }
    }

    private List<IUpgradeOption> GetRandomUpgradeOptions(int count, List<IUpgradeOption> _upgradeList)
    {
        List<IUpgradeOption> selectedUpgrades = new List<IUpgradeOption>();

        // Avoid duplicating count if there aren't enough upgrades
        int selectionCount = Mathf.Min(count, _upgradeList.Count);

        // Create a copy to avoid modifying the original list
        List<IUpgradeOption> availableOptions = new List<IUpgradeOption>(_upgradeList);

        for (int i = 0; i < selectionCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableOptions.Count);
            selectedUpgrades.Add(availableOptions[randomIndex]);
            availableOptions.RemoveAt(randomIndex); // Prevent duplicates
        }

        return selectedUpgrades;
    }

    private void OpenRightPanel(Turret _turret)
    {
        rightPanelUI.SetActive(true);

        turretNameUI.text = $"{_turret.GetInstanceID()}";
        bulletDamageUI.text = $"Damage: {_turret.bulletDamage}";
        fireRateUI.text = $"Fire Rate: {_turret.fireRate}";
        killCountUI.text = $"Kill Count: {_turret.killCount}";
        turretLevelUI.text = $"Level: {_turret.turretLevel}";
        fireRateUpgradeLevel.text = _turret.fireRateUpgrades.ToString();
        damageUpgradeLevel.text = _turret.damageUpgrades.ToString();
        targetRateUpgradeLevel.text = _turret.targetingRateUpgrades.ToString();
        targetRangeUpgradeLevel.text = _turret.targetingRangeUpgrades.ToString();


        for (int i = 0; i < buttonsText.Count; i++)
        {
            buttonsText[i].text = optionUpgrades[i].GetDescription();
            buttonsText[i].fontSize = optionUpgrades[i].GetTextSize();
        }
    }

    public void UpgradeOptions(int _num)
    {
        optionUpgrades[_num].Apply(currentTurret);
        gameManager.InvertGameState();
    }
}


