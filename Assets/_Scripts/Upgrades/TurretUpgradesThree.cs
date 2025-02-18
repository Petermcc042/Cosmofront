using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

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