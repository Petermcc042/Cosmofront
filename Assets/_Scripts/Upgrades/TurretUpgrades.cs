using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public interface IUpgradeOption
{
    void Apply(Turret turret);
    string GetDescription();  // For UI purposes
    int GetTextSize();
    int GetLevel();
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
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Increases fire rate by {fireRateIncrease}";
    }

    public int GetLevel() { return 1; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
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
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Increases targeting rate by {targetRate}";
    }

    public int GetLevel() { return 1; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
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
    }

    public int GetTextSize() { return 18; }

    public string GetDescription()
    {
        return $"Increases the targeting range of the turret by {targetRange}";
    }

    public int GetLevel() { return 1; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}


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
            new PassThroughUpgrade(3)
        };

        int[] tempWeights = { 2, 8 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}


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
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Increases damage by {damageIncrease}";
    }

    public int GetLevel() { return 1; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
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

public class PassThroughUpgrade : IUpgradeOption
{
    public int passThroughIncrease;

    public PassThroughUpgrade(int _increase)
    {
        passThroughIncrease = _increase;
    }

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.passThrough += passThroughIncrease;
        turret.unlockedUpgradeList.Add(this);
    }


    public int GetTextSize() { return 18; }

    public string GetDescription()
    {
        return $"Increases the number of enemies hit by {passThroughIncrease}";
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
        turret.passThrough = 0;
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
            new PiercingRoundsUpgrade()
        };

        int[] tempWeights = { 2, 8 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
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

public class LightningRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.ChainLightning;
        turret.passThrough = 0;
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
            new PassThroughUpgrade(3)
        };

        int[] tempWeights = { 2, 8 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights); 
    }
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
            new PassThroughUpgrade(3),
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

public class PlasmaOverload : IUpgradeOption
{
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

    public int GetLevel() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class SlowRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Slow;
        turret.passThrough = 0;
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
            new DiamondTipUpgrade()
        };

        int[] tempWeights = { 2 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
}


public class SpreadRoundsUpgrade : IUpgradeOption
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
        return $"Bullets split into three projectiles on impact";
    }

    public int GetLevel() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new CirclerUpgrade()
        };

        int[] tempWeights = { 2 };

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
            new DiamondTipUpgrade()
        };

        int[] tempWeights = { 2 };

        return UpgradeMethods.PopulateOptions(tempArray, tempWeights);
    }
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


public class OrbitalStrikeUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.allowOrbitalStrike = true;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Auto lock on and fire a laser that charges then detonates after a second";
    }

    public int GetLevel() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }

}

public class MeteorShowerUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {

        turret.highlightBox.SetActive(false);
        turret.allowMeteorShower = true;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Every few seconds nudge a few asteroids on a collision course with your enemies";
    }

    public int GetLevel() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }

}

public class FirestormUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.allowFirestorm = true;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Every 5 seconds triggers a payload to fall from above coating the area in fire that deals damage over time";
    }

    public int GetLevel() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}


public class TimewarpUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.allowTimewarp = true;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 16; }

    public string GetDescription()
    {
        return $"The turret creates an aura that slows down all enemy movement within a certain radius, giving other turrets more time to attack";
    }

    public int GetLevel() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}