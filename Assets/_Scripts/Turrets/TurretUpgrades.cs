using UnityEngine;
using UnityEngine.UIElements;

public interface IUpgradeOption
{
    void Apply(Turret turret, int position);
    string GetDescription();  // For UI purposes
    int GetTextSize();
}

public class FireRateUpgrade : IUpgradeOption
{
    public float fireRateIncrease;

    public FireRateUpgrade(float increase)
    {
        fireRateIncrease = increase;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Increases fire rate by {fireRateIncrease}";
    }
}

public class TargetingRateUpgrade : IUpgradeOption
{
    public float targetRate;

    public TargetingRateUpgrade(float increase)
    {
        targetRate = increase;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.targetingRate -= targetRate;
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Increases targeting rate by {targetRate}";
    }
}

public class TargetRangeUpgrade : IUpgradeOption
{
    public float targetRange;

    public TargetRangeUpgrade(float increase)
    {
        targetRange = increase;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.range += targetRange;
    }

    public int GetTextSize() { return 18; }

    public string GetDescription()
    {
        return $"Increases the targeting range of the turret by {targetRange}";
    }
}

public class MegaUpgrade : IUpgradeOption
{
    public float fireRateIncrease = 25;
    public int bulletDamage = 10;
    public float targetingRate = 0.1f;


    public MegaUpgrade(int increase)
    {
        fireRateIncrease *= increase;
        bulletDamage *= increase;
        targetingRate /= increase;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate = fireRateIncrease;
        turret.bulletDamage = bulletDamage;
        turret.targetingRate = targetingRate;
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Mega Upgrade";
    }
}

public class DamageUpgrade : IUpgradeOption
{
    public int damageIncrease;

    public DamageUpgrade(int increase)
    {
        damageIncrease = increase;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletDamage += damageIncrease;
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return $"Increases damage by {damageIncrease}";
    }
}


public class ChangeProjectileUpgrade : IUpgradeOption
{
    public GameObject newProjectilePrefab;

    public ChangeProjectileUpgrade(GameObject newPrefab)
    {
        newProjectilePrefab = newPrefab;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
    }

    public int GetTextSize() { return 20; }

    public string GetDescription()
    {
        return "Changes the projectile type";
    }
}

public class PassThroughUpgrade : IUpgradeOption
{
    public int passThroughIncrease;

    public PassThroughUpgrade(int _increase)
    {
        passThroughIncrease = _increase;
    }

    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.passThrough += passThroughIncrease;
    }

    public int GetTextSize() { return 18; }

    public string GetDescription()
    {
        return $"Increases the number of enemies hit by {passThroughIncrease}";
    }
}

public class ExplosiveRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletTypes[position] = BulletType.Explosive;
        turret.passThrough = 0;
    }

    public int GetTextSize() { return 17; }


    public string GetDescription()
    {
        return $"Turns the rounds explosive dealing damage to nearby enemies";
    }
}

public class LightningRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletTypes[position] = BulletType.ChainLightning;
        turret.passThrough = 0;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"creates a chain of lightning dealing damage to nearby enemies";
    }
}


public class SlowRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletTypes[position] = BulletType.Slow;
        turret.passThrough = 0;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"A bullet that slows and damages enemies";
    }
}

public class SpreadRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletTypes[position] = BulletType.Spread;
        turret.passThrough = 0;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Bullets split into multiple projectiles on impact";
    }
}

public class RicochetRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletTypes[position] = BulletType.Ricochet;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Bullets can ricochet off the terrain";
    }
}

public class OverchargeRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletTypes[position] = BulletType.Overcharge;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Bullets charge up damage between shots";
    }
}

public class OrbitalStrikeUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.allowOrbitalStrike = true;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Auto lock on and fire a laser that charges then detonates after a second";
    }
}

public class MeteorShowerUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.allowMeteorShower = true;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Every few seconds nudge a few asteroids on a collision course with your enemies";
    }
}

public class FirestormUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.allowFirestorm = true;
    }

    public int GetTextSize() { return 17; }

    public string GetDescription()
    {
        return $"Every 5 seconds triggers a payload to fall from above coating the area in fire that deals damage over time";
    }
}

public class TimewarpUpgrade : IUpgradeOption
{
    public void Apply(Turret turret, int position)
    {
        turret.highlightBox.SetActive(false);
        turret.allowTimewarp = true;
    }

    public int GetTextSize() { return 16; }

    public string GetDescription()
    {
        return $"The turret creates an aura that slows down all enemy movement within a certain radius, giving other turrets more time to attack";
    }
}