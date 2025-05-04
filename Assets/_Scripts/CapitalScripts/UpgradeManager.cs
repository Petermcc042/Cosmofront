using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [Header("Single Upgrade Fields")]
    [SerializeField] private GameObject singleUpgradeUI;
    [SerializeField] private TextMeshProUGUI singleUpgradeTitle;
    [SerializeField] private TextMeshProUGUI singleUpgradeDescription;
    [SerializeField] private Image singleUpgradeImage;
    [SerializeField] private TextMeshProUGUI singleUpgradeAttCost;
    [SerializeField] private TextMeshProUGUI singleUpgradeMarcCost;
    [SerializeField] private TextMeshProUGUI singleUpgradeImearCost;
    [SerializeField] private Button singleUpgradePurchaseButton;

    private UpgradeNode currentUpgradeNode;

    public List<UpgradeNode> allNodes = new List<UpgradeNode>();

    private void Awake()
    {
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if(t.GetComponent<UpgradeNode>() != null)
            {
                allNodes.Add(t.GetComponent<UpgradeNode>());
            }
            
        }
    }

    public void AddUpgradeNode(UpgradeNode _upgradeNode)
    {
        allNodes.Add(_upgradeNode);
    }

    public void CurrentUpgrade(UpgradeNode _currentUpgradeNode)
    {
        currentUpgradeNode = _currentUpgradeNode;
        OpenSingleUpgrade();
    }

    public void RunLines()
    {

        foreach (UpgradeNode upgradeNode in allNodes)
        {
            bool player_purchased = SaveSystem.playerData.unlockedUpgradeNames.Contains(upgradeNode.upgradeName) ? true : false;
            upgradeNode.isPurchased = player_purchased;
            //Debug.Log($"Is {upgradeNode.upgradeName} unlocked: {player_purchased}");
        }

        foreach (UpgradeNode upgradeNode in allNodes)
        {
            upgradeNode.DrawLines();
        }
    }

    // GUI Method called in capital
    public void OpenSingleUpgrade()
    {
        singleUpgradeUI.SetActive(true);
        singleUpgradePurchaseButton.interactable = true;

        singleUpgradeTitle.text = currentUpgradeNode.upgradeName;
        singleUpgradeDescription.text = currentUpgradeNode.upgradeDescription;
        singleUpgradeImage.sprite = currentUpgradeNode.upgradeImage;

        singleUpgradeAttCost.text = currentUpgradeNode.upgradeCost.x.ToString();
        singleUpgradeMarcCost.text = currentUpgradeNode.upgradeCost.y.ToString();
        singleUpgradeImearCost.text = currentUpgradeNode.upgradeCost.z.ToString();

        //Debug.Log($"current att cost {currentUpgradeNode.upgradeCost.x} vs resources {resourceData.totalAttanium}");
        if (currentUpgradeNode.upgradeCost.x > SaveSystem.playerData.attaniumTotal)
        {
            Debug.Log("called");
            singleUpgradeAttCost.color = Color.red;
            singleUpgradePurchaseButton.interactable = false;
        }

        //Debug.Log($"current marc cost {currentUpgradeNode.upgradeCost.y} vs resources {resourceData.totalMarcum}");
        if (currentUpgradeNode.upgradeCost.y > SaveSystem.playerData.marcumTotal)
        {
            singleUpgradeMarcCost.color = Color.red;
            singleUpgradePurchaseButton.interactable = false;
        }

        if (currentUpgradeNode.upgradeCost.z > SaveSystem.playerData.imearTotal)
        {
            singleUpgradeImearCost.color = Color.red;
            singleUpgradePurchaseButton.interactable = false;
        }
    }


    public void PurchaseUpgrade()
    {
        currentUpgradeNode.isPurchased = true;

        SaveSystem.playerData.attaniumTotal -= (int)currentUpgradeNode.upgradeCost.x;
        SaveSystem.playerData.marcumTotal -= (int)currentUpgradeNode.upgradeCost.y;
        SaveSystem.playerData.imearTotal -= (int)currentUpgradeNode.upgradeCost.z;

        if (!SaveSystem.playerData.unlockedUpgradeNames.Contains(currentUpgradeNode.upgradeName))
        {
            SaveSystem.playerData.unlockedUpgradeNames.Add(currentUpgradeNode.upgradeName);
            SaveSystem.SaveGame();
        }

        foreach (UpgradeNode upgradeNode in allNodes)
        {
            upgradeNode.DrawLines();
        }

        singleUpgradeUI.SetActive(false);
    }
}
