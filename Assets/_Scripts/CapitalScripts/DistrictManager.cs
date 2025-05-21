using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DistrictManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] LayerMask districtLayerMask;

    [Header("Purchase Menu UI")]
    [SerializeField] public GameObject purchaseMenuUI;
    [SerializeField] public GameObject upgradeCostsUI;
    [SerializeField] public GameObject populationSplitsUI;
    [SerializeField] public GameObject populationChoiceUI;
    [SerializeField] private Button purchaseMenuButtonUI;
    [SerializeField] private GameObject purchaseButtonUI;
    [SerializeField] private TextMeshProUGUI purchaseMenuTitleUI;
    [SerializeField] private TextMeshProUGUI purchaseMenuDescriptionUI;
    [SerializeField] private TextMeshProUGUI pmAttCostUI;
    [SerializeField] private TextMeshProUGUI pmMarcCostUI;
    [SerializeField] private TextMeshProUGUI pmImearCostUI;
    [SerializeField] private TextMeshProUGUI civilianPercentUI;
    [SerializeField] private TextMeshProUGUI engineerPercentUI;
    [SerializeField] private TextMeshProUGUI scientistPercentUI;
    [SerializeField] private TextMeshProUGUI soldierPercentUI;

    private District[] districtArray;
    private District currentDistrict;
    private bool hasActivateRunOnce = false;
    private bool hasDeactivateRunOnce = false;
    private float timer = 1f;


    private void Awake()
    {
        districtArray = this.GetComponentsInChildren<District>();
    }

    public bool UpdateDistrictTiles()
    {
        if (mainCamera.transform.position.y > 120)
        {
            hasDeactivateRunOnce = false;
            if (hasActivateRunOnce) { return hasActivateRunOnce; }

            foreach (District tile in districtArray)
            {
                tile.ShowTile(true);
            }

            hasActivateRunOnce = true;
        }
        else
        {
            hasActivateRunOnce = false;
            if (hasDeactivateRunOnce) { return hasActivateRunOnce; }

            foreach (District tile in districtArray)
            {
                tile.ShowTile(false);
            }

            hasDeactivateRunOnce = true;
        }

        return hasActivateRunOnce;
    }

    public void CheckDistrictClick()
    {
        if (IsOverUI()) { return; }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, districtLayerMask))
        {
            District district = raycastHit.collider.GetComponent<District>();
            if (district != null)
            {
                currentDistrict = district;
                OpenMenu();
            }
        }
        else
        {
            CloseMenus();
        }
    }

    public void CloseMenus()
    {
        purchaseMenuUI.SetActive(false);
    }

    private static bool IsOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;
        else
            return false;
    }

    public void OpenMenu()
    {
        if (!SaveSystem.playerData.unlockedDistrictIds.Contains(currentDistrict.districtId))
        {
            OpenPurchaseMenu();
        }
        else
        {
            OpenPurchaseMenu();
        }
    }

    private void OpenPurchaseMenu()
    {
        purchaseMenuUI.SetActive(true);
        upgradeCostsUI.SetActive(true);
        populationSplitsUI.SetActive(true);
        purchaseButtonUI.SetActive(true);
        populationChoiceUI.SetActive(false);

        purchaseMenuButtonUI.interactable = true;
        purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Purchase";

        purchaseMenuTitleUI.text = currentDistrict.districtName;
        purchaseMenuDescriptionUI.text = currentDistrict.districtDescription;

        pmAttCostUI.color = Color.white;
        pmMarcCostUI.color = Color.white;
        pmImearCostUI.color = Color.white;

        pmAttCostUI.text = currentDistrict.purchaseCost.x.ToString();
        pmMarcCostUI.text = currentDistrict.purchaseCost.y.ToString();
        pmImearCostUI.text = currentDistrict.purchaseCost.z.ToString();


        int start = (currentDistrict.districtId - 1) * 4;
        List<int> row = SaveSystem.playerData.unlockedDistrictSplitsFlat.GetRange(start, SaveSystem.playerData.splitWidth);

        civilianPercentUI.text = row[0] + "%";
        engineerPercentUI.text = row[1] + "%";
        scientistPercentUI.text = row[2] + "%";
        soldierPercentUI.text = row[3] + "%";

        if (currentDistrict.upgradeLevel > 3)
        {
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Max Level";
            return;
        }


        if (currentDistrict.purchaseCost.x > SaveSystem.playerData.attaniumTotal)
        {
            pmAttCostUI.color = Color.red;
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Blocked";
            return;
        }

        //Debug.Log($"current marc cost {currentUpgradeNode.upgradeCost.y} vs resources {resourceData.totalMarcum}");
        if (currentDistrict.purchaseCost.y > SaveSystem.playerData.marcumTotal)
        {
            pmMarcCostUI.color = Color.red;
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Blocked";
            return;
        }

        if (currentDistrict.purchaseCost.z > SaveSystem.playerData.imearTotal)
        {
            pmImearCostUI.color = Color.red;
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Blocked";
            return;
        }

        
    }

    public void PurchaseDistrict()
    {
        upgradeCostsUI.SetActive(false);
        populationSplitsUI.SetActive(false);
        purchaseButtonUI.SetActive(false);
        populationChoiceUI.SetActive(true);

        purchaseMenuDescriptionUI.text = "Civilians provide faster population growth. \r\nScientists provide greater environmental effect on turrets.\r\nEngineers provide more efficient turrets.\r\nSoldiers create stronger turrets.";
    }

    public void ConfirmUpgrade(int _option) //UI Method //0: civilian // 1: engineer // 2: scientist // 3: soldier
    {
        purchaseMenuUI.SetActive(true);
        upgradeCostsUI.SetActive(true);
        populationSplitsUI.SetActive(true);
        populationChoiceUI.SetActive(false);
        purchaseMenuUI.SetActive(false);

        currentDistrict.upgradeLevel += 1;

        SaveSystem.playerData.civiliansPerSecond += currentDistrict.populationPerSecond;

        int start = (currentDistrict.districtId - 1) * 4;
        SaveSystem.playerData.unlockedDistrictSplitsFlat[start + _option] += 25;
        SaveSystem.playerData.unlockedDistrictSplitsFlat[start] -= 25;

        SaveSystem.playerData.attaniumTotal -= (int)currentDistrict.purchaseCost.x;
        SaveSystem.playerData.marcumTotal -= (int)currentDistrict.purchaseCost.y;
        SaveSystem.playerData.imearTotal -= (int)currentDistrict.purchaseCost.z;

        if (!SaveSystem.playerData.unlockedDistrictIds.Contains(currentDistrict.districtId))
        {
            SaveSystem.playerData.unlockedDistrictIds.Add(currentDistrict.districtId);
            SaveSystem.SaveGame();
        }
    }
}
