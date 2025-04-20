using System.Collections.Generic;
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
    [SerializeField] private TurretManager turretManager;

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


    public void GetSmallUpgradeOptions(Turret _turret)
    {
        gameManager.InvertGameState();
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

    public void GetFirstUpgradeOptions(Turret _turret)
    {
        gameManager.InvertGameState();
        currentTurret = _turret;

        List<IUpgradeOption> availableUpgrades = new List<IUpgradeOption>();

        IUpgradeOption[] tempFireRate = new FireRateUpgrade(0.3f).NextUpgradeOption();
        CheckUpgrade(tempFireRate, tempFireRate.Length, availableUpgrades);

        IUpgradeOption[] tempDamage = new DamageUpgrade(1).NextUpgradeOption();
        CheckUpgrade(tempDamage, tempDamage.Length, availableUpgrades);

        IUpgradeOption[] tempRange = new TargetRangeUpgrade(1).NextUpgradeOption();
        CheckUpgrade(tempRange, tempRange.Length, availableUpgrades);

        IUpgradeOption[] tempRate = new TargetRangeUpgrade(1).NextUpgradeOption();
        CheckUpgrade(tempRate, tempRate.Length, availableUpgrades);

        foreach (var upgradeOption in availableUpgrades)
        {
            Debug.Log(upgradeOption.GetName());
        }

        for (int i = 0;i < 20;i++)
        {
            availableUpgrades.Add(smallUpgrades[UnityEngine.Random.Range(0, smallUpgrades.Count)]);
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

    public void CheckUpgrade(IUpgradeOption[] _options, int _upgradeLevel, List<IUpgradeOption> _availableUpgrades)
    {
        IUpgradeOption[] tempArray = _options;
        List<IUpgradeOption> actualUpgrades = new List<IUpgradeOption>();

        foreach (var upgradeOption in tempArray)
        {
            if (SaveSystem.playerData.unlockedUpgradeNames.Contains(upgradeOption.GetName()))
            {
                actualUpgrades.Add(upgradeOption);
            }
        }

        if (actualUpgrades.Count > 0)
        {
            for (int i = 0; i < _upgradeLevel; i++)
            {
                IUpgradeOption tempUpgrade = actualUpgrades[UnityEngine.Random.Range(0, actualUpgrades.Count)];
                _availableUpgrades.Add(tempUpgrade);
            }
        }        
    }

    public void GetSecondUpgradeOptions(Turret _turret)
    {
        gameManager.InvertGameState();
        currentTurret = _turret;

        List<IUpgradeOption> availableUpgrades = new List<IUpgradeOption>();

        foreach (IUpgradeOption upgrade in _turret.unlockedUpgradeList)
        {
            if (upgrade.GetLevel() <= 1) { continue; }

            IUpgradeOption[] tempUpgrades = upgrade.NextUpgradeOption();
            foreach (IUpgradeOption nextUpgrade in tempUpgrades)
            {
                if (SaveSystem.playerData.unlockedUpgradeNames.Contains(nextUpgrade.GetName()))
                {
                    availableUpgrades.Add(nextUpgrade);
                }
            }
        }

        if (availableUpgrades.Count == 0)
        {
            IUpgradeOption[] tempFireRate = new FireRateUpgrade(0.3f).NextUpgradeOption();
            CheckUpgrade(tempFireRate, tempFireRate.Length, availableUpgrades);

            IUpgradeOption[] tempDamage = new DamageUpgrade(1).NextUpgradeOption();
            CheckUpgrade(tempDamage, tempDamage.Length, availableUpgrades);

            IUpgradeOption[] tempRange = new TargetRangeUpgrade(1).NextUpgradeOption();
            CheckUpgrade(tempRange, tempRange.Length, availableUpgrades);

            IUpgradeOption[] tempRate = new TargetRangeUpgrade(1).NextUpgradeOption();
            CheckUpgrade(tempRate, tempRate.Length, availableUpgrades);
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

    public void GetLargeUpgradeOptions(Turret _turret)
    {
        gameManager.InvertGameState();
        currentTurret = _turret;

        List<IUpgradeOption> availableUpgrades = largeUpgrades;

        foreach (IUpgradeOption upgrade in _turret.unlockedUpgradeList)
        {
            if (upgrade.GetLevel() <= 2) { continue; }

            IUpgradeOption[] tempUpgrades = upgrade.NextUpgradeOption();
            foreach (IUpgradeOption nextUpgrade in tempUpgrades)
            {
                availableUpgrades.Add(nextUpgrade);
            }
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

    private List<IUpgradeOption> GetRandomUpgradeOptions(int count, List<IUpgradeOption> _upgradeList)
    {
        List<IUpgradeOption> selectedUpgrades = new List<IUpgradeOption>();
        for (int i = 0; i < count; i++)
        {
            selectedUpgrades.Add(GetRandomUpgrade(_upgradeList));
        }
        return selectedUpgrades;
    }

    private IUpgradeOption GetRandomUpgrade(List<IUpgradeOption> _upgradeList)
    {
        int randomIndex = UnityEngine.Random.Range(0, _upgradeList.Count);
        return _upgradeList[randomIndex];
    }
}


