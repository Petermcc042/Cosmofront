using System.Collections.Generic;
using Unity.Mathematics;

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

// New base class that all upgrades can inherit from
public abstract class BaseUpgrade : IUpgradeOption
{
    // Common implementation that all upgrades should use
    public void Apply(Turret turret)
    {
        // Common operations
        turret.highlightBox.SetActive(false);
        turret.unlockedUpgradeList.Add(this);
        turret.currentUpgradeLevel = math.max(turret.currentUpgradeLevel, GetLevel());

        // Call specialized implementation
        ApplyUpgradeEffects(turret);
    }

    // Method that derived classes must implement for specific upgrade effects
    protected abstract void ApplyUpgradeEffects(Turret turret);

    // Implement the rest of the interface (can be overridden by derived classes)
    public abstract string GetDescription();
    public abstract int GetTextSize();
    public abstract int GetLevel();
    public abstract int GetProbability();
    public abstract string GetName();
    public abstract IUpgradeOption[] NextUpgradeOption();
}



public class FireRateUpgrade : BaseUpgrade
{
    public float fireRateIncrease;

    public FireRateUpgrade(float increase)
    {
        fireRateIncrease = increase;
    }

    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.fireRate += fireRateIncrease;
        turret.fireRateUpgrades += 1;
    }

    public override int GetTextSize() { return 20; }
    public override int GetProbability() { return 20; }
    public override string GetName() { return "Fire Rate"; }
    public override string GetDescription()
    {
        return $"Increases fire rate by {fireRateIncrease}";
    }
    public override int GetLevel() { return 1; }
    public override IUpgradeOption[] NextUpgradeOption()
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


public class TargetingRateUpgrade : BaseUpgrade
{
    public float targetRate;

    public TargetingRateUpgrade(float increase)
    {
        targetRate = increase;
    }

    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.targetingRate -= targetRate;
        turret.targetingRateUpgrades += 1;
    }

    public override int GetTextSize() { return 20; }
    public override int GetProbability() { return 20; }
    public override string GetName() { return "Targeting Rate"; }
    public override string GetDescription()
    {
        return $"Increases targeting rate by {targetRate}";
    }
    public override int GetLevel() { return 1; }
    public override IUpgradeOption[] NextUpgradeOption()
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


public class TargetRangeUpgrade : BaseUpgrade
{
    public float targetRange;

    public TargetRangeUpgrade(float increase)
    {
        targetRange = increase;
    }

    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.range += targetRange;
        turret.targetingRangeUpgrades += 1;
    }

    public override int GetTextSize() { return 18; }

    public override string GetName() { return "Targeting Range"; }

    public override string GetDescription()
    {
        return $"Increases the targeting range of the turret by {targetRange}";
    }

    public override int GetLevel() { return 1; }

    public override int GetProbability() { return 20; }

    public override IUpgradeOption[] NextUpgradeOption()
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


public class DamageUpgrade : BaseUpgrade
{
    public int damageIncrease;

    public DamageUpgrade(int increase)
    {
        damageIncrease = increase;
    }

    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.bulletDamage += damageIncrease;
        turret.damageUpgrades += 1;
    }

    public override int GetTextSize() { return 20; }

    public override string GetName() { return "Damage"; }

    public override string GetDescription()
    {
        return $"Increases damage by {damageIncrease}";
    }

    public override int GetLevel() { return 1; }

    public override int GetProbability() { return 20; }

    public override IUpgradeOption[] NextUpgradeOption()
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