using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class OverclockedUpgrade : IUpgradeOption
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

    public string GetName() { return "Overclocked"; }

    public string GetDescription()
    {
        return $"Greatly increases fire rate, with a small damage boost";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new SuperMonkeyUpgrade()
        };

        return tempArray;
    }
}

public class QuadBarrel : IUpgradeOption
{
    public float fireRateIncrease = 2;
    public float targetingRate = 0.25f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Quad Barrel"; }

    public string GetDescription()
    {
        return $"Turret increases two 4 barrels";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new SuperMonkeyUpgrade()
        };

        return tempArray;
    }
}

public class DroneSupport : IUpgradeOption
{
    public float fireRateIncrease = 2;
    public float targetingRate = 0.25f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Drone Support"; }

    public string GetDescription()
    {
        return $"Turret calls in drone support for short periods";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new ElectricSkyUpgrade()
        };

        return tempArray;
    }
}


public class SpecialisedRounds : IUpgradeOption
{
    public float fireRateIncrease = 2;
    public float targetingRate = 0.25f;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.fireRate += fireRateIncrease;
        turret.targetingRate *= targetingRate;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 20; }

    public string GetName() { return "Specialised Rounds"; }

    public string GetDescription()
    {
        return $"Turret dynamically switches rounds to better match enemies";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class ArcLightning : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.ArcLightning;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 15; }

    public string GetName() { return "Arc Lightning"; }

    public string GetDescription()
    {
        return $"increases the length and damage of lightning";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new PlasmaOverloadUpgrade(),
            new PiercingRoundsUpgrade()
        };

        return tempArray;
    }
}

public class ChargedStorm : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.ArcLightning;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 15; }

    public string GetName() { return "Charged Storm"; }

    public string GetDescription()
    {
        return $"Each hit builds energy increasing damage";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new PlasmaOverloadUpgrade()
        };

        return tempArray;
    }
}

public class CirclerUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Circler;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Circler"; }

    public string GetDescription()
    {
        return $"Bullets split into 10 projectiles on impact";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {

        IUpgradeOption[] tempArray =
        {
            new Circlerer(),
        };

        return tempArray;
    }
}

public class RailgunUpgrade : IUpgradeOption
{
    public int damageIncrease = 5;

    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletDamage += damageIncrease;
        turret.bulletType = BulletType.Circler;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Railgun"; }

    public string GetDescription()
    {
        return $"Drastically increases piercing power but reduces fire rate";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption()
    {
        return null;
    }
}

public class SonicPenetratorUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.bulletType = BulletType.Circler;
    }

    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Sonic Penetrator"; }

    public override string GetDescription()
    {
        return $"Piercing shots emit shockwaves";
    }

    public override int GetLevel() { return 3; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption()
    {
        return null;
    }
}

public class PiercingSlowUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.passThrough = 3;
    }

    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Piercing Slow"; }

    public override string GetDescription()
    {
        return $"Slow bullets pierce more enemies";
    }

    public override int GetLevel() { return 3; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption()
    {
        return null;
    }
}


public class DiamondTipUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.passThrough = 3;
    }

    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Diamond Tip"; }

    public override string GetDescription()
    {
        return $"Hardened bullets can pass through 3 enemies";
    }

    public override int GetLevel() { return 3; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class CryoRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.passThrough = 3;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Cryo Rounds"; }

    public string GetDescription()
    {
        return $"Freezes or slows enemies temporarily";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class StickyRoundsUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.passThrough = 3;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Sticky Rounds"; }

    public string GetDescription()
    {
        return $"Drops glue puddles that slow enemies";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class ClusterBombUpgrade : BaseUpgrade
{
    protected override void ApplyUpgradeEffects(Turret turret)
    {
        turret.bulletType = BulletType.Explosive;
        turret.passThrough = 0;
    }

    public override int GetTextSize() { return 17; }

    public override string GetName() { return "Cluster Bomb"; }

    public override string GetDescription()
    {
        return $"The initial impact causes a cluster of smaller explosions";
    }

    public override int GetLevel() { return 3; }
    public override int GetProbability() { return 2; }

    public override IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class ShockwaveUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Explosive;
        turret.passThrough = 0;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Shockwave"; }


    public string GetDescription()
    {
        return $"Pushes enemies away on detonation";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

public class SchwererCanonUpgrade : IUpgradeOption
{
    public void Apply(Turret turret)
    {
        turret.highlightBox.SetActive(false);
        turret.bulletType = BulletType.Explosive;
        turret.passThrough = 0;
        turret.unlockedUpgradeList.Add(this);
    }

    public int GetTextSize() { return 17; }

    public string GetName() { return "Schwerer Canon"; }

    public string GetDescription()
    {
        return $"a bloody big barrel";
    }

    public int GetLevel() { return 2; }
    public int GetProbability() { return 2; }

    public IUpgradeOption[] NextUpgradeOption() { return null; }
}

