using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingManager : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] LayerMask buildingLayerMask;
    [SerializeField] CapitalBuilding[] buildingArray;
    [SerializeField] TextMeshProUGUI upgradeDescription;
    [SerializeField] GameObject descriptionObject;
    [SerializeField] LayerMask upgradeLayer;

    [SerializeField] GameObject turretSingleUpgradeMenu;

    [Header("Purchase Menu")]
    [SerializeField] GameObject tempMenuObject;

    // private int GenHealthChange = 10;
    private int turretDamageChange = 5;
    private int shieldHealthIncrease = 5;

    private void Awake()
    {
        buildingArray = this.GetComponentsInChildren<CapitalBuilding>();
        foreach (CapitalBuilding c in buildingArray)
        {
            if (c.purchaseMenuUI == null) { c.purchaseMenuUI = tempMenuObject; }
            if (c.upgradeMenuUI == null) { c.upgradeMenuUI = tempMenuObject; }
        }
    }

    public void CheckBuildingClick()
    {
        if (IsOverUI()) { return; }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, buildingLayerMask))
        {
            CapitalBuilding building = raycastHit.collider.GetComponent<CapitalBuilding>();
            if (building != null)
            {
                CloseMenus();
                building.OpenMenu();
            }
        }
        else
        {
            CloseMenus();
        }
    }

   

    private static bool IsOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;
        else
            return false;
    }


    #region Unity UI
    

    public void CloseMenus()
    {
        if (!turretSingleUpgradeMenu.activeSelf)
        {
            for (int i = 0; i < buildingArray.Length; i++)
            {
                buildingArray[i].CloseMenu();
            }
        }
        else
        {
            turretSingleUpgradeMenu.SetActive(false);
        }
    }

    public void UpgradePlayerAddition(string upgrade)
    {
        int direction = 1;

        if (Enum.TryParse(upgrade, true, out PlayerUpgradesEnum upgradeType))
        {
            Debug.Log(upgradeType);
            switch (upgradeType)
            {
                case PlayerUpgradesEnum.ShieldHealth:
                    SaveSystem.playerData.shieldHealthIncrease += shieldHealthIncrease * direction;
                    break;
            }
        }
        else
        {
            Debug.LogError($"Invalid round type: {upgradeType}");
        }
    }

    public void UpgradePlayerSubtraction(string upgrade)
    {
        int direction = -1;

        if (Enum.TryParse(upgrade, true, out PlayerUpgradesEnum upgradeType))
        {
            Debug.Log(upgradeType);
            switch (upgradeType)
            {
                case PlayerUpgradesEnum.ShieldHealth:
                    SaveSystem.playerData.shieldHealthIncrease += shieldHealthIncrease * direction;
                    break;
            }
        }
        else
        {
            Debug.LogError($"Invalid round type: {upgradeType}");
        }
    }

    public void SavePlayerData()
    {
        SaveSystem.SaveGame();
    }

    #endregion
}
