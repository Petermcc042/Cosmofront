using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UpgradeNode : MonoBehaviour 
{
    private UpgradeManager upgradeManager;

    public List<GameObject> connectsTo = new List<GameObject>();
    public List<GameObject> currentLines = new List<GameObject>();

    [Header("3D Visualization")]
    [SerializeField] private Material visualizationMaterial;
    private float connectionLineWidth = 3f;
    [SerializeField] private GameObject linesParent;

    [Header("Upgrade Info")]
    public bool isPurchased = true;
    public bool isLocked = true;
    [SerializeField] public string upgradeName;
    [TextArea(15, 20)]
    [SerializeField] public string upgradeDescription;
    [SerializeField] public Sprite upgradeImage;
    [SerializeField] public Vector3 upgradeCost;


    void Awake()
    {
        upgradeManager = GameObject.Find("UpgradeManager").GetComponent<UpgradeManager>();
        linesParent = GameObject.Find("LinesParentObject");
        upgradeImage = this.GetComponentInChildren<Image>().sprite;
        currentLines = new List<GameObject>();

        if (isLocked)
        {
            Button imageTemp = this.GetComponentInChildren<Button>();
            imageTemp.interactable = false;
            return;
        }
    }

    public void DrawLines()
    {
        if (connectsTo == null || connectsTo.Count < 1) { return; }


        if (linesParent == null)
        {
            linesParent = GameObject.Find("LinesParentObject");
        }
        if (upgradeImage == null)
        {
            upgradeImage = this.GetComponentInChildren<Image>().sprite;
        }

        RectTransform initTrans = this.GetComponent<RectTransform>(); // World position of this node
        Vector3 startPos = initTrans.position;

        if (currentLines.Count > 0)
        {
            for (int i = currentLines.Count - 1; i >= 0; i--)
            {
                Destroy(currentLines[i]);
                currentLines.RemoveAt(i);
            }
        }

        foreach (GameObject upgradeNode in connectsTo)
        {
            if (upgradeNode == null)
            {
                Debug.LogWarning($"A connected GameObject in {this.gameObject.name}'s connectsTo list is null.", this);
                continue; // Skip this iteration
            }

            UpgradeNode tempNode = upgradeNode.GetComponent<UpgradeNode>();
            //Debug.Log($"Checknig for {upgradeName}, should we create for {tempNode.upgradeName}. Are both nodes unlocked? {isPurchased} and {tempNode.isPurchased}");

            GameObject imageGO = new GameObject("MyRuntimeImage (Scratch)");
            currentLines.Add(imageGO);

            // --- Add UI Components ---
            Image uiImage = imageGO.AddComponent<Image>(); // Step 2
            imageGO.transform.SetParent(linesParent.transform, true);

            RectTransform imageRect = imageGO.GetComponent<RectTransform>();
            imageRect.localScale = Vector3.one;
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(1, 1); // Step 4 (Example config)

            if (tempNode.isPurchased) 
            {
                uiImage.color = Color.cyan;
            } else
            {
                uiImage.color = Color.red;
            }


            Vector3 endPos = upgradeNode.transform.position; // World position of the target node

            // Calculate midpoint, direction, and distance for the line
            Vector3 direction = endPos - startPos;

            Vector3 midpoint = startPos + (direction / 2.0f); // Same as (startPos + endPos) / 2.0f

            // Position the cube at the midpoint
            imageGO.transform.position = midpoint;

            // Scale the image: make it thin on Z/Y and long on X (matching the distance)
            imageGO.transform.localScale = new Vector3(Vector3.Distance(endPos, startPos)*0.5f, connectionLineWidth, 1);

            // Calculate angle using Atan2 (gives radians)
            // Atan2 takes y component first, then x component
            float angleRadians = Mathf.Atan2(direction.y, direction.x);

            // Convert angle to degrees
            float angleDegrees = angleRadians * Mathf.Rad2Deg;

            // Apply rotation (around the Z-axis for 2D UI)
            imageGO.transform.eulerAngles = new Vector3(0, 0, angleDegrees);
        }
    }

    public void OpenSingleUpgrade()
    {
        upgradeManager.CurrentUpgrade(this);
    }

}