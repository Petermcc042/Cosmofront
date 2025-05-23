using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class SuperMonkeyUpgrade : IUpgradeOption
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

    public string GetName() { return "Super Monkey"; }

    public string GetDescription()
    {
        return $"Greatly increases fire rate, with a small damage boost";
    }

    public int GetLevel() { return 4; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class ElectricSkyUpgrade : IUpgradeOption
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

    public string GetName() { return "Electric Sky"; }

    public string GetDescription()
    {
        return $"Drones upgrade to lightning rounds";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class PlasmaOverloadUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.PlasmaOverload;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 15; }

    public string GetName() { return "Plasma Overload"; }

    public string GetDescription()
    {
        return $"increases the length and damage of lightning";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}


public class Circlerer : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.SpreadCircles;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Circlerer"; }

    public string GetDescription()
    {
        return $"The bullets that split also split bullets";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 3; }

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

    public string GetName() { return "Orbital Strike"; }

    public string GetDescription()
    {
        return $"Auto lock on and fire a laser that charges then detonates after a second";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 3; }

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

    public string GetName() { return "Meteor Shower"; }

    public string GetDescription()
    {
        return $"Every few seconds nudge a few asteroids on a collision course with your enemies";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 3; }

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

    public string GetName() { return "Firestorm Upgrade"; }

    public string GetDescription()
    {
        return $"Every 5 seconds triggers a payload to fall from above coating the area in fire that deals damage over time";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 3; }

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

    public string GetName() { return "Timewarp Upgrade"; }

    public string GetDescription()
    {
        return $"The turret creates an aura that slows down all enemy movement within a certain radius, giving other turrets more time to attack";
    }

    public int GetLevel() { return 3; }
    public int GetProbability() { return 3; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}
