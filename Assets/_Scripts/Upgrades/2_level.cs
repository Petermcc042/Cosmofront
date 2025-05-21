using UnityEngine;


public class RapidFireUpgrade : IUpgradeOption
{
    public float fireRateIncrease = 3;
    public float targetingRate = 0.5f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Rapid Fire"; }

    public string GetDescription()
    {
        return $"Increases fire rate";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new OverclockedUpgrade()
        };

        return tempArray;
    }
}

public class BurstFireUpgrade : IUpgradeOption
{
    public float fireRateIncrease = 3;
    public float targetingRate = 0.5f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Burst Fire"; }

    public string GetDescription()
    {
        return $"Fires a burst of three bullets";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new OverclockedUpgrade()
        };

        return tempArray;
    }
}

public class DualBarrelUpgrade : IUpgradeOption
{
    public float targetingRate = 0.5f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Dual Barrel"; }

    public string GetDescription()
    {
        return $"Adds a second barrel to the turret";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new OverclockedUpgrade(),
            new QuadBarrel()
        };

        return tempArray;
    }
}

public class AI_Targeting : IUpgradeOption
{
    public float fireRateIncrease = 3;
    public float targetingRate = 0.5f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "A.I. Targeting"; }

    public string GetDescription()
    {
        return $"AI increases faster targeting";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
        };

        return tempArray;
    }
}

public class LightningRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.ChainLightning;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Lightning Rounds"; }

    public string GetDescription()
    {
        return $"creates a chain of lightning dealing damage to nearby enemies";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new ArcLightning()
        };

        return tempArray;
    }
}

public class OverchargeRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Overcharge;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Overcharge Rounds"; }

    public string GetDescription()
    {
        return $"Bullets charge up damage between shots";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}


public class SpreadRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Spread;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Spread Rounds"; }

    public string GetDescription()
    {
        return $"Bullets split into three projectiles on impact";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new CirclerUpgrade(),
            new ClusterBombUpgrade()
        };

        return tempArray;
    }
}


public class PiercingRoundsUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.passThrough = 1;
    }

    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Piercing Rounds"; }

    public override string GetDescription()
    {
        return $"Hardened bullets can pass through 1 enemies";
    }

    public override int GetLevel() { return 2; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new DiamondTipUpgrade()
        };

        return tempArray;
    }
}


public class SlowRoundsUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.bulletType = BulletType.Slow;
    }


    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Slow Rounds"; }

    public override string GetDescription()
    {
        return $"A bullet that slows and damages enemies";
    }

    public override int GetLevel() { return 2; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new SonicPenetratorUpgrade(),
            new PiercingSlowUpgrade()
        };

        return tempArray;
    }
}


public class ExplosiveRoundsUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.bulletType = BulletType.Explosive;
    }

    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Explosive Rounds"; }


    public override string GetDescription()
    {
        return $"Turns the rounds explosive dealing damage to nearby enemies";
    }

    public override int GetLevel() { return 2; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new ClusterBombUpgrade(),
            new PiercingRoundsUpgrade()
        };

        //int[] tempWeights = { 4, 8, 1 };

        return tempArray;//UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}

public class HollowPointRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Explosive;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Hollow Point"; }


    public string GetDescription()
    {
        return $"Hollow point rounds that do serious damage";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new SchwererCanonUpgrade(),
            new ShockwaveUpgrade()
        };

        return tempArray;
    }
}




