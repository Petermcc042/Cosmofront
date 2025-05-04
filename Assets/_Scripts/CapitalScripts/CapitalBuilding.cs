using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CapitalBuilding : MonoBehaviour
{
    [SerializeField] CapitalBuildingSO buildingSO;
    [SerializeField] private GameObject purchaseMenuUI;
    [SerializeField] private Button purchaseMenuButtonUI;
    [SerializeField] private TextMeshProUGUI purchaseMenuTitleUI;
    [SerializeField] private TextMeshProUGUI purchaseMenuDescriptionUI;
    [SerializeField] private TextMeshProUGUI pmAttCostUI;
    [SerializeField] private TextMeshProUGUI pmMarcCostUI;
    [SerializeField] private TextMeshProUGUI pmImearCostUI;
    [SerializeField] private GameObject upgradeMenuUI;

    [Header("Camera Lerping")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lerpSpeed = 5.0f;
    [SerializeField] private float stopDistanceThreshold = 0.05f;
    // --- Internal State ---
    private Vector3 targetDestination;
    private bool isMoving = false;
    // Small threshold to prevent endless tiny lerps
    private const float STOP_DISTANCE_THRESHOLD = 0.01f;

    void LateUpdate()
    {
        // Only run the lerp logic if isMoving is true
        if (isMoving)
        {
            float distance = Vector3.Distance(cameraTransform.position, targetDestination);

            // Check if we are close enough to stop
            if (distance > STOP_DISTANCE_THRESHOLD)
            {
                // Perform the lerp
                cameraTransform.position = Vector3.Lerp(
                    cameraTransform.position,
                    targetDestination,
                    Time.deltaTime * lerpSpeed // Frame-rate independent smoothing
                );
            }
            else
            {
                // We are close enough - snap to the exact destination and stop moving
                cameraTransform.position = targetDestination;
                isMoving = false;
            }
        }
    }

    public void StopMovement()
    {
        isMoving = false;
    }


    public void OpenMenu()
    {
        if (!SaveSystem.playerData.unlockedBuildingNames.Contains(buildingSO.buildingName))
        {
            targetDestination = buildingSO.cameraPosition;
            isMoving = true;

            OpenPurchaseMenu();
        } 
        else
        {
            upgradeMenuUI.SetActive(true);
        }
    }

    private void OpenPurchaseMenu()
    {
        purchaseMenuUI.SetActive(true);
        purchaseMenuButtonUI.interactable = true;
        purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Purchase";

        purchaseMenuTitleUI.text = buildingSO.buildingName;
        purchaseMenuDescriptionUI.text = buildingSO.buildingDescription;

        pmAttCostUI.text = buildingSO.purchaseCost.x.ToString();
        pmMarcCostUI.text = buildingSO.purchaseCost.y.ToString();
        pmImearCostUI.text = buildingSO.purchaseCost.z.ToString();

        //Debug.Log($"current att cost {currentUpgradeNode.upgradeCost.x} vs resources {resourceData.totalAttanium}");
        if (buildingSO.purchaseCost.x > SaveSystem.playerData.attaniumTotal)
        {
            Debug.Log("called");
            pmAttCostUI.color = Color.red;
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Blocked";
        }

        //Debug.Log($"current marc cost {currentUpgradeNode.upgradeCost.y} vs resources {resourceData.totalMarcum}");
        if (buildingSO.purchaseCost.y > SaveSystem.playerData.marcumTotal)
        {
            pmMarcCostUI.color = Color.red;
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Blocked";
        }

        if (buildingSO.purchaseCost.z > SaveSystem.playerData.imearTotal)
        {
            pmImearCostUI.color = Color.red;
            purchaseMenuButtonUI.interactable = false;
            purchaseMenuButtonUI.GetComponentInChildren<TextMeshProUGUI>().text = "Blocked";
        }
    }

    public void CloseMenu()
    {
        purchaseMenuUI.SetActive(false);
        upgradeMenuUI.SetActive(false);
    }

    public void OpenWorldView()
    {
        SceneManager.LoadScene("3_WorldView");
    }

    public void PurchaseBuilding()
    {
        upgradeMenuUI.SetActive(true);
        purchaseMenuUI.SetActive(false);

        SaveSystem.playerData.attaniumTotal -= (int)buildingSO.purchaseCost.x;
        SaveSystem.playerData.marcumTotal -= (int)buildingSO.purchaseCost.y;
        SaveSystem.playerData.imearTotal -= (int)buildingSO.purchaseCost.z;

        if (!SaveSystem.playerData.unlockedBuildingNames.Contains(buildingSO.buildingName))
        {
            SaveSystem.playerData.unlockedBuildingNames.Add(buildingSO.buildingName);
            SaveSystem.SaveGame();
        }
    }
}
