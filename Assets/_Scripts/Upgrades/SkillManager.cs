using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillManager: MonoBehaviour 
{
    public event EventHandler<OnSkillUnlockedEventArgs> OnSkillUnlocked;

    public class OnSkillUnlockedEventArgs : EventArgs { public SkillType skillType; public int buildingID; public int upgradePath; public bool global = false; }

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
    [SerializeField] private List<TextMeshProUGUI> buttonsText;

    private static List<IUpgradeOption> smallUpgrades = new List<IUpgradeOption>();
    private static List<IUpgradeOption> mediumUpgrades = new List<IUpgradeOption>();
    private static List<IUpgradeOption> largeUpgrades = new List<IUpgradeOption>();
    private static List<IUpgradeOption> optionUpgrades = new List<IUpgradeOption>();
    private Turret currentTurret;
    private int currentPosition;

    public List<SkillType> unlockedSkillTypeList;

    private SkillManager()
    {
        unlockedSkillTypeList = new List<SkillType>();
    }

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
            mediumUpgrades.Add(new LightningRoundsUpgrade());
            mediumUpgrades.Add(new SlowRoundsUpgrade());
            mediumUpgrades.Add(new ExplosiveRoundsUpgrade());
            mediumUpgrades.Add(new SpreadRoundsUpgrade());
            mediumUpgrades.Add(new PiercingRoundsUpgrade());
            mediumUpgrades.Add(new MegaUpgrade());
        }

        for (int i = 0; i < 4; i++)
        {
            largeUpgrades.Add(new OrbitalStrikeUpgrade());
            largeUpgrades.Add(new MeteorShowerUpgrade());
            largeUpgrades.Add(new FirestormUpgrade());
            largeUpgrades.Add(new TimewarpUpgrade());
        }
    }

    private IUpgradeOption GetRandomUpgrade(List<IUpgradeOption> _upgradeList)
    {
        int randomIndex = UnityEngine.Random.Range(0, _upgradeList.Count);
        return _upgradeList[randomIndex];
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

    public void GetAutoUpgradeOptions(Turret _turret)
    {
        currentTurret = _turret;

        optionUpgrades = GetRandomUpgradeOptions(3, smallUpgrades);
        optionUpgrades[1].Apply(currentTurret);
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

        rightPanelUI.SetActive(true);

        turretNameUI.text = $"{_turret.GetInstanceID()}";
        bulletDamageUI.text = $"Damage: {_turret.bulletDamage}";
        fireRateUI.text = $"Fire Rate: {_turret.fireRate}";
        killCountUI.text = $"Kill Count: {_turret.killCount}";
        turretLevelUI.text = $"Level: {_turret.turretLevel}";

        
        for (int i = 0; i < buttonsText.Count; i++)
        {
            buttonsText[i].text = optionUpgrades[i].GetDescription();
            buttonsText[i].fontSize = optionUpgrades[i].GetTextSize();
        }
    }

    public void GetMediumUpgradeOptions(Turret _turret, bool _firstTime)
    {
        gameManager.InvertGameState();
        currentTurret = _turret;

        List<IUpgradeOption> availableUpgrades = new List<IUpgradeOption>();

        if (_firstTime)
        {
            availableUpgrades = mediumUpgrades;
        } 
        else
        {
            foreach (IUpgradeOption e in _turret.unlockedUpgradeList)
            {
                if (e.NextUpgradeOption() == null || e.GetLevel() != 2) { continue; }

                foreach (var upgrade in e.NextUpgradeOption())
                {
                    availableUpgrades.Add(upgrade);
                }
            }
        }


        // Select 3 random upgrades from the filtered list
        optionUpgrades = GetRandomUpgradeOptions(3, availableUpgrades);

        if (gameManager.autoUpgrade)
        {
            UpgradeOptions(0);
            return;
        }

        rightPanelUI.SetActive(true);

        turretNameUI.text = $"{_turret.GetInstanceID()}";
        bulletDamageUI.text = $"Damage: {_turret.bulletDamage}";
        fireRateUI.text = $"Fire Rate: {_turret.fireRate}";
        killCountUI.text = $"Kill Count: {_turret.killCount}";
        turretLevelUI.text = $"Level: {_turret.turretLevel}";

        
        for (int i = 0; i < buttonsText.Count; i++)
        {
            buttonsText[i].text = optionUpgrades[i].GetDescription();
            buttonsText[i].fontSize = optionUpgrades[i].GetTextSize();
        }
    }

    public void GetLargeUpgradeOptions(Turret _turret)
    {
        gameManager.InvertGameState();
        currentTurret = _turret;

        List<IUpgradeOption> availableUpgrades = largeUpgrades;

        foreach (IUpgradeOption e in _turret.unlockedUpgradeList)
        {
            if (e.NextUpgradeOption() == null || e.GetLevel() != 3) { continue; }

            foreach (var upgrade in e.NextUpgradeOption())
            {
                availableUpgrades.Add(upgrade);
            }
        }

        optionUpgrades = GetRandomUpgradeOptions(3, largeUpgrades);


        if (gameManager.autoUpgrade)
        {
            UpgradeOptions(0);
            return;
        }

        rightPanelUI.SetActive(true);

        turretNameUI.text = $"{_turret.GetInstanceID()}";
        bulletDamageUI.text = $"Damage: {_turret.bulletDamage}";
        fireRateUI.text = $"Fire Rate: {_turret.fireRate}";
        killCountUI.text = $"Kill Count: {_turret.killCount}";
        turretLevelUI.text = $"Level: {_turret.turretLevel}";

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

    public void UnlockSkill(SkillType _skillType, bool _isGlobal)
    {
        if (!IsSkillUnlocked(_skillType))
        {
            unlockedSkillTypeList.Add(_skillType);
            OnSkillUnlocked?.Invoke(this, 
                new OnSkillUnlockedEventArgs { 
                    skillType = _skillType,
                    buildingID = updatedBuildingID,
                    global = _isGlobal
                });
        }
    }


    public void UnlockSkill_UI()
    {
        Enum.TryParse(updatedSkillName, out SkillType skillType);
        OnSkillUnlocked?.Invoke(this, new OnSkillUnlockedEventArgs { skillType = skillType, buildingID = updatedBuildingID, upgradePath = updatedBuildingPath, global = true });
    }

    public bool IsSkillUnlocked(SkillType skillType)
    {
        return unlockedSkillTypeList.Contains(skillType);
    }

    // is used by Unity UI with multiple different dev controls
    public void UnlockSkillType(string roundType)
    {
        Debug.Log("unlocking round: " + roundType);

        if (Enum.TryParse(roundType, true, out SkillType skillType))
        {
            // If parsing is successful, unlock the skill
            UnlockSkill(skillType, true);
        }
        else
        {
            Debug.LogError($"Invalid round type: {roundType}");
        }
    }

    public enum SkillType
    {
        None, MegaUpgrade,
        ExplosiveRound, LightningRound, SlowRound, SpreadRound, RicochetRound, OverchargeRound, PiercingRound, // medium turret upgrades
        OrbitalStrike, MeteorShower, Overclocked, Firestorm, TimewarpPayload, Timewarp,// large turret upgrades
        // not actually used
        BiggerDrill, // attanium building / imear / marcum
        MorePower,
        ExtractionEfficiency,
        ImprovedScanner, // attanium building  /  imear / marcum
        ExtraTooling,
        DoubleDrill, // attanium building
        TripleDrill, // marcum building
        DiamondDrillBit, // Imear building
        WallStrength,// BuildersBrigade
        RoughenedConcrete,
        AbundantResources,
        SpikedWalls,
        PathworkPoly,
        MoonDust,
        SelfRegeneration,
        TirelessWorkers,
        AdvanceIntel, // scanner
        LongRangeScan,
        OmegaThreat,
        TurretFireRate, // generic turrets
        TurretLockOn,
        TurretDamage,
        ResourceAttBoost,
        ResourceEmerBoost,
        ResourceWallRepairs,
        ResourcesDouble,
        WallHealth,
        WallSuperHealth,
        WallDamage,
        WallHealing
    }
}


