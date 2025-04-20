using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;

public interface IUpgradeOption
{
    void Apply(Turret turret);
    string GetDescription();  // For UI purposes
    int GetTextSize();
    int GetLevel();
    int GetProbability();
    string GetName();
    IUpgradeOption[] NextUpgradeOption();
    
}

public static class UpgradeMethods
{
    public static IUpgradeOption[] PopulateOptions(IUpgradeOption[] options, int[] weights)
    {
        List<IUpgradeOption> weightedOptions = new List<IUpgradeOption>();

        for (int i = 0; i < options.Length; i++)
        {
            int count = weights[i] * 10;
            for (int j = 0; j < count; j++)
            {
                weightedOptions.Add(options[i]);
            }
        }

        return weightedOptions.ToArray();
    }
}

public class FireRateUpgrade : IUpgradeOption
{
    public float fireRateIncrease;

    public FireRateUpgrade(float increase)
    {
        fireRateIncrease = increase;
    }

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.unlockedUpgradeList.Add(this);
        turret.fireRateUpgrades += 1;
    }

    public int GetTextSize() { return 20; }
    public int GetProbability() { return 20; }

    public string GetName() { return "Fire Rate"; }

    public string GetDescription()
    {
        return $"Increases fire rate by {fireRateIncrease}";
    }

    public int GetLevel() { return 1; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new RapidFireUpgrade(),
            new BurstFireUpgrade(),
            new DualBarrelUpgrade()
        };

        return tempArray;
    }
}

public class TargetingRateUpgrade : IUpgradeOption
{
    public float targetRate;

    public TargetingRateUpgrade(float increase)
    {
        targetRate = increase;
    }

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.targetingRate -= targetRate;
        turret.unlockedUpgradeList.Add(this);
        turret.targetingRateUpgrades += 1;
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Targeting Rate"; }
    public string GetDescription()
    {
        return $"Increases targeting rate by {targetRate}";
    }

    public int GetLevel() { return 1; }

    public int GetProbability() { return 20; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new DualBarrelUpgrade(),
            new AI_Targeting(),
            new LightningRoundsUpgrade()
        };

        return tempArray;
    }
}

public class TargetRangeUpgrade : IUpgradeOption
{
    public float targetRange;

    public TargetRangeUpgrade(float increase)
    {
        targetRange = increase;
    }

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.range += targetRange;
        turret.unlockedUpgradeList.Add(this);
        turret.targetingRangeUpgrades += 1;
    }

    public int GetTextSize() { return 18; }

    public string GetName() { return "Targeting Range"; }

    public string GetDescription()
    {
        return $"Increases the targeting range of the turret by {targetRange}";
    }

    public int GetLevel() { return 1; }

    public int GetProbability() { return 20; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new LightningRoundsUpgrade(),
            new OverchargeRoundsUpgrade(),
            new SpreadRoundsUpgrade(),
            new PiercingRoundsUpgrade()
        };

        return tempArray;
    }
}


public class DamageUpgrade : IUpgradeOption
{
    public int damageIncrease;

    public DamageUpgrade(int increase)
    {
        damageIncrease = increase;
    }

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletDamage += damageIncrease;
        turret.unlockedUpgradeList.Add(this);
        turret.damageUpgrades += 1;
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Damage"; }

    public string GetDescription()
    {
        return $"Increases damage by {damageIncrease}";
    }

    public int GetLevel() { return 1; }

    public int GetProbability() { return 20; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new PiercingRoundsUpgrade(),
            new SlowRoundsUpgrade(),
            new ExplosiveRoundsUpgrade(),
            new HollowPointRoundsUpgrade()
        };

        return tempArray;
    }
}