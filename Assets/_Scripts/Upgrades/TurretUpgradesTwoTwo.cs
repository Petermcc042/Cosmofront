using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;



public class MegaMegaUpgrade : IUpgradeOption
{
    public float fireRateIncrease = 10;
    public int bulletDamage = 5;
    public float targetingRate = 0.25f;

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

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class ClusterBombUpgrade : IUpgradeOption
{
    public int level = 2;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Explosive;
        turret.passThrough = 0;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }


    public string GetDescription()
    {
        return $"The initial explosion causes a cluster of smaller explosions";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}


public class ArcLightning : IUpgradeOption
{
    public int level = 2;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.ChainLightningTwo;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 15; }

    public string GetDescription()
    {
        return $"increases the length and damage of lightning";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new PlasmaOverload(),
            new PiercingRoundsUpgrade(),
            new OrbitalStrikeUpgrade(),
            new MeteorShowerUpgrade(),
            new TimewarpUpgrade(),
            new FirestormUpgrade(),
            new MegaUpgrade()
        };

        int[] tempWeights = { 1,2,1,1,1,1,1 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}

public class CirclerUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Spread;
        turret.passThrough = 0;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Bullets split into 10 projectiles on impact";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}


public class DiamondTipUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.passThrough = 3;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Hardened bullets can pass through 3 enemies";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class SonicPenetratorUpgrade : IUpgradeOption
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
        return $"Larger bullets that cause sonic waves which slow enemies";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new PiercingRoundsUpgrade(),
            new MegaUpgrade()
        };

        int[] tempWeights = { 2 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}
