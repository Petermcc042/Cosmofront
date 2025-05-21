using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public struct TurretUpgrade
{
    public Turret TurretRef;
    public int Position;
}

public class TurretManager : MonoBehaviour
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private GameManager gameManager;
    private List<TurretUpgrade> turretsToUpgrade;
    private List<TurretUpgrade> turretsToAdd;
    private List<Turret> allTurrets;
    private List<int> turretUpgradeIDs;

    private void Awake()
    {
        turretsToUpgrade = new List<TurretUpgrade>();
        turretsToAdd = new List<TurretUpgrade>();
        allTurrets = new List<Turret>();
        turretUpgradeIDs = new List<int>();
    }

    public void CallUpdate()
    {
        SequentialUpgrades();
    }

    private void SequentialUpgrades()
    {
        if (turretUpgradeIDs.Count == 0) return;

        // find one turret id if the list is not empty
        int upgradeID = turretUpgradeIDs[0];
        Turret tempTurret = null;

        // Find the turret with the matching ID
        for (int j = 0; j < allTurrets.Count; j++)
        {
            if (allTurrets[j].turretID == upgradeID)
            {
                tempTurret = allTurrets[j];
                break;
            }
        }

        if (tempTurret == null)
        {
            turretUpgradeIDs.RemoveAt(0);
            return;
        }

        tempTurret.turretXP += 1;

        switch (tempTurret.turretXP)
        {
            case 20:
            case 40:
            case 60:
            case 100:
            case 120:
            case 160:
            case 180:
                Debug.Log("First upgrad level = " + tempTurret.currentUpgradeLevel);
                TryGetUpgrade(tempTurret, SkillManager.UpgradeType.First);
                break;
            case 80:
                Debug.Log("Second Upgrade level = " + tempTurret.currentUpgradeLevel);
                TryGetUpgrade(tempTurret, SkillManager.UpgradeType.Second);
                break;
            case 200:
                Debug.Log("Third Upgrad level = " + tempTurret.currentUpgradeLevel);
                TryGetUpgrade(tempTurret, SkillManager.UpgradeType.Third);
                break;
            case 250:
                Debug.Log("Fourth Upgrad level = " + tempTurret.currentUpgradeLevel);
                TryGetUpgrade(tempTurret, SkillManager.UpgradeType.Fourth);
                break;
        }

        turretUpgradeIDs.RemoveAt(0);
    }


    public void CallUpdateAllTurrets()
    {
        for (int i = 0; i < allTurrets.Count; i++)
        {
            allTurrets[i].CallUpdate();
        }
    }

    public void AddTurretForUpgrade(TurretUpgrade turret)
    {
        if (!turretsToAdd.Contains(turret))
        {
            turretsToAdd.Add(turret);
        }
    }

    private void TryGetUpgrade(Turret _turret, SkillManager.UpgradeType _level)
    {
        if (_turret == null) { Debug.Log("turret is null!!!!"); };
        _turret.highlightBox.SetActive(true);
        _turret.turretLevel++;
        skillManager.GetUpgradeOptions(_turret, _level);
    }

    public void AddTurret(Turret turret)
    {
        if (!allTurrets.Contains(turret))
        {
            allTurrets.Add(turret);
        }
    }

    public void RemoveTurret(Turret turret)
    {
        if (allTurrets.Contains(turret))
        {
            allTurrets.Remove(turret);
        }
    }

    public void HandleXPUpdate(NativeList<TurretUpgradeData> _turretXP)
    {
        for (int i = 0; i < _turretXP.Length; i++)
        {
            for (int j = 0; j < _turretXP[i].XPAmount; j++)
            {
                turretUpgradeIDs.Add(_turretXP[i].TurretID);
            }
        }
    }

    public void UpgradeAllTurrets()
    { 
        foreach (var turret in allTurrets)
        {
            MeteorShowerUpgrade temp = new MeteorShowerUpgrade();
            temp.Apply(turret);
        }
    }
}
