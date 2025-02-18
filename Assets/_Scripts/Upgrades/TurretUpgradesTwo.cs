using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;


public class MegaUpgrade : IUpgradeOption
{
    public float fireRateIncrease = 3;
    public int bulletDamage = 2;
    public float targetingRate = 0.5f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.bulletDamage += bulletDamage;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Mega Upgrade";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new MegaMegaUpgrade(),
            new PiercingRoundsUpgrade(),
            new LightningRoundsUpgrade(),
            new ExplosiveRoundsUpgrade(),
            new SlowRoundsUpgrade(),
            new SpreadRoundsUpgrade()
        };

        int[] tempWeights = { 4, 8, 1, 2, 2, 2 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}


public class ChangeProjectileUpgrade : IUpgradeOption
{
    public GameObject newProjectilePrefab;

    public ChangeProjectileUpgrade(GameObject newPrefab)
    {
        newProjectilePrefab = newPrefab;
    }

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.unlockedUpgradeList.Add(this);
    }


    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return "Changes the projectile type";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class ExplosiveRoundsUpgrade : IUpgradeOption
{
    public int level = 2;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Explosive;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }


    public string GetDescription()
    {
        return $"Turns the rounds explosive dealing damage to nearby enemies";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new ClusterBombUpgrade(),
            new PiercingRoundsUpgrade(), 
            new MegaUpgrade()
        };

        int[] tempWeights = { 4, 8, 1 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
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

    public string GetDescription()
    {
        return $"creates a chain of lightning dealing damage to nearby enemies";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() {
        
        IUpgradeOption[] tempArray =
        {
            new ArcLightning(),
            new PiercingRoundsUpgrade(),
            new MegaUpgrade()
        };

        int[] tempWeights = { 4, 8, 1 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights); 
    }
}

public class SlowRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Slow;
        turret.unlockedUpgradeList.Add(this);
    }


    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"A bullet that slows and damages enemies";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new SonicPenetratorUpgrade(),
            new PiercingRoundsUpgrade(),
            new MegaUpgrade()
        };

        int[] tempWeights = { 4, 8, 1 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
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

    public string GetDescription()
    {
        return $"Bullets split into three projectiles on impact";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new CirclerUpgrade(),
            new PiercingRoundsUpgrade(),
            new MegaUpgrade()
        };

        int[] tempWeights = { 4, 8, 1 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}

public class PiercingRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.passThrough = 1;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Hardened bullets can pass through 1 enemies";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new DiamondTipUpgrade(),
            new MegaUpgrade(),
            new LightningRoundsUpgrade(),
            new ExplosiveRoundsUpgrade(),
            new SlowRoundsUpgrade(),
            new SpreadRoundsUpgrade()
        };

        int[] tempWeights = { 4, 8, 1, 2, 2, 2 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}


public class RicochetRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Ricochet;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Bullets can ricochet off the terrain";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
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

    public string GetDescription()
    {
        return $"Bullets charge up damage between shots";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}
