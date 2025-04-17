using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuidingManager : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] LayerMask buildingLayerMask;
    [SerializeField] List<CapitalBuilding> capitalBuildings;
    [SerializeField] List<GameObject> menuList;
    [SerializeField] TextMeshProUGUI upgradeDescription;
    [SerializeField] GameObject descriptionObject;
    [SerializeField] LayerMask upgradeLayer;

    [SerializeField] GameObject turretSingleUpgradeMenu;

    // private int GenHealthChange = 10;
    private int turretDamageChange = 5;
    private int shieldHealthIncrease = 5;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))// && !IsOverUI())
        {
            CheckBuildingClick();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { CloseMenu(); }

        if (descriptionObject.activeSelf)
        {
            CheckDescription();
        }
    }

    private void CheckDescription()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, upgradeLayer))
        {
            Debug.Log("hit");
        }
    }

    private void CheckBuildingClick()
    {
        if (IsOverUI()) { return; }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, buildingLayerMask))
        {
            CapitalBuilding building = raycastHit.collider.GetComponent<CapitalBuilding>();
            if (building != null)
            {
                building.OpenMenu();
            }
        }
        else
        {
            for (int i = 0; i < capitalBuildings.Count; i++)
            {
                capitalBuildings[i].CloseMenu();
            }
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
    

    public void CloseMenu()
    {
        if (!turretSingleUpgradeMenu.activeSelf)
        {
            for (int i = 0; i < menuList.Count; i++)
            {
                menuList[i].SetActive(false);
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
                case PlayerUpgradesEnum.TurretDamage:
                    SaveSystem.playerData.turretDamageIncrease += turretDamageChange * direction;
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
                case PlayerUpgradesEnum.TurretDamage:
                    SaveSystem.playerData.turretDamageIncrease += turretDamageChange * direction;
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
