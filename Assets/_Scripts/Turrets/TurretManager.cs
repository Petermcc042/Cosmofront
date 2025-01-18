using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Awake()
    {
        turretsToUpgrade = new List<TurretUpgrade>();
        turretsToAdd = new List<TurretUpgrade>();
    }

    private void Update()
    {
        if (turretsToAdd.Count > 0)
        {
            for (int i = 0; i < turretsToAdd.Count; i++)
            {
                turretsToUpgrade.Add(turretsToAdd[i]);
            }

            turretsToAdd.Clear();
        }

        if (gameManager.GetGameState()) return;

        // maybe add logic to see if the previous turret is in the list from last frame
        // rather than jump about screen to one then back to the same turret

        if (turretsToUpgrade.Count > 0)
        {
            TurretUpgrade tempUpgrade = turretsToUpgrade[0];
            switch (tempUpgrade.UpgradeTypeRef)
            {
                case UpgradeType.Small:
                    SmallUpgradeTurret(tempUpgrade.TurretRef);
                    break;
                case UpgradeType.Medium:
                    MediumUpgradeTurret(tempUpgrade.TurretRef, tempUpgrade.Position);
                    break;
                case UpgradeType.Large:
                    LargeUpgradeTurret(tempUpgrade.TurretRef);
                    break;
            }

            turretsToUpgrade.RemoveAt(0);
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

        if (turret.autoUpgrade)
        {
            skillManager.GetAutoUpgradeOptions(turret);
        }
        else
        {
            skillManager.GetSmallUpgradeOptions(turret);
        }
    }

    private void MediumUpgradeTurret(Turret turret, int position)
    {
        turret.highlightBox.SetActive(true);
        skillManager.GetMediumUpgradeOptions(turret, position);
        turret.turretLevel++;
    }

    private void LargeUpgradeTurret(Turret turret)
    {
        turret.highlightBox.SetActive(true);
        skillManager.GetLargeUpgradeOptions(turret);
        turret.turretLevel++;
    }
}
