using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public struct TurretUpgrade
{
    public Turret TurretRef;
    public UpgradeType UpgradeTypeRef;
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
                SmallUpgradeTurret(tempTurret);
                break;
            case 80:
                MediumUpgradeTurret(tempTurret, true);
                break;
            case 200:
                MediumUpgradeTurret(tempTurret, false);
                break;
            case 250:
                LargeUpgradeTurret(tempTurret);
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

    private void SmallUpgradeTurret(Turret turret)
    {
        turret.highlightBox.SetActive(true);
        turret.turretLevel++;
        skillManager.GetSmallUpgradeOptions(turret);

    }

    private void MediumUpgradeTurret(Turret _turret, bool _firstTime)
    {
        _turret.highlightBox.SetActive(true);
        _turret.turretLevel++;
        skillManager.GetMediumUpgradeOptions(_turret, _firstTime);
    }

    private void LargeUpgradeTurret(Turret turret)
    {
        turret.highlightBox.SetActive(true);
        turret.turretLevel++;
        skillManager.GetLargeUpgradeOptions(turret);   
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
}
